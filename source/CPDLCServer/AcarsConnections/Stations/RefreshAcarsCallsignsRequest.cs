using MediatR;

namespace CPDLCServer.AcarsConnections.Stations;

public record RefreshAcarsCallsignsRequest(string AcarsConnectionId) : IRequest;
