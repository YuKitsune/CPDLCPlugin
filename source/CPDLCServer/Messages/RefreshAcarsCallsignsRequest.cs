using MediatR;

namespace CPDLCServer.Messages;

public record RefreshAcarsCallsignsRequest(string AcarsClientId) : IRequest;
