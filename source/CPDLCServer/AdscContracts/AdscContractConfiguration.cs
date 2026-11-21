using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CPDLCServer.AdscContracts;

public class AdscContractConfiguration : IEntityTypeConfiguration<AdscContract>
{
    public const string IdProperty = "Id";

    public void Configure(EntityTypeBuilder<AdscContract> builder)
    {
        builder.ToTable("AdscContracts");

        // The domain model has no ID, so the key is a shadow property
        builder.Property<int>(IdProperty);
        builder.HasKey(IdProperty);

        builder.Property(c => c.Callsign);
        builder.Property(c => c.StationId);
        builder.Property(c => c.Created);
        builder.Property(c => c.DefaultReportingInterval);
        builder.Property(c => c.ReportingInterval);
        builder.Property(c => c.DefaultContractState);
        builder.Property(c => c.SelectedReportingInterval);
        builder.Property(c => c.SelectedContractState);
        builder.Property(c => c.DemandContractState);
        builder.Property(c => c.LastActivity);
        builder.Property(c => c.LastReportReceived);
        builder.Property(c => c.ReportingSource);

        // One contract for each aircraft and ATSU.
        // Two units of work that add the same contract at the same time cannot both commit.
        builder.HasIndex(c => new { c.Callsign, c.StationId }).IsUnique();
    }
}
