using CPDLCServer.AcarsConnections;
using MediatR;

namespace CPDLCServer.Cpdlc.Connections;

public record ControlMessageReceivedNotification(
    string AcarsConnectionId,
    ReceivedControlMessage ControlMessage)
    : INotification;
