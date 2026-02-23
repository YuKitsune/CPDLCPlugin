using MediatR;

namespace CPDLCServer.Messages;

public record AcarsConnectedCallsignsChangedNotification(string[] Callsigns) : INotification;
