using CPDLCServer.Clients;
using CPDLCServer.Contracts;
using CPDLCServer.Messages;
using MediatR;

namespace CPDLCServer.Handlers;

public class GetAtsuSettingsRequestHandler(IClientManager clientManager)
    : IRequestHandler<GetAtsuSettingsRequest, AtsuSettingsDto>
{
    public async Task<AtsuSettingsDto> Handle(GetAtsuSettingsRequest request, CancellationToken cancellationToken)
    {
        var configuration = await clientManager.GetConfigurationForStation(request.StationId, cancellationToken);

        return new AtsuSettingsDto(
            request.StationId,
            TimeSpan.FromMinutes(configuration.DefaultAdscReportingInterval));
    }
}
