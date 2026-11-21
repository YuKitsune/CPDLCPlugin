using MediatR;

namespace CPDLCServer.AcarsConnections.ConnectionSettings;

// The authentication code is plain text. An empty value keeps the current authentication code.
public record UpdateAcarsConnectionCommand(Guid Id, AcarsConnectionSettings Settings, string? AuthenticationCode) : IRequest;
