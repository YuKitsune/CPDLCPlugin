using CPDLCServer.Logging;
using CPDLCServer.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;

namespace CPDLCServer.Infrastructure;

// The entry point for work that does not come from a handler: the hub, the ACARS connections and the background services.
// Each call runs in a new service scope, and commits the unit of work of that scope.
// Handlers use the IMediator of their own scope, so that nested requests and notifications are in the same unit of work.
public interface IUnitOfWorkMediator
{
    Task Send(IRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task Publish(INotification notification, CancellationToken cancellationToken = default);
}

// The units of work run at the same time. The database prevents conflicts:
// - Each changed entity has a version (concurrency token). If a different unit of work changed the entity after it was
//   read, the commit fails.
// - Unique indexes prevent two units of work from adding the same entity.
// When the commit fails, the unit of work runs again in a new scope, so that it reads the new data.
// The messages to controllers and aircraft are sent only after a successful commit, so a failed attempt sends nothing.
public class UnitOfWorkMediator(IServiceScopeFactory serviceScopeFactory, ILogger logger) : IUnitOfWorkMediator
{
    const int MaxAttempts = 3;

    public async Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        await Run(
            request.GetType(),
            async mediator =>
            {
                await mediator.Send(request, cancellationToken);
                return true;
            },
            cancellationToken);
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        return await Run(request.GetType(), mediator => mediator.Send(request, cancellationToken), cancellationToken);
    }

    public async Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        await Run(
            notification.GetType(),
            async mediator =>
            {
                await mediator.Publish(notification, cancellationToken);
                return true;
            },
            cancellationToken);
    }

    // The log entries of the unit of work get the area of the message. The nested messages are in the same area.
    async Task<TResult> Run<TResult>(Type messageType, Func<IMediator, Task<TResult>> action, CancellationToken cancellationToken)
    {
        using var _ = LogContext.PushProperty(LogArea.PropertyName, LogArea.ForType(messageType));

        for (var attempt = 1; ; attempt++)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<UnitOfWork>();

            try
            {
                var result = await action(mediator);
                await unitOfWork.Commit(cancellationToken);

                return result;
            }
            // DbUpdateConcurrencyException: a different unit of work changed or removed an entity.
            // DbUpdateException: a different unit of work added an entity with the same unique key.
            catch (DbUpdateException ex) when (attempt < MaxAttempts)
            {
                logger.Warning(
                    ex,
                    "Unit of work conflicted with a different unit of work (attempt {Attempt} of {MaxAttempts}), retrying",
                    attempt,
                    MaxAttempts);
            }
        }
    }
}
