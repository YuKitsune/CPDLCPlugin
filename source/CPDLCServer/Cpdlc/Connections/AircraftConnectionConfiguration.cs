using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CPDLCServer.Cpdlc.Connections;

public class AircraftConnectionConfiguration : IEntityTypeConfiguration<AircraftConnection>
{
    public const string IdProperty = "Id";

    public void Configure(EntityTypeBuilder<AircraftConnection> builder)
    {
        builder.ToTable("AircraftConnections");

        // The domain model has no ID, so the key is a shadow property
        builder.Property<int>(IdProperty);
        builder.HasKey(IdProperty);

        builder.Property(c => c.Callsign);
        builder.Property(c => c.AcarsConnectionId);
        builder.Property(c => c.StationId);
        builder.Property(c => c.DataAuthorityState);
        builder.Property(c => c.ConnectionState);
        builder.Property(c => c.LogonRequested);
        builder.Property(c => c.LogonAccepted);
        builder.Property(c => c.LastSeen);
        builder.Property(c => c.NextDataAuthority);
        builder.Property(c => c.ExpectedTransferTime);
        builder.Property(c => c.DidSentNextDataAuthorityMessage);

        builder.HasIndex(c => new { c.Callsign, c.AcarsConnectionId });
    }
}
