using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CPDLCPlugin.Messages;
using CPDLCPlugin.Server;
using MediatR;

namespace CPDLCPlugin.ViewModels;

public partial class SetupViewModel : ObservableObject,
    IRecipient<ConnectedNotification>,
    IRecipient<DisconnectedNotification>,
    IRecipient<AuthenticationChangedNotification>
{
    readonly IMediator _mediator;
    readonly IErrorReporter _errorReporter;
    readonly IWindowHandle _windowHandle;
    readonly AuthenticationState _authenticationState;
    readonly IGuiInvoker _guiInvoker;

    [ObservableProperty] string serverEndpoint;
    [ObservableProperty] string[] availableStationIdentifiers;
    [ObservableProperty] string selectedStationIdentifier;
    [ObservableProperty] bool connected;
    [ObservableProperty] bool authenticated;
    [ObservableProperty] string authenticationStatus = string.Empty;
    [ObservableProperty] bool authenticating;

    public SetupViewModel(
        IMediator mediator,
        IErrorReporter errorReporter,
        IWindowHandle windowHandle,
        AuthenticationState authenticationState,
        IGuiInvoker guiInvoker,
        string serverEndpoint,
        string[] availableStationIdentifiers,
        string selectedStationIdentifier,
        bool connected)
    {
        _mediator = mediator;
        _errorReporter = errorReporter;
        _windowHandle = windowHandle;
        _authenticationState = authenticationState;
        _guiInvoker = guiInvoker;
        ServerEndpoint = serverEndpoint;
        AvailableStationIdentifiers = availableStationIdentifiers;
        SelectedStationIdentifier = selectedStationIdentifier;
        Connected = connected;
        UpdateAuthenticationStatus();

        // Register for connection notifications
        WeakReferenceMessenger.Default.Register<ConnectedNotification>(this);
        WeakReferenceMessenger.Default.Register<DisconnectedNotification>(this);
        WeakReferenceMessenger.Default.Register<AuthenticationChangedNotification>(this);
    }

    [RelayCommand]
    async Task ConnectOrDisconnect()
    {
        try
        {
            if (Connected)
            {
                await _mediator.Send(new DisconnectRequest());
            }
            else
            {
                await _mediator.Send(new ConnectRequest(ServerEndpoint, SelectedStationIdentifier));
            }
        }
        catch (Exception e)
        {
            _errorReporter.ReportError(e);
        }
    }

    [RelayCommand]
    async Task AuthenticateOrLogOut()
    {
        try
        {
            Authenticating = true;

            if (Authenticated)
            {
                await _mediator.Send(new LogOutRequest(ServerEndpoint));
            }
            else
            {
                await _mediator.Send(new AuthenticateRequest(ServerEndpoint));
            }
        }
        catch (Exception e)
        {
            _errorReporter.ReportError(e);
        }
        finally
        {
            Authenticating = false;
        }
    }

    [RelayCommand]
    void Close()
    {
        _windowHandle.Close();
    }

    // The status is for the server in the text box, so it changes when the URL changes
    partial void OnServerEndpointChanged(string value)
    {
        UpdateAuthenticationStatus();
    }

    void UpdateAuthenticationStatus()
    {
        Uri server;
        try
        {
            server = ServerAddress.GetBaseUri(ServerEndpoint);
        }
        catch (ArgumentException)
        {
            Authenticated = false;
            AuthenticationStatus = "The server URL is not valid";
            return;
        }

        var user = _authenticationState.GetUser(server);
        Authenticated = user is not null;
        AuthenticationStatus = user is null
            ? "Not authenticated"
            : $"Authenticated as {user.Name} ({user.Cid})";
    }

    public void Receive(ConnectedNotification message)
    {
        Connected = true;
    }

    public void Receive(DisconnectedNotification message)
    {
        Connected = false;
    }

    // The notification can come from a background thread
    public void Receive(AuthenticationChangedNotification message)
    {
        _guiInvoker.InvokeOnGUI(_ => UpdateAuthenticationStatus());
    }
}
