using CPDLCPlugin.Configuration;
using CPDLCServer.Contracts;
using MediatR;
using Newtonsoft.Json;
using Serilog;
using vatsys;

namespace CPDLCPlugin.Messages;

public record UpdateNextDataAuthorityForFdrRequest(FDP2.FDR Fdr) : IRequest;

public class UpdateNextDataAuthorityForFdrRequestHandler(
    Plugin plugin,
    AircraftConnectionStore aircraftConnectionStore,
    AcarsStationStore acarsStationStore,
    PluginConfiguration configuration,
    ILogger logger)
    : IRequestHandler<UpdateNextDataAuthorityForFdrRequest>
{
    public async Task Handle(UpdateNextDataAuthorityForFdrRequest request, CancellationToken cancellationToken)
    {
        var fdr = request.Fdr;

        if (plugin.ConnectionManager is null || !plugin.ConnectionManager.IsConnected)
            return;

        var aircraftConnections = await aircraftConnectionStore.All(cancellationToken);
        var ourAircraft = aircraftConnections.FirstOrDefault(
            c => c.Callsign == fdr.Callsign &&
                 c.StationId == plugin.ConnectionManager.StationIdentifier &&
                 c.DataAuthorityState == DataAuthorityState.CurrentDataAuthority);

        if (ourAircraft is null)
            return;

        logger.Verbose("Calculating next data authority for {Callsign}", fdr.Callsign);

        var newInfo = await CalculateNextDataAuthority(
            fdr,
            plugin.ConnectionManager.StationIdentifier,
            cancellationToken);

        var oldInfo = ourAircraft.NextDataAuthorityInfo;

        using var serverCallCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        serverCallCts.CancelAfter(TimeSpan.FromSeconds(10));

        // Handle state transitions and error display
        if (newInfo is ErrorNextDataAuthorityInfo errorInfo)
        {
            // Only show error if we're transitioning TO error state (not if already in error)
            if (oldInfo is not ErrorNextDataAuthorityInfo existingErrorInfo ||
                existingErrorInfo.ErrorMessage != errorInfo.ErrorMessage)
            {
                logger.Information(
                    "Next data authority calculation failed for {Callsign}: {ErrorMessage}",
                    fdr.Callsign,
                    errorInfo.ErrorMessage);

                Plugin.AddError(
                    new Exception($"Cannot calculate next data authority for {fdr.Callsign}: {errorInfo.ErrorMessage}"));
            }

            ourAircraft.SetNextDataAuthorityInfo(newInfo);

            // Clear server-side state if previously had valid NDA
            if (oldInfo is not ValidNextDataAuthorityInfo)
                return;

            logger.Information(
                "Transmitting NDA clear to server for {Callsign} (error state)",
                fdr.Callsign);

            await plugin.ConnectionManager.UpdateNextDataAuthority(
                fdr.Callsign,
                null,
                null,
                serverCallCts.Token);

            return;
        }

        // Handle None state
        if (newInfo is NoneNextDataAuthorityInfo)
        {
            ourAircraft.SetNextDataAuthorityInfo(newInfo);

            var didChange = oldInfo is not NoneNextDataAuthorityInfo;
            if (!didChange)
                return;

            logger.Information(
                "No ATSU boundary found for {Callsign}, clearing NDA on server",
                fdr.Callsign);

            await plugin.ConnectionManager.UpdateNextDataAuthority(
                fdr.Callsign,
                null,
                null,
                serverCallCts.Token);

            return;
        }

        // Handle Valid state
        if (newInfo is ValidNextDataAuthorityInfo validInfo)
        {
            var didChange =
                oldInfo is not ValidNextDataAuthorityInfo oldValidInfo || // Check if transitioning from non-Valid state
                oldValidInfo.NextDataAuthority != validInfo.NextDataAuthority || // Check if NDA changed
                Math.Abs((oldValidInfo.ExitTime - validInfo.ExitTime).TotalMinutes) > 1; // Check if exit time changed by more than 1 minute

            if (!didChange)
                return;

            logger.Information(
                "Next data authority for {Callsign}: {NextDataAuthority} at {ExitTime}",
                fdr.Callsign,
                validInfo.NextDataAuthority,
                validInfo.ExitTime);

            ourAircraft.SetNextDataAuthorityInfo(newInfo);

            await plugin.ConnectionManager.UpdateNextDataAuthority(
                fdr.Callsign,
                validInfo.NextDataAuthority,
                validInfo.ExitTime,
                serverCallCts.Token);
        }
    }

    async Task<INextDataAuthorityInfo> CalculateNextDataAuthority(
        FDP2.FDR fdr,
        string currentStationId,
        CancellationToken cancellationToken)
    {
        var sectorEntryInfos = GetSectorEntryInfos(fdr);

        logger.Verbose("{Callsign} sector entry info: {SectorEntryInfo}", fdr.Callsign, JsonConvert.SerializeObject(sectorEntryInfos));

        foreach (var sectorEntry in sectorEntryInfos)
        {
            logger.Verbose("{Callsign} enters [{SectorIds}] at {EntryTime}", fdr.Callsign, string.Join(", ", sectorEntry.SectorIds), sectorEntry.SectorEntryTime);

            // Build ATSU code candidates from the sector hierarchy (most-specific first),
            // deduplicating to avoid redundant online checks.
            var candidates = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sectorId in sectorEntry.SectorIds)
            {
                var matches = configuration.AtsuCodes
                    .Where(m => m.Sectors.Any(s => s.Equals(sectorId, StringComparison.OrdinalIgnoreCase)))
                    .Select(m => m.AtsuCode)
                    .ToList();

                if (matches.Count > 1)
                {
                    var errorMessage = $"Sector {sectorId} is claimed by multiple ATSU codes in config: {string.Join(", ", matches)}";
                    logger.Warning(
                        "Cannot calculate next data authority for {Callsign}: {ErrorMessage}",
                        fdr.Callsign,
                        errorMessage);
                    return new ErrorNextDataAuthorityInfo(errorMessage);
                }

                if (matches.Count == 1 && seen.Add(matches[0]))
                    candidates.Add(matches[0]);
            }

            if (candidates.Count == 0)
            {
                logger.Verbose("No ATSU codes found in hierarchy for {Callsign}, skipping", fdr.Callsign);
                continue;
            }

            // If the most-specific candidate is our own station, this is our airspace.
            if (candidates[0].Equals(currentStationId, StringComparison.OrdinalIgnoreCase))
                continue;

            // Find the first candidate (most-specific first) that is currently online.
            foreach (var atsuCode in candidates)
            {
                if (atsuCode.Equals(currentStationId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (await acarsStationStore.IsOnline(atsuCode, cancellationToken))
                {
                    logger.Verbose(
                        "ATSU boundary found for {Callsign}: {AtsuCode} at {ExitTime}",
                        fdr.Callsign,
                        atsuCode,
                        sectorEntry.SectorEntryTime);

                    return new ValidNextDataAuthorityInfo(atsuCode, sectorEntry.SectorEntryTime);
                }
            }

            logger.Verbose(
                "ATSU boundary found for {Callsign} but no candidates online [{Candidates}], continuing",
                fdr.Callsign,
                string.Join(", ", candidates));
        }

        logger.Verbose("No ATSU boundary found for {Callsign}", fdr.Callsign);
        return NoneNextDataAuthorityInfo.Instance;
    }

    record SectorEntryInfo(IReadOnlyList<string> SectorIds, DateTimeOffset SectorEntryTime);

    static IReadOnlyList<SectorEntryInfo> GetSectorEntryInfos(FDP2.FDR fdr)
    {
        var results = new List<SectorEntryInfo>();

        foreach (var segment in fdr.ParsedRoute.Skip(fdr.ParsedRoute.OverflownIndex))
        {
            if (segment.Type != FDP2.FDR.ExtractedRoute.Segment.SegmentTypes.ZPOINT)
                continue;

            var volume = (SectorsVolumes.Volume?)segment.Tag;
            if (volume is null)
                continue;

            var sector = SectorsVolumes.FindSector(volume);
            if (sector is null)
                continue;

            // Walk the sector hierarchy from most-specific to least-specific.
            var sectorIds = new List<string>();
            var current = sector;
            while (current is not null)
            {
                sectorIds.Add(current.Name);
                var captured = current;
                current = SectorsVolumes.SectorGroupings.Keys
                    .Where(p => p.SubSectors.Contains(captured))
                    .OrderBy(p => p.SubSectors.Count)
                    .FirstOrDefault();
            }

            var eto = segment.ETO;
            var etoDateTimeOffset = new DateTimeOffset(
                eto.Year,
                eto.Month,
                eto.Day,
                eto.Hour,
                eto.Minute,
                eto.Second,
                offset: TimeSpan.Zero);

            results.Add(new SectorEntryInfo(sectorIds.AsReadOnly(), etoDateTimeOffset));
        }

        return results.AsReadOnly();
    }
}
