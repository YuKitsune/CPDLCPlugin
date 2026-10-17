using CPDLCPlugin.Configuration;
using CPDLCPlugin.Server;
using CPDLCServer.Contracts;
using MediatR;
using Newtonsoft.Json;
using Serilog;

namespace CPDLCPlugin.Messages;

public record UpdateNextDataAuthorityRequest(string Callsign, IReadOnlyList<SectorEntry> SectorEntries) : IRequest;

public class UpdateNextDataAuthorityRequestHandler(
    IServerConnection serverConnection,
    AircraftConnectionStore aircraftConnectionStore,
    AcarsStationStore acarsStationStore,
    PluginConfiguration configuration,
    IErrorReporter errorReporter,
    ILogger logger)
    : IRequestHandler<UpdateNextDataAuthorityRequest>
{
    public async Task Handle(UpdateNextDataAuthorityRequest request, CancellationToken cancellationToken)
    {
        if (!serverConnection.IsConnected)
            return;

        var aircraftConnections = await aircraftConnectionStore.All(cancellationToken);
        var ourAircraft = aircraftConnections.FirstOrDefault(
            c => c.Callsign == request.Callsign &&
                 c.StationId == serverConnection.StationIdentifier &&
                 c.DataAuthorityState == DataAuthorityState.CurrentDataAuthority);

        if (ourAircraft is null)
            return;

        logger.Verbose("Calculating next data authority for {Callsign}", request.Callsign);

        var newInfo = await CalculateNextDataAuthority(
            request.Callsign,
            request.SectorEntries,
            serverConnection.StationIdentifier,
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
                    request.Callsign,
                    errorInfo.ErrorMessage);

                errorReporter.ReportError(
                    new Exception($"Cannot calculate next data authority for {request.Callsign}: {errorInfo.ErrorMessage}"));
            }

            ourAircraft.SetNextDataAuthorityInfo(newInfo);

            // Clear server-side state if previously had valid NDA
            if (oldInfo is not ValidNextDataAuthorityInfo)
                return;

            logger.Information(
                "Transmitting NDA clear to server for {Callsign} (error state)",
                request.Callsign);

            await serverConnection.UpdateNextDataAuthority(
                request.Callsign,
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
                request.Callsign);

            await serverConnection.UpdateNextDataAuthority(
                request.Callsign,
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
                request.Callsign,
                validInfo.NextDataAuthority,
                validInfo.ExitTime);

            ourAircraft.SetNextDataAuthorityInfo(newInfo);

            await serverConnection.UpdateNextDataAuthority(
                request.Callsign,
                validInfo.NextDataAuthority,
                validInfo.ExitTime,
                serverCallCts.Token);
        }
    }

    async Task<INextDataAuthorityInfo> CalculateNextDataAuthority(
        string callsign,
        IReadOnlyList<SectorEntry> sectorEntries,
        string currentStationId,
        CancellationToken cancellationToken)
    {
        logger.Verbose("{Callsign} sector entry info: {SectorEntryInfo}", callsign, JsonConvert.SerializeObject(sectorEntries));

        // Build ATSU code candidates for each sector entry along the route.
        var sectorEntryCandidates = new List<(SectorEntry SectorEntry, IReadOnlyList<string> Candidates)>();
        foreach (var sectorEntry in sectorEntries)
        {
            logger.Verbose("{Callsign} enters [{SectorIds}] at {EntryTime}", callsign, string.Join(", ", sectorEntry.SectorIds), sectorEntry.EntryTime);

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
                        callsign,
                        errorMessage);
                    return new ErrorNextDataAuthorityInfo(errorMessage);
                }

                if (matches.Count == 1 && seen.Add(matches[0]))
                    candidates.Add(matches[0]);
            }

            if (candidates.Count == 0)
            {
                logger.Verbose("No ATSU codes found in hierarchy for {Callsign}, skipping", callsign);
                continue;
            }

            sectorEntryCandidates.Add((sectorEntry, candidates.AsReadOnly()));
        }

        // If the route enters our own airspace later, the aircraft was likely handed off early.
        // Do not use the sectors before our last entry, so that the NDA does not send the aircraft back.
        var lastOwnEntryIndex = sectorEntryCandidates.FindLastIndex(
            e => e.Candidates[0].Equals(currentStationId, StringComparison.OrdinalIgnoreCase));

        foreach (var (sectorEntry, candidates) in sectorEntryCandidates.Skip(lastOwnEntryIndex + 1))
        {
            // Find the first candidate (most-specific first) that is currently online.
            foreach (var atsuCode in candidates)
            {
                if (atsuCode.Equals(currentStationId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (await acarsStationStore.IsOnline(atsuCode, cancellationToken))
                {
                    logger.Verbose(
                        "ATSU boundary found for {Callsign}: {AtsuCode} at {ExitTime}",
                        callsign,
                        atsuCode,
                        sectorEntry.EntryTime);

                    return new ValidNextDataAuthorityInfo(atsuCode, sectorEntry.EntryTime);
                }
            }

            logger.Verbose(
                "ATSU boundary found for {Callsign} but no candidates online [{Candidates}], continuing",
                callsign,
                string.Join(", ", candidates));
        }

        logger.Verbose("No ATSU boundary found for {Callsign}", callsign);
        return NoneNextDataAuthorityInfo.Instance;
    }
}
