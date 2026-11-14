using MediatR;

namespace CPDLCServer.VatsimStatus;

/// <summary>
///     Creates an ADS-C position report using VATSIM status data.
/// </summary>
public record ProduceVatsimAdscReportsRequest : IRequest;
