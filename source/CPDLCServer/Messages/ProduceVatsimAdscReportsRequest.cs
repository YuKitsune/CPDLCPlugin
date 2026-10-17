using MediatR;

namespace CPDLCServer.Messages;

// Makes the ADS-C reports that are due from the latest VATSIM status data
public record ProduceVatsimAdscReportsRequest : IRequest;
