using CPDLCServer.AcarsConnections.Stations;
using CPDLCServer.AcarsConnections.Traffic;

namespace CPDLCServer.AcarsConnections;

public static class AcarsConnectionsServiceCollectionExtensions
{
    public static IServiceCollection AddAcarsConnections(this IServiceCollection services)
    {
        services.AddSingleton<AcarsTrafficLog>();
        services.AddSingleton<AcarsConnectionManager>();
        services.AddSingleton<IAcarsConnectionManager>(sp => sp.GetRequiredService<AcarsConnectionManager>());
        services.AddHostedService(sp => sp.GetRequiredService<AcarsConnectionManager>());

        services.AddSingleton<IAcarsStationRepository, InMemoryAcarsStationRepository>();
        services.AddHostedService<AcarsConnectivityCheckService>();

        return services;
    }
}
