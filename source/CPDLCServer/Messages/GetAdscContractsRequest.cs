using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.Messages;

public record GetAdscContractsRequest : IRequest<GetAdscContractsResult>;

public record GetAdscContractsResult(AdscContractDto[] Contracts);
