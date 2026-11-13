using CPDLCServer.AdscContracts.MessageLog;
using CPDLCServer.AdscContracts.Reports;

namespace CPDLCServer.AdscContracts;

public static class AdscContractsServiceCollectionExtensions
{
    public static IServiceCollection AddAdsContracts(this IServiceCollection services)
    {
        services.AddScoped<IAdscContractRepository, AdscContractRepository>();
        services.AddHostedService<AdscContractExpiryService>();

        services.AddScoped<IAdscReportRepository, AdscReportRepository>();
        services.AddSingleton<IAdscMessageLogRepository, InMemoryAdscMessageLogRepository>();

        return services;
    }
}
