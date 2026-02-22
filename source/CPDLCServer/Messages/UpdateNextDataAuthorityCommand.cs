using MediatR;

namespace CPDLCServer.Messages;

public record UpdateNextDataAuthorityCommand(
    string StationId,
    string Callsign,
    string? NextDataAuthority,
    DateTimeOffset? ExpectedTransferTime)
    : IRequest;
