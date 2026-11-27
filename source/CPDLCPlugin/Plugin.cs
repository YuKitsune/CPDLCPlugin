using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Forms;
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
using vatSysToolbox;
using vatSysToolbox.BackgroundWork;
using vatSysToolbox.MediatR;
using vatSysToolbox.Messaging;
using vatSysToolbox.Windows;

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

    /// <summary>
    /// The name that other plugins use to send messages to this plugin. See <see cref="CpdlcMessages"/>.
    /// </summary>
    public const string MailboxName = "dev.eoinmotherway.cpdlc";

    readonly CpdlcStatusLabelItemProvider _cpdlcStatusProvider;
    readonly TextStatusLabelItemProvider _textStatusProvider;
    readonly ColourCache _colourCache;

    readonly IErrorReporter _errorReporter;
    readonly WorkQueue _workQueue;

    // 1 if a rebuild of the CPDLC status label items is in the work queue, and 0 if not
    int _isLabelRebuildQueued;

    PluginConfiguration? _configuration;

    BackgroundTask? _ndaRecalculationWorker;

    ToolStripMenuItem? _installationMenuItem;
    bool _labelsInstalled;

    const string InstallMenuLabel = "Install label items";
    const string UninstallMenuLabel = "Uninstall label items";

    string IPlugin.Name => Name;

    IServiceProvider ServiceProvider { get; set; }

    public SignalRConnectionManager? ConnectionManager { get; set; }

    public Plugin()
    {
        _colourCache = CacheLabelColours();
        _cpdlcStatusProvider = new CpdlcStatusLabelItemProvider();

        var configuration = ConfigurationLoader.Load();
        _configuration = configuration;
        ConfigureServices(configuration);

        _errorReporter = ServiceProvider.GetRequiredService<IErrorReporter>();
        _workQueue = ServiceProvider.GetRequiredService<WorkQueue>();
        _textStatusProvider = new TextStatusLabelItemProvider(_colourCache, _errorReporter);

        EnsureDpiAwareness();

        ApplyAdscTrackPatches();
        ApplyFakeAdscReportPatch();

        AddToolbarItems();

        CheckXmlInstallation();

        Network.Connected += NetworkConnected;
        Network.Disconnected += NetworkDisconnected;

        WeakReferenceMessenger.Default.Register<DialogueChangedNotification>(this);
        WeakReferenceMessenger.Default.Register<ConnectedAircraftChanged>(this);

        StartPluginMessenger();

        // Check the saved token, so that the SETUP window shows the user.
        // The server can be unavailable when vatSys starts, so a failure is not an error.
        _workQueue.Enqueue(async cancellationToken =>
        {
            try
            {
                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new CheckAuthenticationRequest(configuration.ServerEndpoint), cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to check the authentication with the CPDLC server");
            }
        });
    }

    void ConfigureServices(PluginConfiguration pluginConfiguration)
    {
        var logger = ConfigureLogger(pluginConfiguration);

        ServiceProvider = new ServiceCollection()
            .AddLogging(builder => builder.AddSerilog(logger))
            .AddErrorReporter(Name)
            .AddWindowManager()
            .AddWorkQueue()
            .AddPluginMessenger(MailboxName)
            .AddSingleton(logger)
            .AddSingleton(this) // TODO: Ick... Whatever we're relying on this for, move it into a separate service please.
            .AddSingleton(pluginConfiguration)
            .AddSingleton<IServerConnection, PluginServerConnection>()
            .AddSingleton<AuthenticationStore>()
            .AddSingleton<PluginAuthenticator>()
            .AddSingleton<IJurisdictionChecker, JurisdictionChecker>()
            .AddSingleton<IFlightDataProvider, VatSysFlightDataProvider>()
            .AddSingleton<AircraftConnectionStore>()
            .AddSingleton<AcarsStationStore>()
            .AddSingleton<AdscContractStore>()
            .AddSingleton<AdscReportStore>()
            .AddSingleton<AdscTrackDisplay>()
            .AddSingleton<DialogueStore>()
            .AddSingleton<SuspendedMessageStore>()
            .AddSingleton<WindowEvents>()
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

    void EnsureDpiAwareness()
    {
        try
        {
            DpiAwareness.EnsureDpiAwareness();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, "Cannot set the DPI awareness of vatSys");
        }
    }

    // Handles the messages from other plugins. See CpdlcMessages.
    void StartPluginMessenger()
    {
        try
        {
            var messenger = ServiceProvider.GetRequiredService<IPluginMessenger>();
            var mediator = ServiceProvider.GetRequiredService<IMediator>();
            messenger.SubscribeHandlers(typeof(Plugin).Assembly, mediator, new JsonMessageSerializer());
            messenger.Start();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, "Cannot receive the messages from other plugins");
        }
    }

    void ApplyAdscTrackPatches()
    {
        try
        {
            AdscTrackPatches.Apply(ServiceProvider.GetRequiredService<AdscTrackDisplay>());
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, "Cannot show ADS-C tracks on the ASD");
        }
    }

    void ApplyFakeAdscReportPatch()
    {
        try
        {
            FakeAdscReportPatch.Apply(SendFakeAdscReport, _errorReporter);
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, "Cannot use fake ADS-C reports");
        }
    }

    // Runs on the UI thread when the ASD is clicked
    void SendFakeAdscReport(Coordinate position)
    {
        var fdr = MMI.SelectedTrack?.GetFDR();
        if (fdr is null)
            throw new InvalidOperationException("Select a track with a flight plan before you send a fake ADS-C report");

        if (fdr.PRL == -1)
            throw new InvalidOperationException($"{fdr.Callsign} has no reported level");

        var now = DateTimeOffset.UtcNow;
        var report = new CPDLCServer.Contracts.AdscReportDto(
            fdr.Callsign,
            ConnectionManager?.StationIdentifier ?? string.Empty,
            now,
            position.Latitude,
            position.Longitude,
            fdr.PRL,
            now,
            CPDLCServer.Contracts.AdscReportSource.Hoppie);

        Log.Information("Sending fake ADS-C report for {Callsign} at {Latitude} {Longitude} {Altitude}",
            report.Callsign,
            report.Latitude,
            report.Longitude,
            report.Altitude);

        _workQueue.Enqueue(async cancellationToken =>
        {
            var mediator = ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Publish(new AdscReportReceivedNotification(report), cancellationToken);
        });
    }

    void NetworkConnected(object sender, EventArgs e)
    {
        Log.Information("Connected to VATSIM as {Callsign}", Network.Callsign);
        _workQueue.Enqueue(_ =>
        {
            StartNdaRecalculationWorker();
            return Task.CompletedTask;
        });

        if (_configuration is { AutoConnect: true })
        {
            var configuration = _configuration;
            _workQueue.Enqueue(async cancellationToken =>
            {
                Log.Information("Connecting to the CPDLC server automatically");
                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new ConnectRequest(configuration.ServerEndpoint, configuration.Stations.First()), cancellationToken);
            });
        }
    }

    void NetworkDisconnected(object sender, EventArgs e)
    {
        Log.Information("Disconnected from VATSIM");

        try
        {
            _workQueue.Enqueue(_ => StopNdaRecalculationWorker());

            _workQueue.Enqueue(async cancellationToken =>
            {
                var mediator =  ServiceProvider.GetService<IMediator>();
                if (mediator is null)
                    return;

                await mediator.Send(new DisconnectRequest(), cancellationToken).ConfigureAwait(false);
            });
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    void StartNdaRecalculationWorker()
    {
        var interval = TimeSpan.FromMinutes(_configuration?.NdaRecalculationIntervalMinutes ?? 1);
        Log.Information("Starting NDA recalculation worker (interval: {Interval})", interval);
        _ndaRecalculationWorker = new BackgroundTask(async cancellationToken =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, cancellationToken);
                    await RecalculateNextDataAuthorityForAllAircraft();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Log.Verbose("NDA recalculation worker cancellation requested");
                }
                catch (Exception ex)
                {
                    _errorReporter.ReportError(ex);
                }
            }
        });
        _ndaRecalculationWorker.Start();
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
            await mediator.Send(new UpdateNextDataAuthorityRequest(fdr.Callsign, fdr.GetSectorEntries()));
        }
    }

    ColourCache CacheLabelColours()
    {
        // Need to cache these for thread-safe access

        var downlinkColor = VatSysColours.GetColour(Colours.Identities.CPDLCDownlink).Color;
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
            _workQueue.Enqueue(async cancellationToken =>
            {
                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new OpenSetupWindowRequest(), cancellationToken);
            });
        };

        MMI.AddCustomMenuItem(setupMenuItem);

        var currentMessagesMenuItem = new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            menuItemCategory,
            new ToolStripMenuItem("Current Messages"));
        currentMessagesMenuItem.Item.Click += (_, _) =>
        {
            _workQueue.Enqueue(async cancellationToken =>
            {
                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new OpenCurrentMessagesWindowRequest(OpenedAutomatically: false), cancellationToken);
            });
        };

        MMI.AddCustomMenuItem(currentMessagesMenuItem);

        var historyMenuItem = new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            menuItemCategory,
            new ToolStripMenuItem("History"));
        historyMenuItem.Item.Click += (_, _) =>
        {
            _workQueue.Enqueue(async cancellationToken =>
            {
                var selectedCallsign = MMI.SelectedTrack?.GetFDR()?.Callsign;

                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new OpenHistoryWindowRequest(selectedCallsign), cancellationToken);
            });
        };

        MMI.AddCustomMenuItem(historyMenuItem);

        var adsContractMenuItem = new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            menuItemCategory,
            new ToolStripMenuItem("ADS Contract"));
        adsContractMenuItem.Item.Click += (_, _) =>
        {
            _workQueue.Enqueue(async cancellationToken =>
            {
                var selectedCallsign = MMI.SelectedTrack?.GetFDR()?.Callsign;

                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new OpenADSContractWindowRequest(selectedCallsign), cancellationToken);
            });
        };

        MMI.AddCustomMenuItem(adsContractMenuItem);

