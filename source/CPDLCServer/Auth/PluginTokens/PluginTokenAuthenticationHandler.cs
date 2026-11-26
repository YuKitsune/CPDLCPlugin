using System.Security.Claims;
using System.Text.Encodings.Web;
using CPDLCServer.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CPDLCServer.Auth.PluginTokens;

/// <summary>
/// Authenticates the plugin with its token. The token is in the Authorization header ("Bearer ...").
/// For the hub, the token can also be in the access_token query parameter, because a browser WebSocket cannot send headers.
/// </summary>
public class PluginTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IPluginTokenRepository pluginTokenRepository,
    IUnitOfWorkMediator mediator,
    IClock clock)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string AuthenticationScheme = "PluginToken";

    // The authorization policy of the hub and of the plugin endpoints
    public const string PolicyName = "Plugin";
    public const string ExpiresClaimType = "plugin_token_expires";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = ReadToken();
        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.NoResult();

        var pluginToken = await pluginTokenRepository.FindByHash(PluginTokenHash.Compute(token), Context.RequestAborted);
        if (pluginToken is null)
            return AuthenticateResult.Fail("The token is not valid");

        var now = clock.UtcNow();
        if (pluginToken.Expires <= now)
            return AuthenticateResult.Fail("The token has expired");

        if (pluginToken.LastUsed is null || now - pluginToken.LastUsed >= PluginToken.LastUsedPrecision)
            await mediator.Send(new RecordPluginTokenUseCommand(pluginToken.Id), Context.RequestAborted);

        var identity = CurrentUser.CreateIdentity(pluginToken.Cid, pluginToken.Name, AuthenticationScheme);
        identity.AddClaim(new Claim(ExpiresClaimType, pluginToken.Expires.ToString("O")));

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), AuthenticationScheme));
    }

    string? ReadToken()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authorization["Bearer ".Length..].Trim();

        if (Request.Path.StartsWithSegments("/hubs"))
            return Request.Query["access_token"].ToString();

        return null;
    }
}
