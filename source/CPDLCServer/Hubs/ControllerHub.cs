using CPDLCServer.Contracts;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using CpdlcUplinkResponseType = CPDLCServer.Contracts.CpdlcUplinkResponseType;

namespace CPDLCServer.Hubs;

public class ControllerHub(
    IControllerRepository controllerRepository,
    IDialogueRepository dialogueRepository,
    IAcarsStationRepository acarsConnectedCallsignsRepository,
    IMediator mediator,
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

        // Validate API key
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
            callsign,
            "TEST");
            // validationResult.VatsimCid);

        await controllerRepository.Add(controller, Context.GetHttpContext()?.RequestAborted ?? CancellationToken.None);

        _logger.Information(
            "Controller connected: {Callsign} (VATSIM CID: {VatsimCid}; ConnectionId: {ConnectionId})",
            callsign, "TEST", Context.ConnectionId);

        await mediator.Publish(
            new ControllerConnectedNotification(
                controller.UserId,
                controller.StationId,
                controller.Callsign));

        await base.OnConnectedAsync();
    }

    public async Task<UplinkMessageDto> BeginDialogue(
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
            CpdlcUplinkResponseType.NoResponse => Model.CpdlcUplinkResponseType.NoResponse,
            CpdlcUplinkResponseType.WilcoUnable => Model.CpdlcUplinkResponseType.WilcoUnable,
            CpdlcUplinkResponseType.AffirmativeNegative => Model.CpdlcUplinkResponseType.AffirmativeNegative,
            CpdlcUplinkResponseType.Roger => Model.CpdlcUplinkResponseType.Roger,
            _ => throw new ArgumentOutOfRangeException(nameof(responseType), responseType, null)
        };

        var result = await mediator.Send(new BeginDialogueCommand(
            controller.Callsign,
            recipient,
            modelResponseType,
            content));

        return DialogueConverter.ToDto(result.UplinkMessage);
    }

    public async Task<UplinkMessageDto> ReplyToDownlink(
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
            CpdlcUplinkResponseType.NoResponse => Model.CpdlcUplinkResponseType.NoResponse,
            CpdlcUplinkResponseType.WilcoUnable => Model.CpdlcUplinkResponseType.WilcoUnable,
            CpdlcUplinkResponseType.AffirmativeNegative => Model.CpdlcUplinkResponseType.AffirmativeNegative,
            CpdlcUplinkResponseType.Roger => Model.CpdlcUplinkResponseType.Roger,
            _ => throw new ArgumentOutOfRangeException(nameof(responseType), responseType, null)
        };

        var result = await mediator.Send(new ReplyToDownlinkCommand(
            controller.Callsign,
            dialogueId,
            downlinkMessageId,
            modelResponseType,
            content));

        return DialogueConverter.ToDto(result.UplinkMessage);
    }

    public async Task<AircraftConnectionDto[]> GetConnectedAircraft()
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is null)
        {
            _logger.Warning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            throw new InvalidOperationException($"Controller not found for connection {Context.ConnectionId}");
        }

        var query = new GetConnectedAircraftRequest();
        var result = await mediator.Send(query);

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

        var query = new GetConnectedControllersRequest();
        var result = await mediator.Send(query);

        return result.Controllers;
    }

    public async Task<string[]> GetAcarsStations()
    {
        return await acarsConnectedCallsignsRepository.All(CancellationToken.None);
    }

    public async Task AcknowledgeDownlink(Guid dialogueId, int downlinkMessageId)
    {
        var command = new AcknowledgeDownlinkCommand(dialogueId, downlinkMessageId);
        await mediator.Send(command);

        _logger.Information(
            "Controller acknowledged downlink {MessageId} in dialogue {DialogueId}",
            downlinkMessageId,
            dialogueId);
    }

    public async Task AcknowledgeUplink(Guid dialogueId, int uplinkMessageId)
    {
        var command = new AcknowledgeUplinkCommand(dialogueId, uplinkMessageId);
        await mediator.Send(command);

        _logger.Information(
            "Controller acknowledged uplink {MessageId} in dialogue {DialogueId}",
            uplinkMessageId,
            dialogueId);
    }

    public async Task ArchiveDialogue(Guid dialogueId)
    {
        var command = new ArchiveDialogueCommand(dialogueId);
        await mediator.Send(command);

        _logger.Information(
            "Controller manually archived dialogue {DialogueId}",
            dialogueId);
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

        var command = new UpdateNextDataAuthorityCommand(
            controller.StationId,
            callsign,
            nextDataAuthority,
            expectedTransferTime);

        await mediator.Send(command);

        _logger.Information(
            "Controller {Callsign} updated next data authority for {AircraftCallsign} to {NextDataAuthority}",
            controller.Callsign,
            callsign,
            nextDataAuthority ?? "(none)");
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

        _logger.Information(
            "Sending {DialogueCount} dialogues to controller {Callsign}",
            dialogues.Length,
            controller.Callsign);

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
