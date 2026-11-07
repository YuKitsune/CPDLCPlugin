using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.AcarsConnections;

public class GetAtsuSettingsRequestHandler(IAcarsConnectionManager acarsConnectionManager)
    : IRequestHandler<GetAtsuSettingsRequest, AtsuSettingsDto>
{
    public async Task<AtsuSettingsDto> Handle(GetAtsuSettingsRequest request, CancellationToken cancellationToken)
    {
        var configuration = await acarsConnectionManager.GetConfigurationForStation(request.StationId, cancellationToken);

        return new AtsuSettingsDto(
            request.StationId,
            TimeSpan.FromMinutes(configuration.DefaultAdscReportingInterval));
    }
}
