using CPDLCServer.AcarsConnections.ConnectionSettings;
using CPDLCServer.AdscContracts;
using CPDLCServer.AdscContracts.Reports;
using CPDLCServer.Auth.Users;
using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Settings;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Persistence;

// The provider-specific contexts (SqliteCpdlcDbContext, PostgreSqlCpdlcDbContext) derive from this context,
// because EF Core keeps one set of migrations for each context type
public class CpdlcDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<AircraftConnection> AircraftConnections => Set<AircraftConnection>();
    public DbSet<Dialogue> Dialogues => Set<Dialogue>();
    public DbSet<AdscContract> AdscContracts => Set<AdscContract>();
    public DbSet<AircraftAdscReport> AdscReports => Set<AircraftAdscReport>();
    public DbSet<MessageIdSequence> MessageIdSequences => Set<MessageIdSequence>();
    public DbSet<ServerSettings> ServerSettings => Set<ServerSettings>();
    public DbSet<AcarsConnectionSettings> AcarsConnections => Set<AcarsConnectionSettings>();
    public DbSet<User> Users => Set<User>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot compare or sort DateTimeOffset values in a query, so store them as UTC ticks
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksConverter>();
        configurationBuilder.Properties<TimeSpan>().HaveConversion<long>();

        // Store the enums as text, so that a change to the order of the members does not change the data
        configurationBuilder.Properties<AdscContractState>().HaveConversion<string>();
        configurationBuilder.Properties<AdscReportSource>().HaveConversion<string>();
        configurationBuilder.Properties<AlertType>().HaveConversion<string>();
        configurationBuilder.Properties<ConnectionState>().HaveConversion<string>();
        configurationBuilder.Properties<CpdlcDownlinkResponseType>().HaveConversion<string>();
        configurationBuilder.Properties<CpdlcUplinkResponseType>().HaveConversion<string>();
        configurationBuilder.Properties<DataAuthorityState>().HaveConversion<string>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AircraftConnectionConfiguration());
        modelBuilder.ApplyConfiguration(new DialogueConfiguration());
        modelBuilder.ApplyConfiguration(new CpdlcMessageConfiguration());
        modelBuilder.ApplyConfiguration(new UplinkMessageConfiguration());
        modelBuilder.ApplyConfiguration(new DownlinkMessageConfiguration());
        modelBuilder.ApplyConfiguration(new AdscContractConfiguration());
        modelBuilder.ApplyConfiguration(new AircraftAdscReportConfiguration());
        modelBuilder.ApplyConfiguration(new MessageIdSequenceConfiguration());
        modelBuilder.ApplyConfiguration(new ServerSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new AcarsConnectionSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
    }
}
