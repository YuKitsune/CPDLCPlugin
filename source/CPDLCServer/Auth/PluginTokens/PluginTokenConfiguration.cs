using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CPDLCServer.Auth.PluginTokens;

public class PluginTokenConfiguration : IEntityTypeConfiguration<PluginToken>
{
    public void Configure(EntityTypeBuilder<PluginToken> builder)
    {
        builder.ToTable("PluginTokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.Cid);
    }
}
