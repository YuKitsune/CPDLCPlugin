using MediatR;

namespace CPDLCServer.AdscContracts;

public record EndExpiredAdscContractsRequest(TimeSpan ContractTimeout) : IRequest;
