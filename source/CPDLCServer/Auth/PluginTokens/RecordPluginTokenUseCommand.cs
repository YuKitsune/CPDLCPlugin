using MediatR;

namespace CPDLCServer.Auth.PluginTokens;

// Records the time that the plugin used the token, for the token list of the administrators
public record RecordPluginTokenUseCommand(Guid Id) : IRequest;
