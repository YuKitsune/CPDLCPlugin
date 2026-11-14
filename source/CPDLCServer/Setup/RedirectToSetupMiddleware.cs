namespace CPDLCServer.Setup;

/// <summary>
///     Redirects requests to the setup page until the setup has been completed.
/// </summary>
public class RedirectToSetupMiddleware(RequestDelegate next, SetupState setupState)
{
    static readonly string[] AllowedPaths = ["/setup", "/login", "/logout", "/health", "/hubs"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!setupState.IsConfigured && !IsAllowed(context.Request.Path))
        {
            context.Response.Redirect("/setup");
            return;
        }

        await next(context);
    }

    static bool IsAllowed(PathString path)
    {
        return AllowedPaths.Any(allowedPath => path.StartsWithSegments(allowedPath, StringComparison.OrdinalIgnoreCase));
    }
}
