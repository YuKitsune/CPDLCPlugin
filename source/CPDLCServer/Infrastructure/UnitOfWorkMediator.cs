using CPDLCServer.Persistence;
using MediatR;

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

public class UnitOfWorkMediator(IServiceScopeFactory serviceScopeFactory) : IUnitOfWorkMediator, IDisposable
{
    // Only one unit of work runs at a time.
    // Two units of work cannot then change the same entity, and the messages are sent in the order of the changes.
    readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task Send(IRequest request, CancellationToken cancellationToken = default)
    {
        await Run(
            async mediator =>
            {
                await mediator.Send(request, cancellationToken);
                return true;
            },
            cancellationToken);
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        return await Run(mediator => mediator.Send(request, cancellationToken), cancellationToken);
    }

    public async Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        await Run(
            async mediator =>
            {
                await mediator.Publish(notification, cancellationToken);
                return true;
            },
            cancellationToken);
    }

    async Task<TResult> Run<TResult>(Func<IMediator, Task<TResult>> action, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<UnitOfWork>();

            var result = await action(mediator);
            await unitOfWork.Commit(cancellationToken);

            return result;
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
