namespace CPDLCServer.Controllers;

public static class ControllersServiceCollectionExtensions
{
    // Not AddControllers, because ASP.NET Core MVC already has a method with that name
    public static IServiceCollection AddAirTrafficControllers(this IServiceCollection services)
    {
        services.AddSingleton<IControllerRepository, InMemoryControllerRepository>();

        return services;
    }
}
