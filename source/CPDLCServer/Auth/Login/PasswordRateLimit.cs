using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CPDLCServer.Auth.Login;

/// <summary>
/// Limits the number of password attempts from one IP address, so that a person cannot quickly try many passwords.
/// </summary>
public static class PasswordRateLimit
{
    public const string PolicyName = "password";

    const int PermitLimit = 10;
    static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = (context, cancellationToken) =>
            new ValueTask(context.HttpContext.Response.WriteAsync(
                "Too many attempts. Wait one minute, then try again.",
                cancellationToken));

        options.AddPolicy(PolicyName, httpContext =>
        {
            // Only a form post sends a password. Do not limit the page itself.
            if (!HttpMethods.IsPost(httpContext.Request.Method))
                return RateLimitPartition.GetNoLimiter(string.Empty);

            var address = ClientIpAddress.Get(httpContext);
            return RateLimitPartition.GetFixedWindowLimiter(address, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = PermitLimit,
                Window = Window,
                QueueLimit = 0
            });
        });
    }
}
