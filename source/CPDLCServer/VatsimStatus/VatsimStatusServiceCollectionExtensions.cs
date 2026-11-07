namespace CPDLCServer.VatsimStatus;

public static class VatsimStatusServiceCollectionExtensions
{
    public static IServiceCollection AddVatsimStatus(this IServiceCollection services)
    {
        services.AddSingleton<IVatsimPilotRepository, InMemoryVatsimPilotRepository>();
        services.AddHostedService<VatsimStatusService>();

        return services;
    }
}
