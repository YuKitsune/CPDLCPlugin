using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CPDLCPlugin.Extensions;
using CPDLCServer.Contracts;

namespace CPDLCPlugin.ViewModels;

public partial class CurrentMessageViewModel : ObservableObject
{
    readonly int _maxMessageDisplayLength;
    readonly IReadOnlyCollection<int> _flaggedElementIds;

    public CurrentMessageViewModel(DialogueDto dialogue, CpdlcMessageDto message, int maxMessageDisplayLength, IReadOnlyCollection<int> flaggedElementIds)
    {
        _maxMessageDisplayLength = maxMessageDisplayLength;
        _flaggedElementIds = flaggedElementIds;
        UpdateMessage(dialogue, message);
    }

    public DialogueDto Dialogue { get; private set; }
    public CpdlcMessageDto Message { get; private set; }

    public void UpdateMessage(DialogueDto dialogue, CpdlcMessageDto newMessage)
    {
        Dialogue = dialogue;
        Message = newMessage;

        Callsign = GetCallsignFromMessage(newMessage);
        Time = FormatTime(GetTimeFromMessage(newMessage));

        var formattedContent = newMessage switch
        {
            UplinkMessageDto uplinkMessageDto => uplinkMessageDto.Text(),
            DownlinkMessageDto downlinkMessageDto => downlinkMessageDto.Text(),
            _ => string.Empty
        };

        FullContent = formattedContent;
        Content = GetDisplayContent(formattedContent);
        Prefix = CalculatePrefix(formattedContent);
        IsDownlink = newMessage is DownlinkMessageDto;

        var (background, foreground) = MessageColours.GetMessageColors(Message);
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
    bool isDownlink;

    [ObservableProperty]
    MessageColour foregroundColor;

    [ObservableProperty]
    MessageColour backgroundColor;

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

    DateTimeOffset GetTimeFromMessage(CpdlcMessageDto message)
    {
        return message switch
        {
            DownlinkMessageDto dl => dl.Received,
            UplinkMessageDto ul => ul.Sent,
            _ => DateTimeOffset.MinValue
        };
    }

    string FormatTime(DateTimeOffset time)
    {
        return time.ToString("HH:mm");
    }

    string GetDisplayContent(string fullContent)
    {
        if (fullContent.Length >= _maxMessageDisplayLength)
            return fullContent.Substring(0, _maxMessageDisplayLength);

        // Pad with spaces to reach max length so background extends to full width
        return fullContent.PadRight(_maxMessageDisplayLength);
    }

    string CalculatePrefix(string content)
    {
        var sb = new StringBuilder();

        sb.Append(content.Length > _maxMessageDisplayLength ? "*" : " ");

        // "P" has priority over "!" (flagged)
        var hasFreeText = Message switch
        {
            UplinkMessageDto uplink => uplink.HasFreeText(),
            DownlinkMessageDto downlink => downlink.HasFreeText(),
            _ => false
        };
        var isFlagged = Message is DownlinkMessageDto flaggedDownlink && flaggedDownlink.IsFlagged(_flaggedElementIds);
        sb.Append(hasFreeText ? "P" : isFlagged ? "!" : " ");

        sb.Append(Message is UplinkMessageDto uplinkMessage && uplinkMessage.IsNotActioned() ? "X" : " ");

        return sb.ToString();
    }
}
