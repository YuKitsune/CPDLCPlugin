using Npgsql;

namespace CPDLCServer.Persistence;

public static class PostgreSqlConnectionString
{
    // Npgsql accepts only "Key=Value" connection strings.
    // Some platforms, for example DigitalOcean, give a URL (postgresql://user:password@host:port/database?sslmode=require).
    // This changes a URL to a "Key=Value" connection string, and does not change other values.
    public static string Parse(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return value;

        var uri = new Uri(value);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'))
        };

        if (uri.Port > 0)
            builder.Port = uri.Port;

        var userInfo = uri.UserInfo.Split(':', 2);
        builder.Username = Uri.UnescapeDataString(userInfo[0]);
        if (userInfo.Length > 1)
            builder.Password = Uri.UnescapeDataString(userInfo[1]);

        foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = parameter.Split('=', 2);
            if (parts.Length == 2 && parts[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                builder.SslMode = Enum.Parse<SslMode>(parts[1].Replace("-", string.Empty), ignoreCase: true);
        }

        return builder.ConnectionString;
    }
}
