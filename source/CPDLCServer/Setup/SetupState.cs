using System.Security.Cryptography;
using System.Text;
using CPDLCServer.Auth.Users;

namespace CPDLCServer.Setup;

// The setup creates the first user. The server is configured when a user exists.
public class SetupState(IServiceScopeFactory serviceScopeFactory)
{
    volatile bool _isConfigured;

    // Only a person who can read the server log has this token, so only that person can create the first user.
    // The server makes a new token each time that it starts.
    public string SetupToken { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    public bool IsConfigured => _isConfigured;

    public async Task Load(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        _isConfigured = await userRepository.Any(cancellationToken);
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

    public void MarkAdminCreated()
    {
        _isConfigured = true;
    }
}
