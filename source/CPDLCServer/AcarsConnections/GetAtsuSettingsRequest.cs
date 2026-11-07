using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.AcarsConnections;

public record GetAtsuSettingsRequest(string StationId) : IRequest<AtsuSettingsDto>;
