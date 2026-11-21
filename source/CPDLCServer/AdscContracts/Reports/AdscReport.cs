namespace CPDLCServer.AdscContracts.Reports;

// ADS-C basic group. Altitude is in feet.
public record AdscReport(
    DateTimeOffset Timestamp,
    double Latitude,
    double Longitude,
    int Altitude,
    AdscReportSource Source);
