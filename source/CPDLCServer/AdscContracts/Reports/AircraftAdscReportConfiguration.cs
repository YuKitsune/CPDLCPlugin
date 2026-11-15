using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CPDLCServer.AdscContracts.Reports;

public class AircraftAdscReportConfiguration : IEntityTypeConfiguration<AircraftAdscReport>
{
    public const string IdProperty = "Id";

    public void Configure(EntityTypeBuilder<AircraftAdscReport> builder)
    {
        builder.ToTable("AdscReports");

        // The domain model has no ID, so the key is a shadow property
        builder.Property<long>(IdProperty);
        builder.HasKey(IdProperty);

        builder.Property(r => r.Callsign);
        builder.Property(r => r.StationId);
        builder.Property(r => r.Received);

        builder.ComplexProperty(r => r.Report, report =>
        {
            report.Property(r => r.Timestamp).HasColumnName("Timestamp");
            report.Property(r => r.Latitude).HasColumnName("Latitude");
            report.Property(r => r.Longitude).HasColumnName("Longitude");
            report.Property(r => r.Altitude).HasColumnName("Altitude");
            report.Property(r => r.Source).HasColumnName("Source");
        });

        builder.HasIndex(r => new { r.Callsign, r.StationId }).IsUnique();
    }
}
