using CPDLCServer.Model;
using MediatR;

namespace CPDLCServer.Messages;

public record AdscContractChangedNotification(AdscContract Contract) : INotification;
