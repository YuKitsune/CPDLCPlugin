using CPDLCServer.Contracts;

namespace CPDLCServer.Model;

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

    public static CpdlcMessageDto ToMessageDto(ICpdlcMessage message)
    {
        return message switch
        {
            UplinkMessage uplink => ToDto(uplink),
            DownlinkMessage downlink => ToDto(downlink),
            _ => throw new ArgumentException($"Unknown message type: {message.GetType()}")
        };
    }

    public static Contracts.CpdlcUplinkResponseType ToDto(CpdlcUplinkResponseType uplinkResponseType)
    {
        return uplinkResponseType switch
        {
            CpdlcUplinkResponseType.NoResponse => Contracts.CpdlcUplinkResponseType.NoResponse,
            CpdlcUplinkResponseType.WilcoUnable => Contracts.CpdlcUplinkResponseType.WilcoUnable,
            CpdlcUplinkResponseType.AffirmativeNegative => Contracts.CpdlcUplinkResponseType.AffirmativeNegative,
            CpdlcUplinkResponseType.Roger => Contracts.CpdlcUplinkResponseType.Roger,
            _ => throw new ArgumentException($"Unknown uplink response type: {uplinkResponseType}")
        };
    }

    public static Contracts.CpdlcDownlinkResponseType ToDto(CpdlcDownlinkResponseType downlinkResponseType)
    {
        return downlinkResponseType switch
        {
            CpdlcDownlinkResponseType.NoResponse => Contracts.CpdlcDownlinkResponseType.NoResponse,
            CpdlcDownlinkResponseType.ResponseRequired => Contracts.CpdlcDownlinkResponseType.ResponseRequired,
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

    public static Contracts.DataAuthorityState ToDto(DataAuthorityState dataAuthorityState)
    {
        return dataAuthorityState switch
        {
            DataAuthorityState.NextDataAuthority => Contracts.DataAuthorityState.NextDataAuthority,
            DataAuthorityState.CurrentDataAuthority => Contracts.DataAuthorityState.CurrentDataAuthority,
            _ => throw new ArgumentException($"Unknown data authority state: {dataAuthorityState}")
        };
    }
}
