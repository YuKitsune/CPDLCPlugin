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
    // A concurrency token on each entity that a unit of work can change.
    // When two units of work change the same entity at the same time, the second commit fails, and the unit of work
    // runs again with the new data (see UnitOfWorkMediator).
    public const string VersionProperty = "Version";

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

        // The messages have no version. A change to a message is a change to its dialogue (see SaveChangesAsync).
        Type[] versionedEntityTypes =
        [
            typeof(AircraftConnection),
            typeof(Dialogue),
            typeof(AdscContract),
            typeof(AircraftAdscReport),
            typeof(MessageIdSequence),
            typeof(ServerSettings),
            typeof(AcarsConnectionSettings),
            typeof(User)
        ];

        foreach (var entityType in versionedEntityTypes)
        {
            modelBuilder.Entity(entityType)
                .Property<int>(VersionProperty)
                .IsConcurrencyToken();
        }
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        IncrementVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Increments the version of each changed entity.
    // A dialogue is changed when one of its messages is added, changed or removed, so that two units of work cannot
    // change the messages of the same dialogue at the same time.
    void IncrementVersions()
    {
        ChangeTracker.DetectChanges();

        var dialogueIdsWithChangedMessages = ChangeTracker.Entries<CpdlcMessage>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity.DialogueId)
            .ToHashSet();

        var changedEntries = ChangeTracker.Entries()
            .Where(e => e.Metadata.FindProperty(VersionProperty) is not null)
            .Where(e =>
                e.State == EntityState.Modified ||
                (e.State == EntityState.Unchanged && e.Entity is Dialogue dialogue && dialogueIdsWithChangedMessages.Contains(dialogue.Id)))
            .ToArray();

        foreach (var entry in changedEntries)
        {
            var version = entry.Property(VersionProperty);
            version.CurrentValue = (int)version.OriginalValue! + 1;
        }
    }
}
