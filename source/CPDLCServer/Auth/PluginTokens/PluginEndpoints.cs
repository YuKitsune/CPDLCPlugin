using System.Security.Claims;
using CPDLCServer.Contracts;
using CPDLCServer.Infrastructure;

namespace CPDLCServer.Auth.PluginTokens;

// The plugin changes its code for a token here, and checks or revokes its token.
// The browser part of the login is the /plugin/authorize page.
public static class PluginEndpoints
{
    public static void MapPluginEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/plugin/token", CreateToken);
        endpoints.MapGet("/plugin/me", GetUser).RequireAuthorization(PluginTokenAuthenticationHandler.PolicyName);
        endpoints.MapPost("/plugin/logout", Logout).RequireAuthorization(PluginTokenAuthenticationHandler.PolicyName);
    }

    // The form is read directly, so that no antiforgery token is necessary. The plugin is not a browser.
    // The PKCE verifier protects the code.
    static async Task<IResult> CreateToken(
        HttpRequest request,
        PluginAuthorizationCodes authorizationCodes,
        IUnitOfWorkMediator mediator,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "invalid_request" });

        var form = await request.ReadFormAsync(cancellationToken);
        var code = form["code"].ToString();
        var codeVerifier = form["code_verifier"].ToString();
        var redirectUri = form["redirect_uri"].ToString();

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(codeVerifier) || string.IsNullOrEmpty(redirectUri))
            return Results.BadRequest(new { error = "invalid_request" });

        var user = authorizationCodes.Redeem(code, redirectUri, codeVerifier);
        if (user is null)
        {
            logger.Warning("Plugin token request with a code that is not valid");
            return Results.BadRequest(new { error = "invalid_grant" });
        }

        var response = await mediator.Send(new CreatePluginTokenCommand(user.Cid, user.Name), cancellationToken);
        return Results.Ok(response);
    }

    static IResult GetUser(ClaimsPrincipal user)
    {
        var expires = DateTimeOffset.Parse(user.FindFirstValue(PluginTokenAuthenticationHandler.ExpiresClaimType)!);
        return Results.Ok(new PluginUserDto(user.GetCid()!, user.GetName(), expires));
    }

    static async Task<IResult> Logout(HttpRequest request, IUnitOfWorkMediator mediator, CancellationToken cancellationToken)
    {
        // The policy accepted the token, so the header has it
        var token = request.Headers.Authorization.ToString()["Bearer ".Length..].Trim();
        await mediator.Send(new RevokePluginTokenCommand(token), cancellationToken);
        return Results.NoContent();
    }
}
