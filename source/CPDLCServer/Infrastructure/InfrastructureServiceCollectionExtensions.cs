namespace CPDLCServer.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // The start time is recorded now, when the server starts, and not when the service is first used
        var clock = new SystemClock();
        services.AddSingleton<IClock>(clock);
        services.AddSingleton(new ServerStartTime(clock.UtcNow()));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        services.AddSingleton<IUnitOfWorkMediator, UnitOfWorkMediator>();

        return services;
    }
}
