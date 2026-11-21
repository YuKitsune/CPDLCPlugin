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

        builder.Property(s => s.ServerName).HasMaxLength(ServerSettings.MaxServerNameLength);
        builder.Property(s => s.Motd).HasMaxLength(ServerSettings.MaxMotdLength);
        builder.Property(s => s.OperatorName).HasMaxLength(ServerSettings.MaxOperatorNameLength);
        builder.Property(s => s.PrivacyContact).HasMaxLength(ServerSettings.MaxPrivacyContactLength);
    }
}
