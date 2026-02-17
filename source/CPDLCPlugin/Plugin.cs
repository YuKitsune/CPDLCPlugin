using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using CPDLCPlugin.Configuration;
using CPDLCPlugin.Extensions;
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
// TODO: Strip items
// TODO: ADS-C

[Export(typeof(IPlugin))]
public class Plugin : ILabelPlugin, IRecipient<DialogueChangedNotification>, IRecipient<ConnectedAircraftChanged>
{
#if DEBUG
    public const string Name = "CPDLC Plugin - Debug";
#else
    public const string Name = "CPDLC Plugin";
#endif

    static readonly Dictionary<string, DateTimeOffset> ErrorMessages = new();

    // Cache for CustomStripOrLabelItem to avoid expensive lookups on every label update
    readonly LabelItemCache _labelItemCache = new();
    readonly ColourCache _colourCache;

    readonly Channel<Func<Task>> _workQueue = Channel.CreateUnbounded<Func<Task>>();
    readonly Task _worker;

    string IPlugin.Name => Name;

    IServiceProvider ServiceProvider { get; set; }

    public SignalRConnectionManager? ConnectionManager { get; set; }

    public Plugin()
    {
        DpiAwareness.EnsureDpiAwareness();

        ConfigureTheme();
        _colourCache = CacheLabelColours();

        var configuration = ConfigurationLoader.Load();
        ConfigureServices(configuration);

        AddToolbarItems();

        Network.Connected += NetworkConnected;
        Network.Disconnected += NetworkDisconnected;

        WeakReferenceMessenger.Default.Register<DialogueChangedNotification>(this);
        WeakReferenceMessenger.Default.Register<ConnectedAircraftChanged>(this);

        _worker = Worker(CancellationToken.None);
    }

