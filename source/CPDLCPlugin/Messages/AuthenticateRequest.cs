using CommunityToolkit.Mvvm.Messaging;
using CPDLCPlugin.Server;
using CPDLCServer.Contracts;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

// Opens the browser, so that the user can log in to the CPDLC server with VATSIM
public record AuthenticateRequest(string ServerEndpoint) : IRequest;

public class AuthenticateRequestHandler(
    PluginAuthenticator authenticator,
    TokenStore tokenStore,
    AuthenticationState authenticationState,
    ILogger logger)
    : IRequestHandler<AuthenticateRequest>
{
    public async Task Handle(AuthenticateRequest request, CancellationToken cancellationToken)
    {
        var server = ServerAddress.GetBaseUri(request.ServerEndpoint);

        var response = await authenticator.Authenticate(server, cancellationToken);
        tokenStore.Save(server, response.AccessToken);
        authenticationState.Set(server, new PluginUserDto(response.Cid, response.Name, response.Expires));

        logger.Information("Authenticated with {Server} as {Name} ({Cid})", server, response.Name, response.Cid);
        WeakReferenceMessenger.Default.Send(new AuthenticationChangedNotification());
    }
}
