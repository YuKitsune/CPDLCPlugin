using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CPDLCServer.Cpdlc.Uplinks;

public class MessageIdSequenceConfiguration : IEntityTypeConfiguration<MessageIdSequence>
{
    public void Configure(EntityTypeBuilder<MessageIdSequence> builder)
    {
        builder.ToTable("MessageIdSequences");

        builder.HasKey(s => new { s.AcarsConnectionId, s.Callsign });

        builder.Property(s => s.AcarsConnectionId);
        builder.Property(s => s.Callsign);
        builder.Property(s => s.LastMessageId);
    }
}
