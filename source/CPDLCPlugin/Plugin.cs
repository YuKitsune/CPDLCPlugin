using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using CPDLCPlugin.Configuration;
using CPDLCPlugin.Extensions;
using CPDLCPlugin.LabelItems;
using CPDLCPlugin.Messages;
using CPDLCPlugin.Server;
using CPDLCPlugin.Windows;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using vatsys;
using vatsys.Plugin;

namespace CPDLCPlugin;

// TODO: Complex variable entry (popups and validation)
// TODO: Text message fallback
// TODO: window frame styling
// TODO: ADS-C

[Export(typeof(IPlugin))]
public class Plugin : ILabelPlugin, IStripPlugin, IRecipient<DialogueChangedNotification>, IRecipient<ConnectedAircraftChanged>
{
#if DEBUG
    public const string Name = "CPDLC Plugin - Debug";
#else
    public const string Name = "CPDLC Plugin";
#endif

    static readonly Dictionary<string, DateTimeOffset> ErrorMessages = new();

    readonly CpdlcStatusLabelItemProvider _cpdlcStatusProvider;
    readonly TextStatusLabelItemProvider _textStatusProvider;
    readonly ColourCache _colourCache;

    readonly WorkQueue _workQueue;

    BackgroundWorker? _ndaRecalculationWorker;

    string IPlugin.Name => Name;

    IServiceProvider ServiceProvider { get; set; }

    public SignalRConnectionManager? ConnectionManager { get; set; }

    public Plugin()
    {
        DpiAwareness.EnsureDpiAwareness();

        ConfigureTheme();
        _colourCache = CacheLabelColours();
        _cpdlcStatusProvider = new CpdlcStatusLabelItemProvider();
        _textStatusProvider = new TextStatusLabelItemProvider(_colourCache);

        var configuration = ConfigurationLoader.Load();
        ConfigureServices(configuration);

        AddToolbarItems();

        Network.Connected += NetworkConnected;
        Network.Disconnected += NetworkDisconnected;

        WeakReferenceMessenger.Default.Register<DialogueChangedNotification>(this);
        WeakReferenceMessenger.Default.Register<ConnectedAircraftChanged>(this);

        _workQueue = new WorkQueue(AddError, Log.Logger, new SystemClock());
    }

    void ConfigureServices(PluginConfiguration pluginConfiguration)
    {
        var logger = ConfigureLogger(pluginConfiguration);

        ServiceProvider = new ServiceCollection()
            .AddSingleton(logger)
            .AddSingleton(this) // TODO: Ick... Whatever we're relying on this for, move it into a separate service please.
            .AddSingleton(pluginConfiguration)
            .AddSingleton<IClock>(new SystemClock())
            .AddSingleton<IGuiInvoker, GuiInvoker>()
            .AddSingleton<IErrorReporter, ErrorReporter>()
            .AddSingleton<IJurisdictionChecker, JurisdictionChecker>()
            .AddSingleton<AircraftConnectionStore>()
            .AddSingleton<AcarsConnectedCallsignStore>()
            .AddSingleton<WindowManager>()
            .AddSingleton<DialogueStore>()
            .AddSingleton<SuspendedMessageStore>()
            .AddSingleton(_cpdlcStatusProvider)
            .AddSingleton(_colourCache)
            .AddMediatR(c => c.RegisterServicesFromAssemblies(typeof(Plugin).Assembly))
            .BuildServiceProvider();
    }

