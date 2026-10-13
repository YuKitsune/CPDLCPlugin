using CPDLCServer.Contracts;

namespace CPDLCPlugin.Server;

public interface IDownlinkHandlerDelegate
{
    Task DialogueChanged(DialogueDto dialogue, CancellationToken cancellationToken);
    Task AircraftConnectionUpdated(AircraftConnectionDto aircraftConnectionDto, CancellationToken cancellationToken);
    Task AircraftConnectionRemoved(string callsign, string stationId, CancellationToken cancellationToken);
    Task AcarsStationsUpdated(string[] callsigns, CancellationToken cancellationToken);
    Task AdscContractUpdated(AdscContractDto adscContractDto, CancellationToken cancellationToken);
    Task AdscContractRemoved(string callsign, string stationId, CancellationToken cancellationToken);
    Task AdscReportReceived(AdscReportDto adscReportDto, CancellationToken cancellationToken);
    void Error(Exception exception);
}
