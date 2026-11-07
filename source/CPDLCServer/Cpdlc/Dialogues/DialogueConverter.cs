using CPDLCServer.Contracts;
using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;

namespace CPDLCServer.Cpdlc.Dialogues;

public static class DialogueConverter
{
    public static DialogueDto ToDto(Dialogue dialogue)
    {
        return new DialogueDto(
            dialogue.Id,
            dialogue.AircraftCallsign,
            dialogue.Messages.Select(ToMessageDto).ToList(),
            dialogue.Opened,
            dialogue.Closed,
            dialogue.Archived);
    }

    public static CpdlcMessageDto ToMessageDto(CpdlcMessage message)
    {
        return message switch
        {
            UplinkMessage uplink => ToDto(uplink),
            DownlinkMessage downlink => ToDto(downlink),
            _ => throw new ArgumentException($"Unknown message type: {message.GetType()}")
        };
    }

    public static Contracts.CpdlcUplinkResponseType ToDto(Uplinks.CpdlcUplinkResponseType uplinkResponseType)
    {
        return uplinkResponseType switch
        {
            Uplinks.CpdlcUplinkResponseType.NoResponse => Contracts.CpdlcUplinkResponseType.NoResponse,
            Uplinks.CpdlcUplinkResponseType.WilcoUnable => Contracts.CpdlcUplinkResponseType.WilcoUnable,
            Uplinks.CpdlcUplinkResponseType.AffirmativeNegative => Contracts.CpdlcUplinkResponseType.AffirmativeNegative,
            Uplinks.CpdlcUplinkResponseType.Roger => Contracts.CpdlcUplinkResponseType.Roger,
            _ => throw new ArgumentException($"Unknown uplink response type: {uplinkResponseType}")
        };
    }

    public static Contracts.CpdlcDownlinkResponseType ToDto(Downlinks.CpdlcDownlinkResponseType downlinkResponseType)
    {
        return downlinkResponseType switch
        {
            Downlinks.CpdlcDownlinkResponseType.NoResponse => Contracts.CpdlcDownlinkResponseType.NoResponse,
            Downlinks.CpdlcDownlinkResponseType.ResponseRequired => Contracts.CpdlcDownlinkResponseType.ResponseRequired,
            _ => throw new ArgumentException($"Unknown downlink response type: {downlinkResponseType}")
        };
    }

    public static Contracts.AlertType ToDto(AlertType alertType)
    {
        return alertType switch
        {
            AlertType.High => Contracts.AlertType.High,
            AlertType.Medium => Contracts.AlertType.Medium,
            AlertType.Low => Contracts.AlertType.Low,
            AlertType.None => Contracts.AlertType.None,
            _ => throw new ArgumentException($"Unknown alert type: {alertType.GetType()}")
        };
    }

    public static UplinkMessageDto ToDto(UplinkMessage uplink)
    {
        return new UplinkMessageDto
        {
            DialogueId = uplink.DialogueId,
            MessageId = uplink.MessageId,
            MessageReference = uplink.MessageReference,
            AlertType = ToDto(uplink.AlertType),
            Closed = uplink.Closed,
            IsClosedManually = uplink.ClosedManually,
            Acknowledged = uplink.Sent,
            Recipient = uplink.Recipient,
            SenderCallsign = uplink.SenderCallsign,
            ResponseType = ToDto(uplink.ResponseType),
            Content = uplink.Content,
            Sent = uplink.Sent,
            IsPilotLate = uplink.IsPilotLate,
            IsTransmissionFailed = uplink.IsTransmissionFailed
        };
    }

    public static DownlinkMessageDto ToDto(DownlinkMessage downlink)
    {
        return new DownlinkMessageDto
        {
            DialogueId = downlink.DialogueId,
            MessageId = downlink.MessageId,
            MessageReference = downlink.MessageReference,
            AlertType = ToDto(downlink.AlertType),
            Closed = downlink.Closed,
            Acknowledged = downlink.Acknowledged,
            Sender = downlink.Sender,
            ResponseType = ToDto(downlink.ResponseType),
            Content = downlink.Content,
            Received = downlink.Received,
            IsControllerLate = downlink.IsControllerLate
        };
    }

    public static Contracts.DataAuthorityState ToDto(Connections.DataAuthorityState dataAuthorityState)
    {
        return dataAuthorityState switch
        {
            Connections.DataAuthorityState.NextDataAuthority => Contracts.DataAuthorityState.NextDataAuthority,
            Connections.DataAuthorityState.CurrentDataAuthority => Contracts.DataAuthorityState.CurrentDataAuthority,
            _ => throw new ArgumentException($"Unknown data authority state: {dataAuthorityState}")
        };
    }
}
