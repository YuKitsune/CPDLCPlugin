using CPDLCServer.Contracts;
using MediatR;

namespace CPDLCServer.Auth.PluginTokens;

public record CreatePluginTokenCommand(int Cid, string Name) : IRequest<PluginTokenResponse>;
