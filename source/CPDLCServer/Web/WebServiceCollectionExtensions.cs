using CPDLCServer.Auth;

namespace CPDLCServer.Web;

public static class WebServiceCollectionExtensions
{
    public static IServiceCollection AddWeb(this IServiceCollection services)
    {
        services.AddSignalR().AddJsonProtocol();
        services.AddRazorPages(options =>
        {
            options.RootDirectory = "/Web/Pages";

            // Any VATSIM user that is logged in can open the pages
            options.Conventions.AuthorizeFolder("/");
            options.Conventions.AllowAnonymousToPage("/Account/Login");
            options.Conventions.AllowAnonymousToPage("/Account/Logout");
            options.Conventions.AllowAnonymousToPage("/Setup/Index");

            // Only an administrator can open the Configuration area
            options.Conventions.AuthorizeFolder("/Configuration", AdministratorRequirement.PolicyName);
        });

        return services;
    }
}
