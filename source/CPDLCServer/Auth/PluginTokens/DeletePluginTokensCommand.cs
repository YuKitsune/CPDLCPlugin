using MediatR;

namespace CPDLCServer.Auth.PluginTokens;

// An administrator deletes tokens. The plugins that use them must authenticate again.
public record DeletePluginTokensCommand(Guid[] Ids, string AdministratorCid) : IRequest<int>;
