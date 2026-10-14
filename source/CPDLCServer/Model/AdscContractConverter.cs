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
            ToDto(contract.DefaultContractState),
            contract.SelectedReportingInterval,
            ToDto(contract.SelectedContractState),
            contract.SelectedContractEnds,
            ToDto(contract.DemandContractState));
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

    static Contracts.AdscContractState? ToDto(AdscContractState? state)
    {
        return state switch
        {
            null => null,
            AdscContractState.Sent => Contracts.AdscContractState.Sent,
            AdscContractState.Accepted => Contracts.AdscContractState.Accepted,
            AdscContractState.Rejected => Contracts.AdscContractState.Rejected,
            _ => throw new ArgumentException($"Unknown ADS-C contract state: {state}")
        };
    }
}
