namespace CPDLCServer.Infrastructure;

public static class ConfigurationExtensionMethods
{
    public static string GetRequiredValue(this IConfiguration configuration, string key)
    {
        return configuration[key] ?? throw new Exception($"{key} is required but was not configured.");
    }
}
