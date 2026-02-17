using CPDLCServer.Exceptions;
using CPDLCServer.Infrastructure;
using CPDLCServer.Messages;
using MediatR;

namespace CPDLCServer.Clients;

public interface IClientManager
{
    Task<IAcarsClient> GetAcarsClient(string acarsClientId, CancellationToken cancellationToken);
}

public class ClientManager : BackgroundService, IClientManager
{
    readonly AcarsConfiguration[] _acarsConfigurations;
    readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    readonly Dictionary<string, AcarsClientHandle> _clients = [];
    readonly IClock _clock;
    readonly ILogger _logger;
    readonly IMediator _mediator;

    public ClientManager(
        IConfiguration configuration,
        IMediator mediator,
        IClock clock,
        ILogger logger)
    {
        var acarsConfigurationSection = configuration.GetSection("Acars");
        var configurationList = new List<AcarsConfiguration>();

        foreach (var section in acarsConfigurationSection.GetChildren())
        {
            var type = section["Type"];
            var config = type switch
            {
                "Hoppie" => section.Get<HoppiesConfiguration>(),
                _ => throw new NotSupportedException($"ACARS configuration type '{type}' is not supported")
            };

            if (config is not null)
            {
                configurationList.Add(config);
            }
        }

        _acarsConfigurations = configurationList.ToArray();

        _mediator = mediator;
        _clock = clock;
        _logger = logger.ForContext<ClientManager>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("Starting ACARS client manager with {Count} configurations", _acarsConfigurations.Length);

        foreach (var config in _acarsConfigurations)
        {
            await CreateClientWithRetry(config.ClientId, stoppingToken);
        }

        _logger.Information("All ACARS clients initialized");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.Information("Stopping ACARS client manager");
        }
    }

    async Task CreateClientWithRetry(string acarsClientId, CancellationToken cancellationToken)
    {
        const int maxRetries = 5;
        var retryDelay = TimeSpan.FromSeconds(1);

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var acarsClientHandle = await CreateAcarsClient(acarsClientId, cancellationToken);
                _clients.Add(acarsClientId, acarsClientHandle);

                _logger.Information("Successfully created ACARS client {ClientId}", acarsClientId);

                return;
            }
            catch (Exception ex) when (attempt < maxRetries - 1)
            {
                _logger.Warning(
                    ex,
                    "Failed to create ACARS client {ClientId} (attempt {Attempt}/{MaxRetries}). Retrying in {Delay}",
                    acarsClientId,
                    attempt + 1,
                    maxRetries,
                    retryDelay);

                await Task.Delay(retryDelay, cancellationToken);
                retryDelay *= 2;
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "Failed to create ACARS client {ClientId} after {MaxRetries} attempts. Client will not be available",
                    acarsClientId,
                    maxRetries);
                break;
            }
        }
    }

    public Task<IAcarsClient> GetAcarsClient(string acarsClientId, CancellationToken cancellationToken)
    {
        if (!_clients.TryGetValue(acarsClientId, out var acarsClientHandle))
        {
            throw new ConfigurationNotFoundException(acarsClientId);
        }

        return Task.FromResult(acarsClientHandle.Client);
    }

    async Task<AcarsClientHandle> CreateAcarsClient(string acarsClientId, CancellationToken cancellationToken)
    {
        var configuration = _acarsConfigurations.FirstOrDefault(c => c.ClientId == acarsClientId);
        if (configuration is null)
            throw new ConfigurationNotFoundException(acarsClientId);

        IAcarsClient acarsClient = configuration switch
        {
            HoppiesConfiguration hoppieConfig => CreateHoppieClient(hoppieConfig),
            _ => throw new NotSupportedException($"ACARS configuration type {configuration.GetType().Name} is not supported")
        };

        var stationId = configuration switch
        {
            HoppiesConfiguration hoppieConfig => hoppieConfig.StationIdentifier,
            _ => throw new NotSupportedException($"ACARS configuration type {configuration.GetType().Name} is not supported")
        };

        var subscribeTaskCancellationSource = new CancellationTokenSource();
        var subscribeTask = Subscribe(
            acarsClientId,
            stationId,
            acarsClient,
            _mediator,
            subscribeTaskCancellationSource.Token);

        var acarsClientHandle = new AcarsClientHandle(acarsClient, subscribeTask, subscribeTaskCancellationSource);

        await acarsClient.Connect(cancellationToken);

        _logger.Information("Connected to ACARS client {ClientId}", acarsClientId);

        return acarsClientHandle;
    }

    HoppieAcarsClient CreateHoppieClient(HoppiesConfiguration configuration)
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = configuration.Url;

        return new HoppieAcarsClient(
            configuration,
            httpClient,
            _clock,
            _logger.ForContext<HoppieAcarsClient>());
    }

    async Task Subscribe(string acarsClientId, string stationId, IAcarsClient acarsClient, IMediator mediator, CancellationToken cancellationToken)
    {
        var subscriptionLogger = _logger.ForContext("AcarsClientId", acarsClientId);
        await foreach (var downlinkMessage in acarsClient.MessageReader.ReadAllAsync(cancellationToken))
        {
            // TODO: Make this configurable
            var publishTimeoutCancellationTokenSource = new CancellationTokenSource();
            publishTimeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));

            try
            {
                await mediator.Publish(
                    new DownlinkReceivedNotification(
                        acarsClientId,
                        stationId,
                        downlinkMessage),
                    publishTimeoutCancellationTokenSource.Token);
            }
            catch (OperationCanceledException) when (publishTimeoutCancellationTokenSource.Token.IsCancellationRequested)
            {
                subscriptionLogger.Warning("Timeout handling downlink {Downlink}", downlinkMessage);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                subscriptionLogger.Information("Subscription canceled");
            }
            catch (Exception ex)
            {
                subscriptionLogger.Error(ex, "Failed to relay {MessageType}", downlinkMessage.GetType());
            }
        }
    }

    record AcarsClientHandle(
        IAcarsClient Client,
        Task SubscribeTask,
        CancellationTokenSource SubscribeCancellationTokenSource) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();

            await SubscribeCancellationTokenSource.CancelAsync();
            await SubscribeTask;

            SubscribeCancellationTokenSource.Dispose();
            SubscribeTask.Dispose();
        }
    }


    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Information("Stopping ACARS client manager and disposing all clients");

        await base.StopAsync(cancellationToken);

        foreach (var (_, clientHandle) in _clients)
        {
            await clientHandle.DisposeAsync();
        }

        _clients.Clear();
        _semaphoreSlim.Dispose();
    }
}
