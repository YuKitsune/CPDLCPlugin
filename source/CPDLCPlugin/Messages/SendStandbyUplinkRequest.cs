using CPDLC.Messages;
using MediatR;
using Serilog;

namespace CPDLCPlugin.Messages;

public record SendStandbyUplinkRequest(Guid DialogueId, int DownlinkMessageId, string Recipient) : IRequest;
public record SendDeferredUplinkRequest(Guid DialogueId, int DownlinkMessageId, string Recipient) : IRequest;

// ReasonElementId is the ID of a "DUE TO ..." element that follows UNABLE, for example DUE TO TRAFFIC
public record SendUnableUplinkRequest(Guid DialogueId, int DownlinkMessageId, string Recipient, int? ReasonElementId = null) : IRequest;

public class SendStandbyUplinkRequestHandler(IMediator mediator, ILogger logger)
    : IRequestHandler<SendStandbyUplinkRequest>
{
    public async Task Handle(SendStandbyUplinkRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Sending STANDBY response to {Recipient} for downlink {DownlinkMessageId}",
            request.Recipient, request.DownlinkMessageId);

        await mediator.Send(
            new ReplyToDownlinkRequest(
                request.Recipient,
                request.DialogueId,
                request.DownlinkMessageId,
                [new UplinkMessageElement(UplinkElementIds.Standby, [])],
                false),
            cancellationToken);
    }
}

public class SendDeferredUplinkRequestHandler(IMediator mediator, ILogger logger)
    : IRequestHandler<SendDeferredUplinkRequest>
{
    public async Task Handle(SendDeferredUplinkRequest request, CancellationToken cancellationToken)
    {
        logger.Information("Sending REQUEST DEFERRED response to {Recipient} for downlink {DownlinkMessageId}",
            request.Recipient, request.DownlinkMessageId);

        await mediator.Send(
            new ReplyToDownlinkRequest(
                request.Recipient,
                request.DialogueId,
                request.DownlinkMessageId,
                [new UplinkMessageElement(UplinkElementIds.RequestDeferred, [])],
                false),
            cancellationToken);
    }
}

public class SendUnableUplinkRequestHandler(IMediator mediator, ILogger logger)
    : IRequestHandler<SendUnableUplinkRequest>
{
    public async Task Handle(SendUnableUplinkRequest request, CancellationToken cancellationToken)
    {
        var elements = new List<UplinkMessageElement> { new(UplinkElementIds.Unable, []) };
        if (request.ReasonElementId is not null)
        {
            elements.Add(new UplinkMessageElement(request.ReasonElementId.Value, []));
        }

        logger.Information("Sending UNABLE response to {Recipient} for downlink {DownlinkMessageId} (Reason: {ReasonElementId})",
            request.Recipient, request.DownlinkMessageId, request.ReasonElementId?.ToString() ?? "none");

        await mediator.Send(
            new ReplyToDownlinkRequest(
                request.Recipient,
                request.DialogueId,
                request.DownlinkMessageId,
                elements.ToArray(),
                false),
            cancellationToken);
    }
}
