using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CPDLCServer.Auth.Login;

/// <summary>
/// Limits the number of form posts from one IP address to the login page and to the plugin authorize page, so that a
/// person cannot quickly try many passwords, or make many plugin tokens. The two pages use the same limit.
/// The limit also applies to a plugin authorize request that has a CID in the URL, because the BuiltIn login makes a
/// token from it without a form post.
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
            // Only limit the form posts and the requests with a CID. Do not limit the pages themselves.
            if (!HttpMethods.IsPost(httpContext.Request.Method) && !httpContext.Request.Query.ContainsKey("cid"))
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
