using MediatR;

namespace CPDLCServer.AdscContracts;

public record AdscContractChangedNotification(AdscContract Contract) : INotification;
