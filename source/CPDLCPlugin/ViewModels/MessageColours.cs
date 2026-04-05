using System.Windows.Media;
using CPDLCServer.Contracts;

namespace CPDLCPlugin.ViewModels;

public class MessageColours
{
    public static ColorPair GetMessageColors(CpdlcMessageDto message)
    {
        var background = Theme.CPDLCBackgroundColor;

        if (IsUrgent(message))
        {
            return new ColorPair(background, Theme.CPDLCUrgentColor).InvertIf(!message.IsAcknowledged);
        }

        if (IsFailed(message))
        {
            // Failed message background is CPDLCClosedColor.
            return new ColorPair(Theme.CPDLCClosedColor, Theme.CPDLCFailedColor).InvertIf(!message.IsAcknowledged);
        }

        if (IsClosed(message) && message is UplinkMessageDto)
        {
            return new ColorPair(background, Theme.CPDLCClosedColor);
        }

        if (IsClosed(message) && message is DownlinkMessageDto)
        {
            return new ColorPair(background, Theme.CPDLCClosedColor).InvertIf(!message.IsAcknowledged);
        }

        // Special closed timeout: Special uplink that timed out before acknowledgement
        // Shows Normal video (not inverted) with pilot late color
        if (message is UplinkMessageDto { IsSpecial: true, IsClosed: true, IsPilotLate: true, IsAcknowledged: false })
        {
            return new ColorPair(background, Theme.CPDLCPilotLateColor);
        }

        // Special Closed: For a special Uplink Message that is closed by itself
        // After acknowledgement (even if it timed out), show Normal video with CPDLCClosedColor
        // Before acknowledgement (and hasn't timed out), show Inverse video with CPDLCClosedColor
        if (message is UplinkMessageDto { IsSpecial: true, IsClosed: true } ul)
        {
            return new ColorPair(background, Theme.CPDLCClosedColor).InvertIf(!ul.IsAcknowledged);
        }

        if (IsPilotLate(message))
        {
            // Time Out (pilot or Controller) message background is CPDLCClosedColor
            return new ColorPair(Theme.CPDLCClosedColor, Theme.CPDLCPilotLateColor).InvertIf(!message.IsAcknowledged);
        }

        if (IsControllerLate(message))
        {
            // Time Out (pilot or Controller) message background is CPDLCClosedColor
            return new ColorPair(Theme.CPDLCClosedColor, Theme.CPDLCControllerLateColor).InvertIf(!message.IsAcknowledged);
        }

        if (message is DownlinkMessageDto)
        {
            return new ColorPair(background, Theme.CPDLCDownlinkColor).InvertIf(!message.IsAcknowledged);
        }

        if (message is UplinkMessageDto)
        {
            return new ColorPair(background, Theme.CPDLCUplinkColor).InvertIf(!message.IsAcknowledged);
        }

        return new ColorPair(background, Theme.CPDLCClosedColor);

        bool IsUrgent(CpdlcMessageDto message)
        {
            return message switch
            {
                DownlinkMessageDto downlinkMessage => downlinkMessage.AlertType != AlertType.None,
                UplinkMessageDto uplinkMessage => uplinkMessage.AlertType != AlertType.None,
                _ => false
            };
        }

        bool IsFailed(CpdlcMessageDto message)
        {
            return message switch
            {
                DownlinkMessageDto downlinkMessage => downlinkMessage.Content.StartsWith("ERROR"),
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

    public record ColorPair(SolidColorBrush Background, SolidColorBrush Foreground)
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
