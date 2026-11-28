using System.Text.Json;
using CPDLC.Messages;
using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
        builder.Property(m => m.Elements)
            .HasColumnName("Elements")
            .HasConversion(ElementsConversion.Converter<UplinkMessageElement>(), ElementsConversion.Comparer<UplinkMessageElement>());
        builder.Property(m => m.Sent);
        builder.Property(m => m.ClosedManually);
        builder.Property(m => m.IsPilotLate);
        builder.Property(m => m.DidTransmissionFail);
        builder.Property(m => m.CanAction);
        builder.Property(m => m.Actioned);
    }
}

public class DownlinkMessageConfiguration : IEntityTypeConfiguration<DownlinkMessage>
{
    public void Configure(EntityTypeBuilder<DownlinkMessage> builder)
    {
        builder.Property(m => m.Sender);
        builder.Property(m => m.ResponseType).HasColumnName("DownlinkResponseType");
        builder.Property(m => m.Elements)
            .HasColumnName("Elements")
            .HasConversion(ElementsConversion.Converter<DownlinkMessageElement>(), ElementsConversion.Comparer<DownlinkMessageElement>());
        builder.Property(m => m.Received);
        builder.Property(m => m.IsControllerLate);
    }
}

// The elements of a message are kept as JSON text, so that SQLite and PostgreSQL keep them in the same way
static class ElementsConversion
{
    public static ValueConverter<IReadOnlyList<T>, string> Converter<T>()
    {
        return new ValueConverter<IReadOnlyList<T>, string>(
            elements => JsonSerializer.Serialize(elements, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<T[]>(json, (JsonSerializerOptions?)null)!);
    }

    // The elements do not change after the message is made, so the JSON text is enough to compare them
    public static ValueComparer<IReadOnlyList<T>> Comparer<T>()
    {
        return new ValueComparer<IReadOnlyList<T>>(
            (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
            elements => JsonSerializer.Serialize(elements, (JsonSerializerOptions?)null).GetHashCode(),
            elements => elements);
    }
}
