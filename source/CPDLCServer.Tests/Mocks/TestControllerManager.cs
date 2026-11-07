using CPDLCServer.Controllers;

namespace CPDLCServer.Tests.Mocks;

public class TestControllerRepository : IControllerRepository
{
    private readonly InMemoryControllerRepository _inner = new();

    public Task Add(ControllerInfo controller, CancellationToken cancellationToken)
    {
        return _inner.Add(controller, cancellationToken);
    }

    public Task<ControllerInfo?> FindByConnectionId(string connectionId, CancellationToken cancellationToken)
    {
        return _inner.FindByConnectionId(connectionId, cancellationToken);
    }

    public Task<ControllerInfo[]> All(CancellationToken cancellationToken)
    {
        return _inner.All(cancellationToken);
    }

    public Task<bool> RemoveByConnectionId(string connectionId, CancellationToken cancellationToken)
    {
        return _inner.RemoveByConnectionId(connectionId, cancellationToken);
    }
}
