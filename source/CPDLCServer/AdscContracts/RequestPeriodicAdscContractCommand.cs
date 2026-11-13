using MediatR;

namespace CPDLCServer.AdscContracts;

// A reporting interval that is the same as the default reporting interval of the ATSU requests the default contract.
// Any other reporting interval requests a selected contract.
public record RequestPeriodicAdscContractCommand(
    string StationId,
    string Callsign,
    TimeSpan ReportingInterval)
    : IRequest;
