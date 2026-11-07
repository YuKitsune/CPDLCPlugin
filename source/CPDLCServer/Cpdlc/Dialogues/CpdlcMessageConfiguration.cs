using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CPDLCServer.Cpdlc.Dialogues;

// Uplink and downlink messages are kept in one table
public class CpdlcMessageConfiguration : IEntityTypeConfiguration<CpdlcMessage>
{
    // The message ID comes from the aircraft or the server, so it is not unique. The key is a shadow property.
    public const string IdProperty = "Id";

    public void Configure(EntityTypeBuilder<CpdlcMessage> builder)
    {
        builder.ToTable("CpdlcMessages");

        builder.Property<long>(IdProperty);
        builder.HasKey(IdProperty);

        builder.HasDiscriminator<string>("Direction")
            .HasValue<UplinkMessage>("Uplink")
            .HasValue<DownlinkMessage>("Downlink");

        builder.Property(m => m.DialogueId);
        builder.Property(m => m.MessageId);
        builder.Property(m => m.MessageReference);
        builder.Property(m => m.Position);
        builder.Property(m => m.AlertType);
        builder.Property(m => m.Closed);
        builder.Property(m => m.Acknowledged);
        builder.Ignore(m => m.Time);
    }
}

public class UplinkMessageConfiguration : IEntityTypeConfiguration<UplinkMessage>
{
    public void Configure(EntityTypeBuilder<UplinkMessage> builder)
    {
        builder.Property(m => m.Recipient);
        builder.Property(m => m.SenderCallsign);
        builder.Property(m => m.ResponseType).HasColumnName("UplinkResponseType");
        builder.Property(m => m.Content).HasColumnName("Content");
        builder.Property(m => m.Sent);
        builder.Property(m => m.ClosedManually);
        builder.Property(m => m.IsPilotLate);
        builder.Property(m => m.IsTransmissionFailed);
    }
}

public class DownlinkMessageConfiguration : IEntityTypeConfiguration<DownlinkMessage>
{
    public void Configure(EntityTypeBuilder<DownlinkMessage> builder)
    {
        builder.Property(m => m.Sender);
        builder.Property(m => m.ResponseType).HasColumnName("DownlinkResponseType");
        builder.Property(m => m.Content).HasColumnName("Content");
        builder.Property(m => m.Received);
        builder.Property(m => m.IsControllerLate);
    }
}
