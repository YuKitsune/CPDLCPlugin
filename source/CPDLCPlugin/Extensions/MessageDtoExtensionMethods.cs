using CPDLC.Messages;
using CPDLCServer.Contracts;

namespace CPDLCPlugin.Extensions;

public static class MessageDtoExtensionMethods
{
    public static string Text(this UplinkMessageDto dto)
    {
        return OnOneLine(MessageText.Format(dto.Elements));
    }

    public static string Text(this DownlinkMessageDto dto)
    {
        return OnOneLine(MessageText.Format(dto.Elements));
    }

    public static bool HasElement(this UplinkMessageDto dto, int elementId)
    {
        return dto.Elements.Any(e => e.Id == elementId);
    }

    public static bool HasElement(this DownlinkMessageDto dto, int elementId)
    {
        return dto.Elements.Any(e => e.Id == elementId);
    }

    public static bool HasFreeText(this UplinkMessageDto dto)
    {
        return dto.HasElement(UplinkElementIds.FreeText) || dto.HasElement(UplinkElementIds.DistressFreeText);
    }

    public static bool HasFreeText(this DownlinkMessageDto dto)
    {
        return dto.HasElement(DownlinkElementIds.FreeText) || dto.HasElement(DownlinkElementIds.DistressFreeText);
    }

    // A flagged downlink has one of the elements in FlaggedElementId in the configuration
    public static bool IsFlagged(this DownlinkMessageDto dto, IReadOnlyCollection<int> flaggedElementIds)
    {
        return dto.Elements.Any(e => flaggedElementIds.Contains(e.Id));
    }

    // True when the pilot sent WILCO to an uplink that can update the FDR, but the FDR was not updated
    public static bool IsNotActioned(this UplinkMessageDto dto, DialogueDto dialogue)
    {
        if (!dto.CanAction || dto.Actioned)
            return false;

        return dialogue.Messages
            .OfType<DownlinkMessageDto>()
            .Any(d => d.MessageReference == dto.MessageId && d.HasElement(DownlinkElementIds.Wilco));
    }

    // STANDBY and REQUEST DEFERRED do not close a dialogue, so they have their own colours
    public static bool IsSpecial(this UplinkMessageDto dto)
    {
        return dto.HasElement(UplinkElementIds.Standby) || dto.HasElement(UplinkElementIds.RequestDeferred);
    }

    static string OnOneLine(string text)
    {
        return text
            .Replace("\r\n", ". ")
            .Replace("\r", ". ")
            .Replace("\n", ". ");
    }
}
