using CPDLCServer.Contracts;
using CPDLCServer.AcarsConnections.Stations;
using CPDLCServer.AdscContracts;
using CPDLCServer.AdscContracts.Reports;
using CPDLCServer.Controllers;
using CPDLCServer.Cpdlc.Connections;
using CPDLCServer.Cpdlc.Dialogues;
using CPDLCServer.Cpdlc.Downlinks;
using CPDLCServer.Cpdlc.Uplinks;
using CPDLCServer.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using CpdlcUplinkResponseType = CPDLCServer.Contracts.CpdlcUplinkResponseType;

namespace CPDLCServer.Web;

public class ControllerHub(
    IControllerRepository controllerRepository,
    IDialogueRepository dialogueRepository,
    IAcarsStationRepository acarsConnectedCallsignsRepository,
    IUnitOfWorkMediator mediator,
    ILogger logger)
    : Hub
{
    private readonly ILogger _logger = logger.ForContext<ControllerHub>();

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext is null)
        {
            throw new HubException("HTTP context not available");
        }

        // Read connection parameters from query string
        var query = httpContext.Request.Query;
        var stationId = query["stationId"].ToString().ToUpper();
        if (string.IsNullOrWhiteSpace(stationId))
        {
            throw new HubException("stationId must be provided");
        }

        var callsign = query["callsign"].ToString().ToUpper();
        if (string.IsNullOrWhiteSpace(callsign))
        {
            throw new HubException("callsign must be provided");
        }

        // TODO: Validate API key
        // var validationResult = await _apiKeyValidator.ValidateAsync(apiKey);
        // if (validationResult is null)
        // {
        //     _logger.Warning("Invalid API key attempt from {ConnectionId}", Context.ConnectionId);
        //     throw new HubException("Invalid API key");
        // }

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            Context.ConnectionId,
            stationId,
            callsign);

        await controllerRepository.Add(controller, Context.GetHttpContext()?.RequestAborted ?? CancellationToken.None);

        _logger.Information(
            "Controller connected: {Callsign}. ConnectionId: {ConnectionId}",
            callsign, Context.ConnectionId);

        await mediator.Publish(
            new ControllerConnectedNotification(
                controller.UserId,
                controller.StationId,
                controller.Callsign));

        await base.OnConnectedAsync();
    }

    public async Task BeginDialogue(
        string recipient,
        CpdlcUplinkResponseType responseType,
        string content)
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is null)
        {
            _logger.Warning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            throw new InvalidOperationException($"Controller not found for connection {Context.ConnectionId}");
        }

        var modelResponseType = responseType switch
        {
            CpdlcUplinkResponseType.NoResponse => Cpdlc.Uplinks.CpdlcUplinkResponseType.NoResponse,
            CpdlcUplinkResponseType.WilcoUnable => Cpdlc.Uplinks.CpdlcUplinkResponseType.WilcoUnable,
            CpdlcUplinkResponseType.AffirmativeNegative => Cpdlc.Uplinks.CpdlcUplinkResponseType.AffirmativeNegative,
            CpdlcUplinkResponseType.Roger => Cpdlc.Uplinks.CpdlcUplinkResponseType.Roger,
            _ => throw new ArgumentOutOfRangeException(nameof(responseType), responseType, null)
        };

        await mediator.Send(
            new BeginDialogueCommand(
                controller.Callsign,
                recipient,
                modelResponseType,
                content));
    }

    public async Task ReplyToDownlink(
        Guid dialogueId,
        int downlinkMessageId,
        CpdlcUplinkResponseType responseType,
        string content)
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is null)
        {
            _logger.Warning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            throw new InvalidOperationException($"Controller not found for connection {Context.ConnectionId}");
        }

        var modelResponseType = responseType switch
        {
            CpdlcUplinkResponseType.NoResponse => Cpdlc.Uplinks.CpdlcUplinkResponseType.NoResponse,
            CpdlcUplinkResponseType.WilcoUnable => Cpdlc.Uplinks.CpdlcUplinkResponseType.WilcoUnable,
            CpdlcUplinkResponseType.AffirmativeNegative => Cpdlc.Uplinks.CpdlcUplinkResponseType.AffirmativeNegative,
            CpdlcUplinkResponseType.Roger => Cpdlc.Uplinks.CpdlcUplinkResponseType.Roger,
            _ => throw new ArgumentOutOfRangeException(nameof(responseType), responseType, null)
        };

        await mediator.Send(
            new ReplyToDownlinkCommand(
                controller.Callsign,
                dialogueId,
                downlinkMessageId,
                modelResponseType,
                content));
    }

    public async Task RequestConnection(string callsign, string stationId)
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is null)
        {
            _logger.Warning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            throw new InvalidOperationException($"Controller not found for connection {Context.ConnectionId}");
        }

        await mediator.Send(new SendConnectionRequestCommand(callsign, stationId));
    }

    public async Task<AircraftConnectionDto[]> GetConnectedAircraft()
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is null)
        {
            _logger.Warning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            throw new InvalidOperationException($"Controller not found for connection {Context.ConnectionId}");
        }

        var result = await mediator.Send(new GetConnectedAircraftRequest());
        return result.Aircraft;
    }

    public async Task<ControllerConnectionDto[]> GetConnectedControllers()
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is null)
        {
            _logger.Warning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            throw new InvalidOperationException($"Controller not found for connection {Context.ConnectionId}");
        }

        var result = await mediator.Send(new GetConnectedControllersRequest());
        return result.Controllers;
    }

    public async Task<string[]> GetAcarsStations()
    {
        return await acarsConnectedCallsignsRepository.All(CancellationToken.None);
    }

    public async Task AcknowledgeDownlink(Guid dialogueId, int downlinkMessageId)
    {
        await mediator.Send(new AcknowledgeDownlinkCommand(dialogueId, downlinkMessageId));
    }

    public async Task AcknowledgeUplink(Guid dialogueId, int uplinkMessageId)
    {
        await mediator.Send(new AcknowledgeUplinkCommand(dialogueId, uplinkMessageId));
    }

    public async Task ArchiveDialogue(Guid dialogueId)
    {
        await mediator.Send(new ArchiveDialogueCommand(dialogueId));
    }

    public async Task UpdateNextDataAuthority(
        string callsign,
        string? nextDataAuthority,
        DateTimeOffset? expectedTransferTime)
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is null)
        {
            _logger.Warning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            throw new InvalidOperationException($"Controller not found for connection {Context.ConnectionId}");
        }

        await mediator.Send(
            new UpdateNextDataAuthorityCommand(
                controller.StationId,
                callsign,
                nextDataAuthority,
                expectedTransferTime));
    }

    // The default reporting interval of the ATSU requests the default contract. Any other interval requests a selected contract.
    public async Task RequestPeriodicAdscContract(string callsign, TimeSpan reportingInterval)
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is null)
        {
            _logger.Warning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            throw new InvalidOperationException($"Controller not found for connection {Context.ConnectionId}");
        }

        await mediator.Send(new RequestPeriodicAdscContractCommand(controller.StationId, callsign, reportingInterval));
    }

    public async Task RequestAdscDemandReport(string callsign)
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is null)
        {
            _logger.Warning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            throw new InvalidOperationException($"Controller not found for connection {Context.ConnectionId}");
        }

        await mediator.Send(new RequestAdscDemandReportCommand(controller.StationId, callsign));
    }

    public async Task CancelAdscContract(string callsign)
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is null)
        {
            _logger.Warning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            throw new InvalidOperationException($"Controller not found for connection {Context.ConnectionId}");
        }

        await mediator.Send(new CancelAdscContractCommand(controller.StationId, callsign));
    }

    public async Task<AdscContractDto[]> GetAdscContracts()
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is null)
        {
            _logger.Warning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            throw new InvalidOperationException($"Controller not found for connection {Context.ConnectionId}");
        }

        var result = await mediator.Send(new GetAdscContractsRequest());
        return result.Contracts;
    }

    public async Task<AdscReportDto[]> GetAdscReports()
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is null)
        {
            _logger.Warning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            throw new InvalidOperationException($"Controller not found for connection {Context.ConnectionId}");
        }

        var result = await mediator.Send(new GetAdscReportsRequest());
        return result.Reports;
    }

    public async Task<DialogueDto[]> GetAllDialogues()
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is null)
        {
            _logger.Warning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            throw new InvalidOperationException($"Controller not found for connection {Context.ConnectionId}");
        }

        var dialogues = await GetAllDialoguesFor(controller, CancellationToken.None);
        return dialogues;
    }

    // TODO: Move this into a MediatR handler
    async Task<DialogueDto[]> GetAllDialoguesFor(ControllerInfo controller, CancellationToken cancellationToken)
    {
        var dialogues = await dialogueRepository.All(cancellationToken);
        return dialogues.Select(DialogueConverter.ToDto).ToArray();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is not null)
        {
            await controllerRepository.RemoveByConnectionId(Context.ConnectionId,  CancellationToken.None);
            _logger.Information(
                "Controller disconnected: {Callsign} (ConnectionId: {ConnectionId})",
                controller.Callsign, Context.ConnectionId);

            await mediator.Publish(new ControllerDisconnectedNotification(controller.UserId, controller.Callsign));
        }

        await base.OnDisconnectedAsync(exception);
    }
}
