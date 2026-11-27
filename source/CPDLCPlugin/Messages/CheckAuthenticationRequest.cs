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
    AuthenticationStore authenticationStore,
    ILogger logger)
    : IRequestHandler<CheckAuthenticationRequest, string?>
{
    public async Task<string?> Handle(CheckAuthenticationRequest request, CancellationToken cancellationToken)
    {
        var server = ServerAddress.GetBaseUri(request.ServerEndpoint);

        // The store does not return an expired token, so the server gets only a token that is not expired
        var token = authenticationStore.Get(server);
        if (token is null)
            return null;

        var user = await authenticator.GetUser(server, token.AccessToken, cancellationToken);
        if (user is not null)
            return token.AccessToken;

        logger.Information("The server {Server} did not accept the saved token. Deleting it", server);
        authenticationStore.Delete(server);
        WeakReferenceMessenger.Default.Send(new AuthenticationChangedNotification());

        return null;
    }
}
