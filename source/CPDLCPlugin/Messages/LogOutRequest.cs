using CommunityToolkit.Mvvm.Messaging;
using CPDLCPlugin.Server;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

// Revokes the token on the CPDLC server, and deletes it from this computer
public record LogOutRequest(string ServerEndpoint) : IRequest;

public class LogOutRequestHandler(
    PluginAuthenticator authenticator,
    TokenStore tokenStore,
    AuthenticationState authenticationState,
    ILogger logger)
    : IRequestHandler<LogOutRequest>
{
    public async Task Handle(LogOutRequest request, CancellationToken cancellationToken)
    {
        var server = ServerAddress.GetBaseUri(request.ServerEndpoint);

        var token = tokenStore.Get(server);
        tokenStore.Delete(server);
        authenticationState.Set(server, null);
        WeakReferenceMessenger.Default.Send(new AuthenticationChangedNotification());

        if (token is null)
            return;

        try
        {
            await authenticator.Logout(server, token, cancellationToken);
        }
        catch (Exception ex)
        {
            // The token is deleted from this computer. It expires on the server later.
            logger.Warning(ex, "Failed to revoke the token on {Server}", server);
        }

        logger.Information("Logged out from {Server}", server);
    }
}
