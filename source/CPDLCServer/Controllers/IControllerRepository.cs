namespace CPDLCServer.Controllers;

public interface IControllerRepository
{
    Task Add(ControllerInfo controller, CancellationToken cancellationToken);
    Task<ControllerInfo?> FindByConnectionId(string connectionId, CancellationToken cancellationToken);
    Task<ControllerInfo[]> All(CancellationToken cancellationToken);
    Task<bool> RemoveByConnectionId(string connectionId, CancellationToken cancellationToken);
}
