using MediatR;

namespace CPDLCServer.Settings;

public record SaveServerSettingsCommand(ServerSettings Settings) : IRequest;
