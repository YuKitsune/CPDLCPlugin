using MediatR;

namespace CPDLCServer.Auth.PluginTokens;

// Does nothing if the token does not exist
public record RevokePluginTokenCommand(string Token) : IRequest;
