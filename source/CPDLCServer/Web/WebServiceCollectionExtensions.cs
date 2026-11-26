using CPDLCServer.Auth;

namespace CPDLCServer.Web;

public static class WebServiceCollectionExtensions
{
    public static IServiceCollection AddWeb(this IServiceCollection services, AuthOptions authOptions)
    {
        services.AddSignalR().AddJsonProtocol();
        services.AddRazorPages(options =>
        {
            options.RootDirectory = "/Web/Pages";

            // With VATSIM Connect, any VATSIM user that is logged in can open the pages.
            // With the BuiltIn login, the pages need no login. Only the administrators log in.
            if (authOptions.IsVatsim)
            {
                options.Conventions.AuthorizeFolder("/");
                options.Conventions.AllowAnonymousToPage("/Account/Login");
                options.Conventions.AllowAnonymousToPage("/Account/Logout");
                options.Conventions.AllowAnonymousToPage("/Setup/Index");
                options.Conventions.AllowAnonymousToPage("/Privacy");
            }

            // Only an administrator can open the Configuration area and change their password
            options.Conventions.AuthorizeFolder("/Configuration", AdministratorRequirement.PolicyName);
            options.Conventions.AuthorizePage("/Account/Password", AdministratorRequirement.PolicyName);
        });

        return services;
    }
}
