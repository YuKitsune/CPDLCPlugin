using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CPDLCServer.Cpdlc.Dialogues;

public class DialogueConfiguration : IEntityTypeConfiguration<Dialogue>
{
    public void Configure(EntityTypeBuilder<Dialogue> builder)
    {
        builder.ToTable("Dialogues");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.AircraftCallsign);
        builder.Property(d => d.Opened);
        builder.Property(d => d.Closed);
        builder.Property(d => d.Archived);

        builder.HasMany(d => d.Messages)
            .WithOne()
            .HasForeignKey(m => m.DialogueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(d => d.Messages)
            .HasField("_messages")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(d => d.AircraftCallsign);
    }
}
