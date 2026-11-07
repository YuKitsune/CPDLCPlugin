namespace CPDLCServer.Settings;

public static class SettingsServiceCollectionExtensions
{
    public static IServiceCollection AddSettings(this IServiceCollection services)
    {
        services.AddScoped<ServerSettingsProvider>();
        services.AddScoped<IServerSettingsProvider>(sp => sp.GetRequiredService<ServerSettingsProvider>());

        return services;
    }
}
