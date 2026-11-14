using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public record UpdateNextDataAuthorityCommand(
    string StationId,
    string Callsign,
    string? NextDataAuthority,
    DateTimeOffset? ExpectedTransferTime)
    : IRequest;
