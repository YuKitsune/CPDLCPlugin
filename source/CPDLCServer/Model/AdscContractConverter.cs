using CPDLCServer.Contracts;

namespace CPDLCServer.Model;

public static class AdscContractConverter
{
    public static AdscContractDto ToDto(AdscContract contract)
    {
        return new AdscContractDto(
            contract.Callsign,
            contract.StationId,
            contract.ReportingInterval,
            contract.Requested);
    }

    public static AdscReportDto ToDto(string callsign, string stationId, AdscReport report, DateTimeOffset received)
    {
        return new AdscReportDto(
            callsign,
            stationId,
            report.Timestamp,
            report.Latitude,
            report.Longitude,
            report.Altitude,
            received);
    }
}
