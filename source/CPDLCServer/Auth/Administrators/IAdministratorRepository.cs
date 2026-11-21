namespace CPDLCServer.Auth.Administrators;

public interface IAdministratorRepository
{
    Task Add(Administrator administrator, CancellationToken cancellationToken);
    Task Remove(Administrator administrator, CancellationToken cancellationToken);
    Task<Administrator?> FindByCid(string cid, CancellationToken cancellationToken);
    Task<Administrator[]> All(CancellationToken cancellationToken);
    Task<bool> Any(CancellationToken cancellationToken);
}
