namespace CPDLCServer.Model;

// The system that supplied the ADS-C report
public enum AdscReportSource
{
    // The aircraft sent the report through Hoppie's ACARS network
    Hoppie,

    // The server made the report from the VATSIM status data
    VATSIM
}
