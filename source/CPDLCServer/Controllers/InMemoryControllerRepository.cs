using CPDLCServer.Infrastructure;

namespace CPDLCServer.Controllers;

public class InMemoryControllerRepository : IControllerRepository
{
    private readonly Dictionary<string, ControllerInfo> _controllers = new();
    readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task Add(ControllerInfo controller, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            _controllers.TryAdd(controller.ConnectionId, controller);
        }
    }

    public async Task<ControllerInfo?> FindByConnectionId(string connectionId, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            _controllers.TryGetValue(connectionId, out var controller);
            return controller;
        }
    }

    public async Task<ControllerInfo[]> All(CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _controllers.Values.ToArray();
        }
    }

    public async Task<bool> RemoveByConnectionId(string connectionId, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _controllers.Remove(connectionId);
        }
    }
}
