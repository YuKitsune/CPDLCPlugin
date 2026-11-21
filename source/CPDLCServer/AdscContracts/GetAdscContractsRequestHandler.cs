using MediatR;

namespace CPDLCServer.AdscContracts;

public class GetAdscContractsRequestHandler(IAdscContractRepository adscContractRepository)
    : IRequestHandler<GetAdscContractsRequest, GetAdscContractsResult>
{
    public async Task<GetAdscContractsResult> Handle(
        GetAdscContractsRequest request,
        CancellationToken cancellationToken)
    {
        var contracts = await adscContractRepository.All(cancellationToken);
        var contractDtos = contracts
            .Select(AdscContractConverter.ToDto)
            .ToArray();

        return new GetAdscContractsResult(contractDtos);
    }
}
