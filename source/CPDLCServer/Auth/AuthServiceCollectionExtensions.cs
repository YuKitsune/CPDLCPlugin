using CPDLCServer.Auth.Login;
using CPDLCServer.Auth.Users;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace CPDLCServer.Auth;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddAuth(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        // Keep the keys of the login cookies only in memory, so that no key is on the disk.
        // After a restart, the user must log in again.
        services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.AccessDeniedPath = "/login";
                options.Cookie.Name = "cpdlc-admin";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });
        services.AddAuthorization();

        services.AddSingleton<LoginLockout>();
        services.AddRateLimiter(PasswordRateLimit.Configure);

        return services;
    }
}