    ILogger ConfigureLogger(PluginConfiguration configuration)
    {
        var logFileName = Path.Combine(Helpers.GetFilesFolder(), "cpdlc_log.txt");

        var logger = new LoggerConfiguration()
            .WriteTo.File(
                path: logFileName,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: configuration.MaxLogFileAgeDays,
                outputTemplate: "{Timestamp:u} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .MinimumLevel.Is(configuration.LogLevel)
            .CreateLogger();

        Log.Logger = logger;

        return logger;
    }

    void NetworkConnected(object sender, EventArgs e)
    {
        Log.Information("Connected to VATSIM as {Callsign}", Network.Callsign);
        _workQueue.Enqueue(() =>
        {
            StartNdaRecalculationWorker();
            return Task.CompletedTask;
        });

        // TODO: Connect if auto-connect enabled
    }

    void NetworkDisconnected(object sender, EventArgs e)
    {
        Log.Information("Disconnected from VATSIM");

        try
        {
            _workQueue.Enqueue(StopNdaRecalculationWorker);

            _workQueue.Enqueue(async () =>
            {
                var mediator =  ServiceProvider.GetService<IMediator>();
                if (mediator is null)
                    return;

                await mediator.Send(new DisconnectRequest()).ConfigureAwait(false);
            });
        }
        catch (Exception ex)
        {
            AddError(ex);
        }
    }

    void StartNdaRecalculationWorker()
    {
        Log.Information("Starting NDA recalculation worker");
        _ndaRecalculationWorker = new BackgroundWorker(async cancellationToken =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                    await RecalculateNextDataAuthorityForAllAircraft();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Log.Verbose("NDA recalculation worker cancellation requested");
                }
                catch (Exception ex)
                {
                    AddError(ex);
                }
            }
        });
    }

    async Task StopNdaRecalculationWorker()
    {
        if (_ndaRecalculationWorker is null)
            return;

        Log.Information("Stopping NDA recalculation worker");
        await _ndaRecalculationWorker.DisposeAsync();
        _ndaRecalculationWorker = null;
    }

    async Task RecalculateNextDataAuthorityForAllAircraft()
    {
        var store = ServiceProvider.GetRequiredService<AircraftConnectionStore>();
        var connections = await store.All(CancellationToken.None);

        var ourAircraft = connections
            .Where(c => c.StationId == ConnectionManager?.StationIdentifier &&
                       c.DataAuthorityState == CPDLCServer.Contracts.DataAuthorityState.CurrentDataAuthority);

        foreach (var aircraft in ourAircraft)
        {
            var fdr = FDP2.GetFDRs.FirstOrDefault(f => f.Callsign == aircraft.Callsign);
            if (fdr is null || !fdr.IsTrackedByMe)
                continue;

            var mediator = ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new UpdateNextDataAuthorityForFdrRequest(fdr));
        }
    }

    public static void AddError(Exception exception)
    {
        Log.Error(exception, "An error has occurred");
        TryAddErrorInternal(exception);
    }

    public static void AddError(Exception exception, string message)
    {
        Log.Error(exception, message);
        TryAddErrorInternal(exception);
    }

    static void TryAddErrorInternal(Exception exception)
    {
        lock (ErrorMessages)
        {
            // Don't flood the error window with the same message over and over again
            if (ErrorMessages.TryGetValue(exception.Message, out var lastShown) &&
                DateTimeOffset.Now - lastShown <= TimeSpan.FromMinutes(1))
                return;

            Errors.Add(exception, Name);
            ErrorMessages[exception.Message] = DateTimeOffset.Now;
        }
    }

    void ConfigureTheme()
    {
        Theme.BackgroundColor = GetColour(Colours.Identities.WindowBackground);
        Theme.GenericTextColor = GetColour(Colours.Identities.GenericText);
        Theme.InteractiveTextColor = GetColour(Colours.Identities.InteractiveText);
        Theme.NonInteractiveTextColor = GetColour(Colours.Identities.NonInteractiveText);
        Theme.SelectedButtonColor = GetColour(Colours.Identities.WindowButtonSelected);

        Theme.CPDLCBackgroundColor = GetColour(Colours.Identities.CPDLCMessageBackground);
        Theme.CPDLCUplinkColor = GetColour(Colours.Identities.CPDLCUplink);
        Theme.CPDLCDownlinkColor = GetColour(Colours.Identities.CPDLCDownlink);
        Theme.CPDLCSendBackgroundColor = GetColour(Colours.Identities.CPDLCSendButton);
        Theme.CPDLCHotButtonBackgroundColor = GetColour(Colours.Identities.CPDLCSendButton);

        Theme.FontFamily = new FontFamily(MMI.eurofont_xsml.FontFamily.Name);
        Theme.FontSize = MMI.eurofont_xsml.Size;
        Theme.FontWeight = MMI.eurofont_xsml.Bold ? FontWeights.Bold : FontWeights.Regular;

        SolidColorBrush GetColour(Colours.Identities identity)
        {
            return new SolidColorBrush(Colours.GetColour(identity).ToWindowsColor());
        }
    }

    ColourCache CacheLabelColours()
    {
        // Need to cache these for thread-safe access

        var downlinkColor = Theme.CPDLCDownlinkColor.Color;
        var customDownlinkColor = new CustomColour(downlinkColor.R, downlinkColor.G, downlinkColor.B);

        var unableColor = Theme.CPDLCUnableDownlinkColor.Color;
        var customUnableDownlinkColor = new CustomColour(unableColor.R, unableColor.G, unableColor.B);

        var suspendedColor = Theme.CPDLCSuspendedColor.Color;
        var customSuspendedColor = new CustomColour(suspendedColor.R, suspendedColor.G, suspendedColor.B);

        return new ColourCache
        {
            DownlinkBackgroundColour = customDownlinkColor,
            UnableBackgroundColour = customUnableDownlinkColor,
            SuspendedForegroundColour = customSuspendedColor
        };
    }

    void AddToolbarItems()
    {
        const string menuItemCategory = "CPDLC";

        var setupMenuItem = new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            menuItemCategory,
            new ToolStripMenuItem("Setup"));
        setupMenuItem.Item.Click += (_, _) =>
        {
            _workQueue.Enqueue(async () =>
            {
                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new OpenSetupWindowRequest());
            });
        };

        MMI.AddCustomMenuItem(setupMenuItem);

        var currentMessagesMenuItem = new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            menuItemCategory,
            new ToolStripMenuItem("Current Messages"));
        currentMessagesMenuItem.Item.Click += (_, _) =>
        {
            _workQueue.Enqueue(async () =>
            {
                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new OpenCurrentMessagesWindowRequest());
            });
        };

        MMI.AddCustomMenuItem(currentMessagesMenuItem);

        var historyMenuItem = new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            menuItemCategory,
            new ToolStripMenuItem("History"));
        historyMenuItem.Item.Click += (_, _) =>
        {
            _workQueue.Enqueue(async () =>
            {
                var selectedCallsign = MMI.SelectedTrack?.GetFDR()?.Callsign;

                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new OpenHistoryWindowRequest(selectedCallsign));
            });
        };

        MMI.AddCustomMenuItem(historyMenuItem);
    }

    public void OnFDRUpdate(FDP2.FDR updated)
    {
        try
        {
            // Record the last known owner of each FDR
            _workQueue.Enqueue(async () =>
            {
                Log.Debug(
                    "{Callsign}: IsTracked {IsTracked}; IsTrackedByMe: {IsTrackedByMe}; Controller Tracking: {CurrentController}; Handoff Controller: {HandoffController}",
                    updated.Callsign,
                    updated.IsTracked,
                    updated.IsTrackedByMe,
                    updated.ControllerTracking?.Callsign,
                    updated.HandoffController?.Callsign);

                var jurisdictionChecker = ServiceProvider.GetRequiredService<IJurisdictionChecker>();

                jurisdictionChecker.RecordFdrOwner(updated.Callsign, updated.ControllerTracking?.Callsign);

                var record = jurisdictionChecker.GetOwnershipRecord(updated.Callsign);
                if (record?.PreviousOwner != Network.Callsign)
                    return;

                // We were the previous owner, process the handoff
                Log.Information("{Callsign} handoff to {NextController}", updated.Callsign, record.CurrentOwner);
                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Publish(new HandoffCompletedNotification(updated.Callsign, record.CurrentOwner));
            });

            // Ensure the next data authority is up-to-date
            _workQueue.Enqueue(async () =>
            {
                var handoffMediator = ServiceProvider.GetRequiredService<IMediator>();
                await handoffMediator.Send(new UpdateNextDataAuthorityForFdrRequest(updated));
            });

            // Re-build the CPDLC status label items
            _workQueue.Enqueue(() => RebuildCpdlcStatusLabelItems());

            // If this flight has any open dialogues that we have jurisdiction over, open the current messages window
            // Ensures flights handed off to us open the window so we can see their requests
            _workQueue.Enqueue(async () =>
            {
                var store = ServiceProvider.GetRequiredService<DialogueStore>();
                var jurisdictionChecker = ServiceProvider.GetRequiredService<IJurisdictionChecker>();

                var hasOpenDialogues = (await store.All(CancellationToken.None))
                    .Where(d => d.AircraftCallsign == updated.Callsign)
                    .Any(d => !d.IsClosed && !d.IsArchived && jurisdictionChecker.ShouldDisplayDialogue(d));

                if (hasOpenDialogues)
                {
                    var mediator = ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Send(new OpenCurrentMessagesWindowRequest());
                }
            });
        }
        catch (Exception ex)
        {
            AddError(ex);
        }
    }

    public void OnRadarTrackUpdate(RDP.RadarTrack updated) {}

    public CustomLabelItem? GetCustomLabelItem(
        string itemType,
        Track track,
        FDP2.FDR flightDataRecord,
        RDP.RadarTrack radarTrack)
    {
        try
        {
            if (itemType.StartsWith("CPDLCPLUGIN_TEXTSTATUS"))
                return _textStatusProvider.GetLabelItem(itemType, flightDataRecord);

            if (itemType.StartsWith("CPDLCPLUGIN_CPDLCSTATUS"))
                return _cpdlcStatusProvider.GetLabelItem(itemType, flightDataRecord);
        }
        catch (Exception ex)
        {
            AddError(ex, "Failed to generate custom label item");
        }

        return null;
    }

    public CustomStripItem? GetCustomStripItem(
        string itemType,
        Track track,
        FDP2.FDR flightDataRecord,
        RDP.RadarTrack radarTrack)
    {
        try
        {
            if (itemType.StartsWith("CPDLCPLUGIN_TEXTSTATUS"))
                return _textStatusProvider.GetStripItem(itemType, flightDataRecord);

            if (itemType.StartsWith("CPDLCPLUGIN_CPDLCSTATUS"))
                return _cpdlcStatusProvider.GetStripItem(itemType, flightDataRecord);
        }
        catch (Exception ex)
        {
            AddError(ex, "Failed to generate custom strip item");
        }

        return null;
    }

    public CustomColour? SelectASDTrackColour(Track track) => null;

    public CustomColour? SelectGroundTrackColour(Track track) => null;

    async Task RebuildCpdlcStatusLabelItems(CancellationToken cancellationToken = default)
    {
        try
        {
            var mediator =  ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new RebuildCpdlcStatusLabelItemsRequest(), cancellationToken);
        }
        catch (Exception ex)
        {
            AddError(ex);
        }
    }

    public void Receive(ConnectedAircraftChanged _)
    {
        try
        {
            _workQueue.Enqueue(() => RebuildCpdlcStatusLabelItems());
        }
        catch (Exception ex)
        {
            AddError(ex);
        }
    }

    public void Receive(DialogueChangedNotification dialogueChangedNotification)
    {
        try
        {
            _workQueue.Enqueue(async () =>
            {
                await RebuildCpdlcStatusLabelItems().ConfigureAwait(false);

                // Try to open the Current Messages Window if this dialogue is relevant to the controller
                var jurisdictionChecker = ServiceProvider.GetRequiredService<IJurisdictionChecker>();
                if (dialogueChangedNotification.Dialogue.IsArchived ||
                    !jurisdictionChecker.ShouldDisplayDialogue(dialogueChangedNotification.Dialogue))
                {
                    return;
                }

                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new OpenCurrentMessagesWindowRequest());
            });
        }
        catch (Exception ex)
        {
            AddError(ex);
        }
    }
}
