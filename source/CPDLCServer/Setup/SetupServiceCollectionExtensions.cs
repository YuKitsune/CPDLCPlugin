namespace CPDLCServer.Setup;

public static class SetupServiceCollectionExtensions
{
    public static IServiceCollection AddSetup(this IServiceCollection services)
    {
        services.AddSingleton<SetupState>();
        return services;
    }
}
