using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CPDLCServer.AcarsConnections.ConnectionSettings;

public class AcarsConnectionSettingsConfiguration : IEntityTypeConfiguration<AcarsConnectionSettings>
{
    public void Configure(EntityTypeBuilder<AcarsConnectionSettings> builder)
    {
        builder.ToTable("AcarsConnections");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Type).HasConversion<string>();
        builder.Property(c => c.Url).HasConversion<string>();

        // The ACARS connection ID comes from the station identifier, so only one connection can use a station identifier
        builder.HasIndex(c => c.StationIdentifier).IsUnique();
    }
}