    async Task Worker(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var work = await _workQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                await work().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                try
                {
                    AddError(ex);
                }
                catch
                {
                    // Ignore errors during error reporting
                }
            }
        }
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
            .AddSingleton<ControllerConnectionStore>()
            .AddSingleton<WindowManager>()
            .AddSingleton<DialogueStore>()
            .AddSingleton<SuspendedMessageStore>()
            .AddSingleton(_labelItemCache)
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

        // TODO: Connect if auto-connect enabled
    }

    void NetworkDisconnected(object sender, EventArgs e)
    {
        Log.Information("Disconnected from VATSIM");

        try
        {
            _workQueue.Writer.TryWrite(async () =>
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
            _workQueue.Writer.TryWrite(async () =>
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
            _workQueue.Writer.TryWrite(async () =>
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
            _workQueue.Writer.TryWrite(async () =>
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
            _workQueue.Writer.TryWrite(() =>
            {
                if (updated.ControllerTracking is not null)
                {
                    var jurisdictionChecker = ServiceProvider.GetRequiredService<IJurisdictionChecker>();
                    jurisdictionChecker.RecordFdrOwner(updated.Callsign, updated.ControllerTracking.Callsign);
                }

                return Task.CompletedTask;
            });

            // Re-build the label item cache
            _workQueue.Writer.TryWrite(() => RebuildLabelItemCache());

            // If this flight has any open dialogues, open the current messages window
            // Ensures flights handed off to us open the window so we can see their requests
            _workQueue.Writer.TryWrite(async () =>
            {
                var store = ServiceProvider.GetRequiredService<DialogueStore>();

                var hasOpenDialogues = (await store.All(CancellationToken.None))
                    .Where(d => d.AircraftCallsign == updated.Callsign)
                    .Any(d => !d.IsClosed || !d.IsArchived);

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
            {
                return GetTextStatusLabelItem(itemType, flightDataRecord);
            }

            if (itemType.StartsWith("CPDLCPLUGIN_CPDLCSTATUS"))
            {
                return GetCpdlcStatusLabelItem(itemType, flightDataRecord);
            }
        }
        catch (Exception ex)
        {
            AddError(ex, "Failed to generate custom label item");
        }

        return null;
    }

    CustomLabelItem? GetTextStatusLabelItem(string itemType, FDP2.FDR? flightDataRecord)
    {
        // vatSys bug: Custom background colours can't be drawn selectively.
        // vatSys won't draw the custom background if the original colour (specified in the Labels.xml file) is transparent (or empty).
        // To work around this, we define two label items. One with the background, and one without.
        // If we need to draw a custom background colour, we return `null` for the one without the background.

        if (flightDataRecord is null)
            return null;

        var lastTextMessage = Network.GetRadioMessages
            .LastOrDefault(r => r.Address == flightDataRecord.Callsign && !r.Acknowledged);

        string? text = null;
        CustomColour? backgroundColour = null;

        if (flightDataRecord.TextOnly)
        {
            text = "T";
        }
        else if (flightDataRecord.ReceiveOnly)
        {
            text = "R";
        }
        else if (lastTextMessage is not null)
        {
            // Only show "V" when there is an unacknowledged message
            text = "V";
            backgroundColour = _colourCache.DownlinkBackgroundColour;
        }

        // Don't take up the space if it's not necessary
        if (text is null)
            return null;

        // vatSys bug: custom background colours can't be drawn selectively.
        // To work around this, we define two label items. One with the background, and one without.
        if (backgroundColour is not null && itemType != "CPDLCPLUGIN_TEXTSTATUS_BG")
            return null;

        if (backgroundColour is null && itemType != "CPDLCPLUGIN_TEXTSTATUS")
            return null;

        var textLabelItem = new CustomLabelItem
        {
            Type = itemType,
            Text = text,
            Border = BorderFlags.All
        };

        if (backgroundColour is not null)
        {
            textLabelItem.BackColourIdentity = Colours.Identities.Custom;
            textLabelItem.CustomBackColour = backgroundColour;
        }

        // Left-click to open the CPDLC Menu
        textLabelItem.OnMouseClick = args =>
        {
            try
            {
                if (args.Button != CustomLabelItemMouseButton.Left)
                    return;

                if (lastTextMessage is not null)
                {
                    MMI.OpenCPDLCMenu(lastTextMessage);
                }
                else
                {
                    MMI.OpenCPDLCWindow(flightDataRecord);
                }

                args.Handled = true;
            }
            catch (Exception ex)
            {
                AddError(ex, "Failed to handle label item click");
            }
        };

        return textLabelItem;
    }

    CustomLabelItem? GetCpdlcStatusLabelItem(string itemType, FDP2.FDR flightDataRecord)
    {
        // vatSys bug: Custom background colours can't be drawn selectively.
        // vatSys won't draw the custom background if the original colour (specified in the Labels.xml file) is transparent (or empty).
        // To work around this, we define two label items. One with the background, and one without.
        // If we need to draw a custom background colour, we return `null` for the one without the background.

        // Blank by default
        var labelItem = new CustomLabelItem
        {
            Type = itemType,
            Text = " "
        };

        var customItem = GetCustomStripOrLabelItem(flightDataRecord);
        if (customItem is null)
        {
            if (itemType == "CPDLCPLUGIN_CPDLCSTATUS_BG")
                return null;

            return labelItem;
        }

        labelItem.Text = customItem.Text;
        if (customItem.BackgroundColour is not null && itemType != "CPDLCPLUGIN_CPDLCSTATUS_BG")
            return null;

        if (customItem.BackgroundColour is null && itemType != "CPDLCPLUGIN_CPDLCSTATUS")
            return null;

        if (customItem.BackgroundColour is not null)
        {
            labelItem.BackColourIdentity = Colours.Identities.Custom;
            labelItem.CustomBackColour = customItem.BackgroundColour;
        }

        if (customItem.ForegroundColour is not null)
        {
            labelItem.ForeColourIdentity = Colours.Identities.Custom;
            labelItem.CustomForeColour = customItem.ForegroundColour;
        }

        labelItem.OnMouseClick = args =>
        {
            try
            {
                if (args.Button != CustomLabelItemMouseButton.Left)
                    return;

                customItem.LeftClickCallback();
                args.Handled = true;
            }
            catch (Exception ex)
            {
                AddError(ex, "Failed to handle CPDLC status label click");
            }
        };

        return labelItem;
    }

    CustomStripOrLabelItem? GetCustomStripOrLabelItem(FDP2.FDR flightDataRecord)
    {
        try
        {
            return _labelItemCache.Find(flightDataRecord.Callsign);
        }
        catch (Exception ex)
        {
            AddError(ex);
            return null;
        }
    }

    public CustomColour? SelectASDTrackColour(Track track) => null;

    public CustomColour? SelectGroundTrackColour(Track track) => null;

    async Task RebuildLabelItemCache(CancellationToken cancellationToken = default)
    {
        try
        {
            var mediator =  ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new RebuildLabelItemCacheRequest(), cancellationToken);
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
            _workQueue.Writer.TryWrite(() => RebuildLabelItemCache());
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
            _workQueue.Writer.TryWrite(async () =>
            {
                await RebuildLabelItemCache().ConfigureAwait(false);

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
