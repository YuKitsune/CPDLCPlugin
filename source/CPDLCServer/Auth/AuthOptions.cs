namespace CPDLCServer.Auth;

// The AuthType environment variable is BuiltIn or VATSIM. The server does not start without it.
public record AuthOptions(AuthType Type)
{
    public bool IsBuiltIn => Type == AuthType.BuiltIn;
    public bool IsVatsim => Type == AuthType.Vatsim;

    public static AuthOptions FromConfiguration(IConfiguration configuration)
    {
        var value = configuration["AuthType"];

        // Enum.TryParse also accepts numbers, so check that the value is a name
        if (string.IsNullOrWhiteSpace(value) ||
            !Enum.TryParse<AuthType>(value, ignoreCase: true, out var type) ||
            !Enum.GetNames<AuthType>().Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Set the AuthType environment variable to BuiltIn or VATSIM");
        }

        return new AuthOptions(type);
    }
}
