using CPDLCServer.Contracts;

namespace CPDLCPlugin;

// The state of an ADS-C request. IsUsingVatsimData is true when the server made the latest report from the VATSIM status data.
public record AdscTransmissionState(AdscContractState State, bool IsUsingVatsimData);
