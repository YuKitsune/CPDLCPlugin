using CPDLCServer.Auth.Administrators;

namespace CPDLCServer.Setup;

// The setup adds the first administrator. The server is configured when an administrator exists.
public class SetupState(IServiceScopeFactory serviceScopeFactory)
{
    volatile bool _isConfigured;

    public bool IsConfigured => _isConfigured;

    public async Task Load(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var administratorRepository = scope.ServiceProvider.GetRequiredService<IAdministratorRepository>();

        _isConfigured = await administratorRepository.Any(cancellationToken);
    }

    public void MarkConfigured()
    {
        _isConfigured = true;
    }
}
