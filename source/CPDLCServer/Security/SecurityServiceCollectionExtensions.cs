using CPDLCServer.Infrastructure;

namespace CPDLCServer.Security;

public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // The master key encrypts the secrets in the database. A missing or invalid MasterKey stops the server.
        var masterKey = configuration.GetRequiredValue("MasterKey");
        services.AddSingleton(new MasterKeyEncryption(masterKey));

        return services;
    }
}
