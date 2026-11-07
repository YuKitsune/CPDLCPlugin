using CPDLCServer.Persistence;
using Npgsql;

namespace CPDLCServer.Tests.Persistence;

public class PostgreSqlConnectionStringTests
{
    [Fact]
    public void Parse_WithUrl_ReturnsKeyValueConnectionString()
    {
        // Act
        var connectionString = PostgreSqlConnectionString.Parse(
            "postgresql://cpdlc:p%40ss%3Aword@db.example.com:25060/cpdlc?sslmode=require");

        // Assert
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.Equal("db.example.com", builder.Host);
        Assert.Equal(25060, builder.Port);
        Assert.Equal("cpdlc", builder.Database);
        Assert.Equal("cpdlc", builder.Username);
        Assert.Equal("p@ss:word", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
    }

    [Fact]
    public void Parse_WithPostgresScheme_ReturnsKeyValueConnectionString()
    {
        // Act
        var connectionString = PostgreSqlConnectionString.Parse("postgres://cpdlc:secret@localhost/cpdlc");

        // Assert
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.Equal("localhost", builder.Host);
        Assert.Equal("cpdlc", builder.Database);
        Assert.Equal("secret", builder.Password);
    }

    [Fact]
    public void Parse_WithKeyValueConnectionString_ReturnsTheSameValue()
    {
        // Arrange
        const string value = "Host=localhost;Database=cpdlc;Username=cpdlc;Password=secret";

        // Act & Assert
        Assert.Equal(value, PostgreSqlConnectionString.Parse(value));
    }
}
