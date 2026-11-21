using MediatR;

namespace CPDLCServer.AcarsConnections.ConnectionSettings;

public record DeleteAcarsConnectionCommand(Guid Id) : IRequest;
