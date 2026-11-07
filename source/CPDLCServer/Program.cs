using CPDLCServer.AcarsConnections;
using CPDLCServer.AdscContracts;
using CPDLCServer.Auth;
using CPDLCServer.Controllers;
using CPDLCServer.Cpdlc;
using CPDLCServer.Infrastructure;
using CPDLCServer.Logging;
using CPDLCServer.Persistence;
using CPDLCServer.Security;
using CPDLCServer.Settings;
using CPDLCServer.Setup;
using CPDLCServer.VatsimStatus;
using CPDLCServer.Web;
using DotNetEnv;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

// TODO:
// - Remove FlightSimulationNetwork variable
// - Aggregate controller connections for multiple FIRs
// - Introduce a Contracts project
// - Value types for IDs
// - Re-do dashboard

TryLoadEnvFile();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

ConfigureSerilog(builder);

// The data directory holds the SQLite database
var dataPath = GetDataPath(builder);

var databaseProvider = GetDatabaseProvider(builder);
var databasePath = Path.Combine(dataPath, "cpdlc.db");

builder.Services.AddInfrastructure();
builder.Services.AddPersistence(builder.Configuration, databaseProvider, databasePath);
builder.Services.AddSecurity(builder.Configuration);
builder.Services.AddSetup();
builder.Services.AddAuth();
builder.Services.AddSettings();
builder.Services.AddAcarsConnections();
builder.Services.AddAirTrafficControllers();
builder.Services.AddCpdlc();
builder.Services.AddAdscContracts();
builder.Services.AddVatsimStatus();
builder.Services.AddWeb();

var app = builder.Build();

await MigrateDatabase(app, databaseProvider, databasePath);
await SeedServerSettings(app);

var setupState = app.Services.GetRequiredService<SetupState>();
await setupState.Load(CancellationToken.None);
app.Lifetime.ApplicationStarted.Register(() => LogSetupUrl(app, setupState));

app.UseHttpsRedirection();
app.UseRouting();

app.UseMiddleware<RedirectToSetupMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapRazorPages();
app.MapHub<ControllerHub>("/hubs/controller");

app.MapGet("/health", () => Results.Ok());
app.MapGet("/logs/stream", LogStreamEndpoint.Handle).RequireAuthorization();

app.Run();
return;

void TryLoadEnvFile()
{
    // Only load .env file in development
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development")
        return;

    // Search for .env file in current directory and parent directories up to git root or filesystem root
    var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (currentDir != null)
    {
        var envPath = Path.Combine(currentDir.FullName, ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
            break;
        }

        // Stop at git root
        if (Directory.Exists(Path.Combine(currentDir.FullName, ".git")))
        {
            break;
        }

        currentDir = currentDir.Parent;
    }
}

DatabaseProvider GetDatabaseProvider(WebApplicationBuilder builder)
{
    var configuredProvider = builder.Configuration["DatabaseProvider"];
    if (string.IsNullOrWhiteSpace(configuredProvider))
        return DatabaseProvider.Sqlite;

    if (!Enum.TryParse<DatabaseProvider>(configuredProvider, ignoreCase: true, out var provider))
        throw new Exception($"The database provider {configuredProvider} is not supported. Use Sqlite or PostgreSql.");

    return provider;
}

string GetDataPath(WebApplicationBuilder builder)
{
    var configuredPath = builder.Configuration["DataPath"];
    if (!string.IsNullOrWhiteSpace(configuredPath))
        return Path.GetFullPath(configuredPath);

    // For local development, keep the data in the .data directory at the root of the repository
    if (builder.Environment.IsDevelopment())
        return Path.Combine(FindRepositoryRoot() ?? Directory.GetCurrentDirectory(), ".data");

    return "/data";
}

string? FindRepositoryRoot()
{
    var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (currentDir != null)
    {
        if (Directory.Exists(Path.Combine(currentDir.FullName, ".git")))
            return currentDir.FullName;

        currentDir = currentDir.Parent;
    }

    return null;
}

void LogSetupUrl(WebApplication app, SetupState setupState)
{
    if (setupState.IsConfigured)
        return;

    var setupPath = $"/setup?token={setupState.SetupToken}";

    var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses ?? [];
    if (addresses.Count == 0)
    {
        Log.Warning("The server is not set up. Open {SetupPath} on this server to set it up", setupPath);
        return;
    }

    foreach (var address in addresses)
    {
        // The server can listen on all addresses, for example in Docker. Show an address that a browser can open.
        var browserAddress = address
            .Replace("://[::]", "://localhost")
            .Replace("://0.0.0.0", "://localhost")
            .Replace("://+", "://localhost")
            .Replace("://*", "://localhost")
            .TrimEnd('/');

        Log.Warning("The server is not set up. Open {SetupUrl} to set it up", browserAddress + setupPath);
    }
}

async Task MigrateDatabase(WebApplication app, DatabaseProvider provider, string databasePath)
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CpdlcDbContext>();

    if (provider == DatabaseProvider.Sqlite)
    {
        // SQLite makes the database file, but not the directory
        var databaseDirectory = Path.GetDirectoryName(databasePath);
        if (databaseDirectory is not null)
            Directory.CreateDirectory(databaseDirectory);

        Log.Information("Using SQLite database {DatabasePath}", databasePath);
    }
    else
    {
        // Do not write the connection string to the log, because it contains the password
        var connection = dbContext.Database.GetDbConnection();
        Log.Information("Using PostgreSQL database {Database} on {Host}", connection.Database, connection.DataSource);
    }

    await dbContext.Database.MigrateAsync();
}

// Adds the default settings to the database if they do not exist already
async Task SeedServerSettings(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<ServerSettingsProvider>().SeedDefaults(CancellationToken.None);
    await scope.ServiceProvider.GetRequiredService<UnitOfWork>().Commit(CancellationToken.None);
}

void ConfigureSerilog(WebApplicationBuilder builder)
{
    if (!Enum.TryParse<LogEventLevel>(builder.Configuration["Logging:Level"], out var logLevel))
    {
        logLevel = LogEventLevel.Information;
    }

    // Keeps the recent log entries for the Logs page
    var logBuffer = new LogBuffer();

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Is(logLevel)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.Sink(new LogBufferSink(logBuffer))
        .CreateLogger();

    builder.Host.UseSerilog();

    builder.Services.AddSingleton(Log.Logger);
    builder.Services.AddSingleton(logBuffer);
}
