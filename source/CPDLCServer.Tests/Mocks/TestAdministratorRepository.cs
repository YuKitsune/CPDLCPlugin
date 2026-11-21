using CPDLCServer.Auth.Administrators;

namespace CPDLCServer.Tests.Mocks;

public class TestAdministratorRepository : IAdministratorRepository
{
    private readonly List<Administrator> _administrators = [];

    public Task Add(Administrator administrator, CancellationToken cancellationToken)
    {
        _administrators.Add(administrator);
        return Task.CompletedTask;
    }

    public Task Remove(Administrator administrator, CancellationToken cancellationToken)
    {
        _administrators.Remove(administrator);
        return Task.CompletedTask;
    }

    public Task<Administrator?> FindByCid(int cid, CancellationToken cancellationToken)
    {
        return Task.FromResult(_administrators.FirstOrDefault(a => a.Cid == cid));
    }

    public Task<Administrator[]> All(CancellationToken cancellationToken)
    {
        return Task.FromResult(_administrators.OrderBy(a => a.Cid).ToArray());
    }

    public Task<bool> Any(CancellationToken cancellationToken)
    {
        return Task.FromResult(_administrators.Count > 0);
    }
}
