using MediatR;

namespace CPDLCServer.AcarsConnections.ConnectionSettings;

// The authentication code is plain text. The handler encrypts it.
public record CreateAcarsConnectionCommand(AcarsConnectionSettings Settings, string? AuthenticationCode) : IRequest<Guid>;