#if DEBUG
        var editorMenuItem = new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            menuItemCategory,
            new ToolStripMenuItem("CPDLC Editor"));
        editorMenuItem.Item.Click += (_, _) =>
        {
            _workQueue.Enqueue(async cancellationToken =>
            {
                var selectedCallsign = MMI.SelectedTrack?.GetFDR()?.Callsign;
                if (selectedCallsign is null)
                    throw new InvalidOperationException("Select a track with a flight plan before you open the CPDLC Editor");

                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new OpenEditorWindowRequest(selectedCallsign), cancellationToken);
            });
        };

        MMI.AddCustomMenuItem(editorMenuItem);

        var fakeAdscReportMenuItem = new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            menuItemCategory,
            new ToolStripMenuItem("Fake ADS-C Report"));
        fakeAdscReportMenuItem.Item.Click += (_, _) => FakeAdscReportPatch.Arm();

        MMI.AddCustomMenuItem(fakeAdscReportMenuItem);
#endif

        if (_configuration?.ShowInstallationMenuItems ?? true)
        {
            var status = XmlInstaller.GetStatus();
            if (!string.IsNullOrEmpty(status.ErrorMessage))
                return;

            _labelsInstalled = status.IsInstalled;

            var toolStripItem = new ToolStripMenuItem(_labelsInstalled ? UninstallMenuLabel : InstallMenuLabel);
            toolStripItem.Click += (_, _) =>
            {
                if (_labelsInstalled)
                    RunUninstall();
                else
                    RunInstall();
            };

            _installationMenuItem = toolStripItem;

            var menuItem = new CustomToolStripMenuItem(
                CustomToolStripMenuItemWindowType.Main,
                menuItemCategory,
                toolStripItem);
            MMI.AddCustomMenuItem(menuItem);
        }
    }

    void CheckXmlInstallation()
    {
        try
        {
            var status = XmlInstaller.GetStatus();
            if (!string.IsNullOrEmpty(status.ErrorMessage))
            {
                Log.Warning("Could not verify Labels.xml/Strips.xml installation: {Error}", status.ErrorMessage);
                return;
            }

            if (!status.IsInstalled)
            {
                _errorReporter.ReportError(new InvalidOperationException("CPDLC Label and Strip items must be installed. Please install them from the CPDLC menu."));
            }
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, "Failed to verify Labels.xml/Strips.xml installation");
        }
    }

    void RunInstall()
    {
        try
        {
            var result = XmlInstaller.Install();
            if (result.Succeeded)
            {
                SetInstallationMenuState(installed: true);
                System.Windows.Forms.MessageBox.Show(
                    "CPDLC label & strip items installed successfully.\n\nPlease restart vatSys to apply the changes.",
                    Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(
                    $"Failed to install CPDLC label & strip items.\n\n{result.ErrorMessage}",
                    Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, "Failed to install label & strip items");
        }
    }

    void SetInstallationMenuState(bool installed)
    {
        _labelsInstalled = installed;
        if (_installationMenuItem is not null)
            _installationMenuItem.Text = installed ? UninstallMenuLabel : InstallMenuLabel;
    }

    void RunUninstall()
    {
        try
        {
            var result = XmlInstaller.Uninstall();
            if (result.Succeeded)
            {
                SetInstallationMenuState(installed: false);
                System.Windows.Forms.MessageBox.Show(
                    "CPDLC label & strip items uninstalled successfully.\n\nPlease restart vatSys to apply the changes.",
                    Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(
                    $"Failed to uninstall CPDLC label & strip items.\n\n{result.ErrorMessage}",
                    Name,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex, "Failed to uninstall label & strip items");
        }
    }

    public void OnFDRUpdate(FDP2.FDR updated)
    {
        try
        {
            // Record the last known owner of each FDR
            _workQueue.Enqueue(async cancellationToken =>
            {
                Log.Verbose(
                    "{Callsign}: IsTracked {IsTracked}; IsTrackedByMe: {IsTrackedByMe}; Controller Tracking: {CurrentController}; Handoff Controller: {HandoffController}",
                    updated.Callsign,
                    updated.IsTracked,
                    updated.IsTrackedByMe,
                    updated.ControllerTracking?.Callsign,
                    updated.HandoffController?.Callsign);

                var jurisdictionChecker = ServiceProvider.GetRequiredService<IJurisdictionChecker>();

                var previousRecord = jurisdictionChecker.GetOwnershipRecord(updated.Callsign);
                jurisdictionChecker.RecordFdrOwner(updated.Callsign, updated.ControllerTracking?.Callsign);

                var record = jurisdictionChecker.GetOwnershipRecord(updated.Callsign);

                // Tell the Current Messages Window that the dialogues it must show can be different
                if (previousRecord?.CurrentOwner != record?.CurrentOwner)
                    WeakReferenceMessenger.Default.Send(new FdrOwnerChanged(updated.Callsign));

                if (record?.PreviousOwner != Network.Callsign)
                    return;

                // We were the previous owner, process the handoff
                Log.Information("{Callsign} handoff to {NextController}", updated.Callsign, record.CurrentOwner);
                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Publish(new HandoffCompletedNotification(updated.Callsign, record.CurrentOwner), cancellationToken);
            });

            // Ensure the next data authority is up-to-date
            _workQueue.Enqueue(async cancellationToken =>
            {
                var handoffMediator = ServiceProvider.GetRequiredService<IMediator>();
                await handoffMediator.Send(new UpdateNextDataAuthorityRequest(updated.Callsign, updated.GetSectorEntries()), cancellationToken);
            });

            // Re-build the CPDLC status label items
            QueueLabelRebuild();

            // If this flight has any dialogues that we have jurisdiction over, open the current messages window
            // Ensures flights handed off to us open the window so we can see their requests
            // A closed dialogue stays in the window until it is archived (e.g. a downlink that needs no response)
            _workQueue.Enqueue(async cancellationToken =>
            {
                var store = ServiceProvider.GetRequiredService<DialogueStore>();
                var jurisdictionChecker = ServiceProvider.GetRequiredService<IJurisdictionChecker>();

                var hasDialoguesToDisplay = (await store.All(cancellationToken))
                    .Where(d => d.AircraftCallsign == updated.Callsign)
                    .Any(d => !d.IsArchived && jurisdictionChecker.ShouldDisplayDialogue(d));

                // vatSys updates the FDRs often. If the window is open, do not post to the UI thread to open it again.
                var windowManager = ServiceProvider.GetRequiredService<WindowManager>();
                if (hasDialoguesToDisplay && !windowManager.IsOpen(WindowKeys.CurrentMessages))
                {
                    var mediator = ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Send(new OpenCurrentMessagesWindowRequest(OpenedAutomatically: true), cancellationToken);
                }
            });
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
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
            _errorReporter.ReportError(ex, "Failed to generate custom label item");
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
            _errorReporter.ReportError(ex, "Failed to generate custom strip item");
        }

        return null;
    }

    public CustomColour? SelectASDTrackColour(Track track) => null;

    public CustomColour? SelectGroundTrackColour(Track track) => null;

    // vatSys updates the FDRs often, and each rebuild reads all the FDRs. A rebuild reads the state when it runs. Thus,
    // if a rebuild is already in the work queue, do not queue another one.
    void QueueLabelRebuild()
    {
        if (Interlocked.Exchange(ref _isLabelRebuildQueued, 1) == 1)
            return;

        _workQueue.Enqueue(cancellationToken =>
        {
            // Clear the flag before the rebuild reads the state. Then a change during the rebuild queues a new rebuild.
            Interlocked.Exchange(ref _isLabelRebuildQueued, 0);
            return RebuildCpdlcStatusLabelItems(cancellationToken);
        });
    }

    async Task RebuildCpdlcStatusLabelItems(CancellationToken cancellationToken = default)
    {
        try
        {
            var mediator =  ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new RebuildCpdlcStatusLabelItemsRequest(), cancellationToken);
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    public void Receive(ConnectedAircraftChanged _)
    {
        try
        {
            QueueLabelRebuild();
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }

    public void Receive(DialogueChangedNotification dialogueChangedNotification)
    {
        try
        {
            // The rebuild is in the work queue before the next work item. Thus, the labels are rebuilt before the window
            // opens.
            QueueLabelRebuild();

            _workQueue.Enqueue(async cancellationToken =>
            {
                // Try to open the Current Messages Window if this dialogue is relevant to the controller
                var jurisdictionChecker = ServiceProvider.GetRequiredService<IJurisdictionChecker>();
                if (dialogueChangedNotification.Dialogue.IsArchived ||
                    !jurisdictionChecker.ShouldDisplayDialogue(dialogueChangedNotification.Dialogue))
                {
                    return;
                }

                // If the window is open, do not post to the UI thread to open and activate it again
                var windowManager = ServiceProvider.GetRequiredService<WindowManager>();
                if (windowManager.IsOpen(WindowKeys.CurrentMessages))
                    return;

                var mediator = ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new OpenCurrentMessagesWindowRequest(OpenedAutomatically: true), cancellationToken);
            });
        }
        catch (Exception ex)
        {
            _errorReporter.ReportError(ex);
        }
    }
}
