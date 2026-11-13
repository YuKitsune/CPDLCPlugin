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
        builder.Property(c => c.DefaultReportingInterval);
        builder.Property(c => c.ReportingInterval);
        builder.Property(c => c.DefaultContractState);
        builder.Property(c => c.SelectedReportingInterval);
        builder.Property(c => c.SelectedContractState);
        builder.Property(c => c.SelectedContractEnds);
        builder.Property(c => c.DemandContractState);
        builder.Property(c => c.LastActivity);

        builder.HasIndex(c => new { c.Callsign, c.StationId });
    }
}
