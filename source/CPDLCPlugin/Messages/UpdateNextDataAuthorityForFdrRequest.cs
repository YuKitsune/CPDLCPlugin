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
            plugin.ConnectionManager.StationIdentifier);

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

    Task<INextDataAuthorityInfo> CalculateNextDataAuthority(
        FDP2.FDR fdr,
        string currentStationId)
    {
        var sectorEntryInfos = GetSectorEntryInfos(fdr);

        logger.Verbose("{Callsign} sector entry info: {SectorEntryInfo}", fdr.Callsign, JsonConvert.SerializeObject(sectorEntryInfos));

        foreach (var sectorEntry in sectorEntryInfos)
        {
            // Find which ATSU codes in the config claim this sector
            var matchingAtsuCodes = configuration.AtsuCodes
                .Where(kvp => kvp.Value.Any(s => s.Equals(sectorEntry.SectorId, StringComparison.OrdinalIgnoreCase)))
                .Select(kvp => kvp.Key)
                .ToList();

            logger.Verbose("{Callsign} enters {SectorId} at {EntryTime}", fdr.Callsign, sectorEntry.SectorId, sectorEntry.SectorEntryTime);

            if (matchingAtsuCodes.Count == 0)
            {
                logger.Verbose(
                    "Sector {SectorId} not found in AtsuCodes config for {Callsign}, skipping",
                    sectorEntry.SectorId,
                    fdr.Callsign);
                continue;
            }

            if (matchingAtsuCodes.Count > 1)
            {
                var errorMessage = $"Sector {sectorEntry.SectorId} is claimed by multiple ATSU codes in config: {string.Join(", ", matchingAtsuCodes)}";
                logger.Warning(
                    "Cannot calculate next data authority for {Callsign}: {ErrorMessage}",
                    fdr.Callsign,
                    errorMessage);
                return Task.FromResult<INextDataAuthorityInfo>(new ErrorNextDataAuthorityInfo(errorMessage));
            }

            var atsuCode = matchingAtsuCodes[0];
            if (atsuCode.Equals(currentStationId, StringComparison.OrdinalIgnoreCase))
                continue;

            logger.Verbose(
                "ATSU boundary found for {Callsign}: sector {SectorId} with code {AtsuCode} at {ExitTime}",
                fdr.Callsign,
                sectorEntry.SectorId,
                atsuCode,
                sectorEntry.SectorEntryTime);

            return Task.FromResult<INextDataAuthorityInfo>(new ValidNextDataAuthorityInfo(atsuCode, sectorEntry.SectorEntryTime));
        }

        logger.Verbose("No ATSU boundary found for {Callsign}", fdr.Callsign);
        return Task.FromResult<INextDataAuthorityInfo>(NoneNextDataAuthorityInfo.Instance);
    }

    record SectorEntryInfo(string SectorId, DateTimeOffset SectorEntryTime);

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

            // If this is a subsector, find the smallest parent sector that contains it
            // (e.g., if flying through ML-SNO subsector, find parent YMMM sector)
            var parentSector = SectorsVolumes.SectorGroupings.Keys
                .Where(parent => parent.SubSectors.Contains(sector))
                .OrderBy(parent => parent.SubSectors.Count)
                .FirstOrDefault();

            if (parentSector is not null)
            {
                sector = parentSector;
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

            results.Add(new SectorEntryInfo(sector.Name, etoDateTimeOffset));
        }

        return results.AsReadOnly();
    }
}
