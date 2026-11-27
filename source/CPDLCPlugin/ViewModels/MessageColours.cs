using CPDLCPlugin.Extensions;
using CPDLCServer.Contracts;

namespace CPDLCPlugin.ViewModels;

// The colours of a message. The views change each colour to a brush with the MessageColourToBrushConverter.
public enum MessageColour
{
    MessageBackground,
    Closed,
    Urgent,
    Failed,
    PilotLate,
    ControllerLate,
    Downlink,
    Uplink
}

public class MessageColours
{
    public static ColorPair GetMessageColors(CpdlcMessageDto message)
    {
        const MessageColour background = MessageColour.MessageBackground;

        if (IsUrgent(message))
        {
            return new ColorPair(background, MessageColour.Urgent).InvertIf(!message.IsAcknowledged);
        }

        if (IsFailed(message))
        {
            // Failed message background is CPDLCClosedColor.
            return new ColorPair(MessageColour.Closed, MessageColour.Failed).InvertIf(!message.IsAcknowledged);
        }

        if (IsClosed(message) && message is UplinkMessageDto)
        {
            return new ColorPair(background, MessageColour.Closed);
        }

        if (IsClosed(message) && message is DownlinkMessageDto)
        {
            return new ColorPair(background, MessageColour.Closed).InvertIf(!message.IsAcknowledged);
        }

        // Special closed timeout: Special uplink that timed out before acknowledgement
        // Shows Normal video (not inverted) with pilot late color
        if (message is UplinkMessageDto { IsClosed: true, IsPilotLate: true, IsAcknowledged: false } specialLate && specialLate.IsSpecial())
        {
            return new ColorPair(background, MessageColour.PilotLate);
        }

        // Special Closed: For a special Uplink Message that is closed by itself
        // After acknowledgement (even if it timed out), show Normal video with CPDLCClosedColor
        // Before acknowledgement (and hasn't timed out), show Inverse video with CPDLCClosedColor
        if (message is UplinkMessageDto { IsClosed: true } ul && ul.IsSpecial())
        {
            return new ColorPair(background, MessageColour.Closed).InvertIf(!ul.IsAcknowledged);
        }

        if (IsPilotLate(message))
        {
            // Time Out (pilot or Controller) message background is CPDLCClosedColor
            return new ColorPair(MessageColour.Closed, MessageColour.PilotLate).InvertIf(!message.IsAcknowledged);
        }

        if (IsControllerLate(message))
        {
            // Time Out (pilot or Controller) message background is CPDLCClosedColor
            return new ColorPair(MessageColour.Closed, MessageColour.ControllerLate).InvertIf(!message.IsAcknowledged);
        }

        if (message is DownlinkMessageDto)
        {
            return new ColorPair(background, MessageColour.Downlink).InvertIf(!message.IsAcknowledged);
        }

        if (message is UplinkMessageDto)
        {
            return new ColorPair(background, MessageColour.Uplink).InvertIf(!message.IsAcknowledged);
        }

        return new ColorPair(background, MessageColour.Closed);

        // Only messages with a high alert use the urgent colour
        bool IsUrgent(CpdlcMessageDto message)
        {
            return message.AlertType == AlertType.High;
        }

        bool IsFailed(CpdlcMessageDto message)
        {
            return message switch
            {
                DownlinkMessageDto downlinkMessage => downlinkMessage.HasElement(DownlinkElementIds.Error),
                UplinkMessageDto uplinkMessage => uplinkMessage.IsTransmissionFailed,
                _ => false
            };
        }

        bool IsPilotLate(CpdlcMessageDto message)
        {
            return message switch
            {
                UplinkMessageDto uplinkMessage => uplinkMessage.IsPilotLate,
                _ => false
            };
        }

        bool IsControllerLate(CpdlcMessageDto message)
        {
            return message switch
            {
                DownlinkMessageDto downlinkMessage => downlinkMessage.IsControllerLate,
                _ => false
            };
        }

        bool IsClosed(CpdlcMessageDto message)
        {
            return message switch
            {
                DownlinkMessageDto downlinkMessage => downlinkMessage.IsClosed,
                UplinkMessageDto uplinkMessage => uplinkMessage.IsClosed,
                _ => false
            };
        }
    }

    public record ColorPair(MessageColour Background, MessageColour Foreground)
    {
        public ColorPair InvertIf(bool condition)
        {
            return condition
                ? Invert()
                : this;
        }

        public ColorPair Invert() => new(Foreground, Background);
    }
}
