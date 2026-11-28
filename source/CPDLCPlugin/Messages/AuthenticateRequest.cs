using CommunityToolkit.Mvvm.Messaging;
using CPDLCPlugin.Server;
using MediatR;
using Serilog;
using vatsys;

namespace CPDLCPlugin.Messages;

// Opens the browser, so that the user can log in to the CPDLC server with VATSIM
public record AuthenticateRequest(string ServerEndpoint) : IRequest;

public class AuthenticateRequestHandler(
    PluginAuthenticator authenticator,
    AuthenticationStore authenticationStore,
    ILogger logger)
    : IRequestHandler<AuthenticateRequest>
{
    public async Task Handle(AuthenticateRequest request, CancellationToken cancellationToken)
    {
        var server = ServerAddress.GetBaseUri(request.ServerEndpoint);

        // The CID from vatSys. It is empty if the user did not enter it.
        var response = await authenticator.Authenticate(server, Network.ControllerId, cancellationToken);
        authenticationStore.Save(server, new AuthenticationToken(response.AccessToken, response.Cid, response.Expires));

        logger.Information("Authenticated with {Server} as {Cid}", server, response.Cid);
        WeakReferenceMessenger.Default.Send(new AuthenticationChangedNotification());
    }
}
