using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.AdscContracts;

public record GetAdscContractsRequest : IRequest<GetAdscContractsResult>;

public record GetAdscContractsResult(AdscContractDto[] Contracts);
