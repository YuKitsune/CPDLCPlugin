using CommunityToolkit.Mvvm.Messaging;
using CPDLCPlugin.Server;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

// Asks the CPDLC server if the saved token is valid. Returns the token, or null when the plugin is not authenticated.
// A token that the server does not accept is deleted.
public record CheckAuthenticationRequest(string ServerEndpoint) : IRequest<string?>;

public class CheckAuthenticationRequestHandler(
    PluginAuthenticator authenticator,
    TokenStore tokenStore,
    AuthenticationState authenticationState,
    ILogger logger)
    : IRequestHandler<CheckAuthenticationRequest, string?>
{
    public async Task<string?> Handle(CheckAuthenticationRequest request, CancellationToken cancellationToken)
    {
        var server = ServerAddress.GetBaseUri(request.ServerEndpoint);

        var token = tokenStore.Get(server);
        var user = token is null ? null : await authenticator.GetUser(server, token, cancellationToken);

        if (token is not null && user is null)
        {
            logger.Information("The server {Server} did not accept the saved token. Deleting it", server);
            tokenStore.Delete(server);
            token = null;
        }

        authenticationState.Set(server, user);
        WeakReferenceMessenger.Default.Send(new AuthenticationChangedNotification());

        return token;
    }
}
