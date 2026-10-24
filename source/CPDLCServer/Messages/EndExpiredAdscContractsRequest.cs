using MediatR;

namespace CPDLCServer.Messages;

public record EndExpiredAdscContractsRequest(TimeSpan ContractTimeout) : IRequest;
