using CPDLCServer.AdscContracts.Reports;
using CPDLCServer.Contracts;

namespace CPDLCServer.AdscContracts;

public static class AdscContractConverter
{
    public static AdscContractDto ToDto(AdscContract contract)
    {
        return new AdscContractDto(
            contract.Callsign,
            contract.StationId,
            contract.ReportingInterval,
            contract.DefaultReportingInterval,
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
            received,
            ToDto(report.Source));
    }

    static Contracts.AdscReportSource ToDto(Reports.AdscReportSource source)
    {
        return source switch
        {
            Reports.AdscReportSource.Hoppie => Contracts.AdscReportSource.Hoppie,
            Reports.AdscReportSource.VATSIM => Contracts.AdscReportSource.VATSIM,
            _ => throw new ArgumentException($"Unknown ADS-C report source: {source}")
        };
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
