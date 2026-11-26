using System.Net.Http.Headers;
using System.Text.Json;
using CPDLCServer.Auth.Administrators;
using CPDLCServer.Auth.Login;
using CPDLCServer.Auth.PluginTokens;
using CPDLCServer.Auth.VatsimConnect;
using CPDLCServer.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace CPDLCServer.Auth;

public static class AuthServiceCollectionExtensions
{
    // The time without a request before the user must log in again. The privacy policy shows this value.
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    public static IServiceCollection AddAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        AuthOptions authOptions)
    {
        services.AddSingleton(authOptions);

        services.AddScoped<IAdministratorRepository, AdministratorRepository>();
        services.AddScoped<IPluginTokenRepository, PluginTokenRepository>();
        services.AddSingleton<PluginAuthorizationCodes>();

        // Only the BuiltIn login uses passwords. The services are registered for both types, so that the
        // administrator handlers do not change with the type.
        services.AddSingleton<IPasswordHasher<Administrator>, PasswordHasher<Administrator>>();
        services.AddSingleton<LoginLockout>();
        services.AddRateLimiter(PasswordRateLimit.Configure);

        // Keep the keys of the login cookies only in memory, so that no key is on the disk.
        // After a restart, the user must log in again.
        services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();

        var authentication = services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.AccessDeniedPath = "/access-denied";
                options.Cookie.Name = "cpdlc-session";
                options.Cookie.HttpOnly = true;

                // Lax, not Strict: after the login, VATSIM Connect redirects the browser back to this server.
                // With Strict, the browser does not send the cookie in that redirect, and the user is not logged in.
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = SessionLifetime;
                options.SlidingExpiration = true;
            })
            .AddScheme<AuthenticationSchemeOptions, PluginTokenAuthenticationHandler>(
                PluginTokenAuthenticationHandler.AuthenticationScheme,
                _ => { });

        // The BuiltIn login does not need a VATSIM Connect client
        if (authOptions.IsVatsim)
            AddVatsimConnect(authentication, configuration);

        services.AddScoped<IAuthorizationHandler, AdministratorRequirementHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AdministratorRequirement.PolicyName,
                policy => policy.RequireAuthenticatedUser().AddRequirements(new AdministratorRequirement()));

            // The plugin uses its token, not the login cookie
            options.AddPolicy(
                PluginTokenAuthenticationHandler.PolicyName,
                policy => policy
                    .AddAuthenticationSchemes(PluginTokenAuthenticationHandler.AuthenticationScheme)
                    .RequireAuthenticatedUser());
        });

        return services;
    }

    static void AddVatsimConnect(AuthenticationBuilder authentication, IConfiguration configuration)
    {
        var vatsimConnectOptions = new VatsimConnectOptions
        {
            ClientId = configuration.GetRequiredValue("VatsimConnect:ClientId"),
            ClientSecret = configuration.GetRequiredValue("VatsimConnect:ClientSecret"),
            Authority = configuration["VatsimConnect:Authority"] is { Length: > 0 } authority
                ? new Uri(authority)
                : new Uri("https://auth.vatsim.net")
        };

        authentication.AddOAuth(VatsimConnectOptions.AuthenticationScheme, "VATSIM", options =>
        {
            options.ClientId = vatsimConnectOptions.ClientId;
            options.ClientSecret = vatsimConnectOptions.ClientSecret;
            options.CallbackPath = VatsimConnectOptions.CallbackPath;
            options.AuthorizationEndpoint = new Uri(vatsimConnectOptions.Authority, "/oauth/authorize").ToString();
            options.TokenEndpoint = new Uri(vatsimConnectOptions.Authority, "/oauth/token").ToString();
            options.UserInformationEndpoint = new Uri(vatsimConnectOptions.Authority, "/api/user").ToString();
            options.UsePkce = true;
            options.Scope.Add("full_name");
            options.Scope.Add("vatsim_details");

            // The server uses the VATSIM token only to read the user. It does not keep the token.
            options.SaveTokens = false;

            options.Events.OnCreatingTicket = async context =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();

                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
                var user = VatsimUser.Parse(document.RootElement);

                context.Principal = new(CurrentUser.CreateIdentity(user.Cid, user.Name, VatsimConnectOptions.AuthenticationScheme));

                var mediator = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWorkMediator>();
                await mediator.Send(new UpdateAdministratorNameCommand(user.Cid, user.Name), context.HttpContext.RequestAborted);

                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger>();
                logger.Information("{Name} ({Cid}) logged in", user.Name, user.Cid);
            };
        });
    }
}
