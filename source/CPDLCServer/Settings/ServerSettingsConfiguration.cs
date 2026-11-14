using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CPDLCServer.Settings;

// The table has one row
public class ServerSettingsConfiguration : IEntityTypeConfiguration<ServerSettings>
{
    public const string IdProperty = "Id";

    public void Configure(EntityTypeBuilder<ServerSettings> builder)
    {
        builder.ToTable("ServerSettings");

        // The settings have no ID, so the key is a shadow property
        builder.Property<int>(IdProperty);
        builder.HasKey(IdProperty);

        builder.Property(s => s.VatsimStatusUrl).HasConversion<string>();
    }
}
