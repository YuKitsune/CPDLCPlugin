using MediatR;

namespace CPDLCServer.Settings;

public record GetServerSettingsRequest : IRequest<ServerSettings>;
