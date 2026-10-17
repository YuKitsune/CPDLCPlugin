using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.Messages;

public record GetAtsuSettingsRequest(string StationId) : IRequest<AtsuSettingsDto>;
