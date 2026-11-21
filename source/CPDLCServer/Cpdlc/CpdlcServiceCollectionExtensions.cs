using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Motd;
using CPDLCServer.Cpdlc.Uplinks;

namespace CPDLCServer.Cpdlc;

public static class CpdlcServiceCollectionExtensions
{
    public static IServiceCollection AddCpdlc(this IServiceCollection services)
    {
        services.AddScoped<IAircraftRepository, AircraftRepository>();
        services.AddHostedService<LostConnectionService>();
        services.AddHostedService<NdaUplinkService>();

        services.AddScoped<IDialogueRepository, DialogueRepository>();
        services.AddHostedService<MessageMonitorService>();

        services.AddScoped<IMessageIdProvider, MessageIdProvider>();
        services.AddScoped<IUplinkTransmitter, UplinkTransmitter>();

        services.AddSingleton<MotdScheduler>();

        return services;
    }
}
