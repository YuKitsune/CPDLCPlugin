using MediatR;

namespace CPDLCServer.Messages;

public record AdscContractRemovedNotification(string Callsign, string StationId) : INotification;
