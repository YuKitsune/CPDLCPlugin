using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CPDLCServer.Infrastructure;
using CPDLCServer.Settings;

namespace CPDLCServer.VatsimStatus;

/// <summary>
/// Downloads the VATSIM status data, then makes the ADS-C reports that are due.
/// Not all aircraft support ADS-C over Hoppie's network, so the server uses the VATSIM data for these aircraft.
/// </summary>
public class VatsimStatusService : IHostedService, IDisposable
{
    readonly TimeSpan _errorInterval = TimeSpan.FromMinutes(1);

    readonly IVatsimPilotRepository _vatsimPilotRepository;
    readonly IUnitOfWorkMediator _mediator;
    readonly ILogger _logger;
    readonly HttpClient _httpClient = new();

    CancellationTokenSource? _cancellationTokenSource;
    Task? _task;

    public VatsimStatusService(
        IVatsimPilotRepository vatsimPilotRepository,
        IUnitOfWorkMediator mediator,
        ILogger logger)
    {
        _vatsimPilotRepository = vatsimPilotRepository;
        _mediator = mediator;
        _logger = logger.ForContext<VatsimStatusService>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource is not null || _task is not null)
            throw new Exception("Already started");

        _cancellationTokenSource = new CancellationTokenSource();
        _task = DoWork(_cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource is null || _task is null)
            throw new Exception("Already stopped");

        await _cancellationTokenSource.CancelAsync();
        await _task;

        _cancellationTokenSource = null;
        _task = null;
    }

    async Task DoWork(CancellationToken stoppingToken)
    {
        _logger.Information("Starting VATSIM status service");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // The settings can change while the server is running, so read them each time
                    var settings = await _mediator.Send(new GetServerSettingsRequest(), stoppingToken);
                    var pollingInterval = TimeSpan.FromSeconds(settings.VatsimStatusPollingIntervalSeconds);

                    if (!settings.VatsimStatusEnabled)
                    {
                        _logger.Verbose("ADS-C reports from VATSIM data are disabled");
                        await Task.Delay(pollingInterval, stoppingToken);
                        continue;
                    }

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    timeoutCts.CancelAfter(TimeSpan.FromMinutes(1));

                    await DownloadPilots(settings.VatsimStatusUrl, timeoutCts.Token);
                    await _mediator.Send(new ProduceVatsimAdscReportsRequest(), timeoutCts.Token);

                    await Task.Delay(pollingInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error downloading VATSIM status data");
                    await Task.Delay(_errorInterval, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected on shutdown
        }
        catch (Exception exception)
        {
            _logger.Fatal(exception, "VatsimStatusService failed");
        }

        _logger.Information("Stopped VATSIM status service");
    }

    async Task DownloadPilots(Uri url, CancellationToken cancellationToken)
    {
        _logger.Debug("Downloading VATSIM status data");

        var data = await _httpClient.GetFromJsonAsync<VatsimData>(url, cancellationToken);
        if (data is null)
            throw new Exception("VATSIM status data is empty");

        var pilots = data.Pilots
            .Select(p => new VatsimPilot(
                p.Callsign,
                p.Latitude,
                p.Longitude,
                p.Altitude,
                p.LastUpdated,
                VatsimPilot.HasAdscEquipment(p.FlightPlan?.Aircraft)))
            .ToArray();

        await _vatsimPilotRepository.Replace(pilots, cancellationToken);

        _logger.Debug("Downloaded {PilotCount} pilot(s) from VATSIM status data", pilots.Length);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _cancellationTokenSource?.Dispose();
        _task?.Dispose();
    }

    // Only the fields that the server uses
    record VatsimData(
        [property: JsonPropertyName("pilots")] VatsimDataPilot[] Pilots);

    record VatsimDataPilot(
        [property: JsonPropertyName("callsign")] string Callsign,
        [property: JsonPropertyName("latitude")] double Latitude,
        [property: JsonPropertyName("longitude")] double Longitude,
        [property: JsonPropertyName("altitude")] int Altitude,
        [property: JsonPropertyName("last_updated")] DateTimeOffset LastUpdated,
        [property: JsonPropertyName("flight_plan")] VatsimDataFlightPlan? FlightPlan);

    record VatsimDataFlightPlan(
        [property: JsonPropertyName("aircraft")] string? Aircraft);
}
