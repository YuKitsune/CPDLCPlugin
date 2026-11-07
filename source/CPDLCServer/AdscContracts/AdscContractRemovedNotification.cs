using MediatR;

namespace CPDLCServer.AdscContracts;

public record AdscContractRemovedNotification(string Callsign, string StationId) : INotification;
