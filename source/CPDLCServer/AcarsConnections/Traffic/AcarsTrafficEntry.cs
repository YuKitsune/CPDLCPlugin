namespace CPDLCServer.AcarsConnections.Traffic;

// One HTTP request to the ACARS network and its response, as they were sent and received.
// The authentication code is not in the request.
public record AcarsTrafficEntry(
    DateTimeOffset Time,
    string Type,
    string Request,
    int? StatusCode,
    string? Response,
    TimeSpan Duration,
    string? Error)
{
    // Most polls get no messages. The traffic page can hide them.
    public bool IsEmptyPoll => Type == "poll" && Error is null && Response?.Trim() == "ok";
}
