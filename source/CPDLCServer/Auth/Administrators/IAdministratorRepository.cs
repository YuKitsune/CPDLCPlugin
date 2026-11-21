namespace CPDLCServer.Auth.Administrators;

public interface IAdministratorRepository
{
    Task Add(Administrator administrator, CancellationToken cancellationToken);
    Task Remove(Administrator administrator, CancellationToken cancellationToken);
    Task<Administrator?> FindByCid(int cid, CancellationToken cancellationToken);
    Task<Administrator[]> All(CancellationToken cancellationToken);
    Task<bool> Any(CancellationToken cancellationToken);
}
