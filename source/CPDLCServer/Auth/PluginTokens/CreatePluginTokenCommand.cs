using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.Auth.PluginTokens;

public record CreatePluginTokenCommand(string Cid, string Name) : IRequest<PluginTokenResponse>;
