using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;

namespace CPDLCServer.Web.Pages.ViewModels;

public record MessageRowViewModel(
    Guid DialogueId,
    DateTimeOffset Timestamp,
    string AcarsNetwork,
    bool IsUplink,
    string Sender,
    string Receiver,
    string AlertType,
    string ResponseType,
    string Content,
    bool IsClosed)
{
    public static MessageRowViewModel From(
        Cpdlc.Dialogues.Dialogue dialogue,
        CpdlcMessage message,
        Dictionary<string, AircraftConnection> aircraftLookup)
    {
        var aircraft = aircraftLookup.GetValueOrDefault(dialogue.AircraftCallsign);
        var acarsNetwork = aircraft?.AcarsConnectionId ?? "Unknown";
        var stationId = aircraft?.StationId ?? "Unknown";

        return message switch
        {
            UplinkMessage uplink => new MessageRowViewModel(
                DialogueId: dialogue.Id,
                Timestamp: uplink.Sent,
                AcarsNetwork: acarsNetwork,
                IsUplink: true,
                Sender: $"{stationId} ({uplink.SenderCallsign})",
                Receiver: uplink.Recipient,
                AlertType: uplink.AlertType.ToString(),
                ResponseType: uplink.ResponseType.ToString(),
                Content: uplink.Content,
                IsClosed: uplink.IsClosed),

            DownlinkMessage downlink => new MessageRowViewModel(
                DialogueId: dialogue.Id,
                Timestamp: downlink.Received,
                AcarsNetwork: acarsNetwork,
                IsUplink: false,
                Sender: downlink.Sender,
                Receiver: stationId,
                AlertType: downlink.AlertType.ToString(),
                ResponseType: downlink.ResponseType.ToString(),
                Content: downlink.Content,
                IsClosed: downlink.IsClosed),

            _ => throw new InvalidOperationException("Unknown message type")
        };
    }
}
