using System.Security.Cryptography;
using System.Text;
using CPDLCServer.Auth.Administrators;

namespace CPDLCServer.Setup;

// The setup adds the first administrator. The server is configured when an administrator exists.
public class SetupState(IServiceScopeFactory serviceScopeFactory)
{
    volatile bool _isConfigured;

    // With the BuiltIn login, only a person who can read the server log has this token, so only that person can add
    // the first administrator. The server makes a new token each time that it starts.
    public string SetupToken { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    public bool IsConfigured => _isConfigured;

    public async Task Load(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var administratorRepository = scope.ServiceProvider.GetRequiredService<IAdministratorRepository>();

        _isConfigured = await administratorRepository.Any(cancellationToken);
    }

    public bool IsValidSetupToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        // Compare in constant time, so that the time of the comparison does not show the token
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(token),
            Encoding.UTF8.GetBytes(SetupToken));
    }

    public void MarkConfigured()
    {
        _isConfigured = true;
    }
}
