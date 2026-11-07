namespace CPDLCServer.Web;

public static class WebServiceCollectionExtensions
{
    public static IServiceCollection AddWeb(this IServiceCollection services)
    {
        services.AddSignalR().AddJsonProtocol();
        services.AddRazorPages(options =>
        {
            options.RootDirectory = "/Web/Pages";

            // Only a user that is logged in can open these pages
            options.Conventions.AuthorizeFolder("/Settings");
            options.Conventions.AuthorizeFolder("/Logs");
        });

        return services;
    }
}
