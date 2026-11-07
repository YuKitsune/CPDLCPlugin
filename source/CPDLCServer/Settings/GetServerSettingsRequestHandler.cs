using MediatR;

namespace CPDLCServer.Settings;

public class GetServerSettingsRequestHandler(IServerSettingsProvider settingsProvider)
    : IRequestHandler<GetServerSettingsRequest, ServerSettings>
{
    public Task<ServerSettings> Handle(GetServerSettingsRequest request, CancellationToken cancellationToken)
    {
        return settingsProvider.Load(cancellationToken);
    }
}
