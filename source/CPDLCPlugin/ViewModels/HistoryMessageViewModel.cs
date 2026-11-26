using System.Text;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CPDLCPlugin.Extensions;
using CPDLCServer.Contracts;
using vatsys;
using vatSysToolbox;

namespace CPDLCPlugin.ViewModels;

public partial class HistoryMessageViewModel : ObservableObject
{
    readonly int _maxMessageDisplayLength;

    public DialogueDto Dialogue { get; }
    public CpdlcMessageDto OriginalMessage { get; }

    public HistoryMessageViewModel(DialogueDto dialogue, CpdlcMessageDto message, int maxMessageDisplayLength)
    {
        Dialogue = dialogue;
        OriginalMessage = message;
        _maxMessageDisplayLength = maxMessageDisplayLength;

        Callsign = GetCallsignFromMessage(message);
        Time = FormatTime(GetTimeFromMessage(message));

        var formattedContent = message switch
        {
            UplinkMessageDto uplinkMessage => uplinkMessage.Text(),
            DownlinkMessageDto downlinkMessage => downlinkMessage.Text(),
            _ => string.Empty
        };

        Content = GetDisplayText(message, formattedContent);
        FullContent = GetExtendedDisplayText(message, formattedContent);
        Prefix = CalculatePrefix(formattedContent);

        var (background, foreground) = MessageColours.GetMessageColors(message);
        BackgroundColor = background;
        ForegroundColor = foreground;
    }

    [ObservableProperty]
    string callsign = string.Empty;

    [ObservableProperty]
    string time = string.Empty;

    [ObservableProperty]
    string prefix = string.Empty;

    [ObservableProperty]
    string content = string.Empty;

    [ObservableProperty]
    string fullContent = string.Empty;

    [ObservableProperty]
    bool isExtended;

    [ObservableProperty]
    SolidColorBrush foregroundColor = VatSysColours.GetColour(Colours.Identities.GenericText);

    [ObservableProperty]
    SolidColorBrush backgroundColor = VatSysColours.GetColour(Colours.Identities.WindowBackground);

    string GetCallsignFromMessage(CpdlcMessageDto message)
    {
        var callsign = message switch
        {
            DownlinkMessageDto dl => dl.Sender,
            UplinkMessageDto ul => ul.Recipient,
            _ => string.Empty
        };

        // Pad callsign to 8 characters so background extends to full width
        return callsign.PadRight(8);
    }

    string FormatTime(DateTimeOffset time)
    {
        return time.ToString("HH:mm");
    }

    DateTimeOffset GetTimeFromMessage(CpdlcMessageDto message)
    {
        return message switch
        {
            DownlinkMessageDto dl => dl.Received,
            UplinkMessageDto ul => ul.Sent,
            _ => DateTimeOffset.MinValue
        };
    }

    string GetDisplayText(CpdlcMessageDto message, string fullContent)
    {
        var fullText = GetFormattedContent(message, fullContent);
        if (fullText.Length >= _maxMessageDisplayLength)
            return fullText.Substring(0, _maxMessageDisplayLength);

        return fullText.PadRight(_maxMessageDisplayLength);
    }

    string GetExtendedDisplayText(CpdlcMessageDto message, string fullContent)
    {
        return GetFormattedContent(message, fullContent);
    }

    string GetFormattedContent(CpdlcMessageDto message, string fullContent)
    {
        var contentPrefix = GetMessageContentPrefix(message);
        return contentPrefix + fullContent;
    }

    string GetMessageContentPrefix(CpdlcMessageDto message)
    {
        var sb = new StringBuilder();
        if (message is DownlinkMessageDto dl)
        {
            // TODO
            // if (dl.HasPilotFreeText)
            // {
            //     sb.Append("P");
            // }
            // else
            // {
            //     sb.Append(" ");
            // }
            sb.Append(" ");
        }

        sb.Append(message is UplinkMessageDto uplink && uplink.IsNotActioned(Dialogue) ? "X" : " ");

        sb.Append(message is UplinkMessageDto { IsClosedManually: true } ? "M" : " ");

        var isHighPriority = false;
        sb.Append(isHighPriority ? "!" : " ");

        sb.Append(!string.IsNullOrWhiteSpace(sb.ToString()) ? ":" : " ");
        return sb.ToString();
    }

    string CalculatePrefix(string content)
    {
        var sb = new StringBuilder();
        sb.Append(content.Length > _maxMessageDisplayLength ? "*" : " ");
        return sb.ToString();
    }
}
