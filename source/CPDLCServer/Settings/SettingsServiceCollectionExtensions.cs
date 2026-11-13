namespace CPDLCServer.Settings;

public static class SettingsServiceCollectionExtensions
{
    public static IServiceCollection AddSettings(this IServiceCollection services)
    {
        services.AddScoped<IServerSettingsProvider, ServerSettingsProvider>();
        return services;
    }
}
