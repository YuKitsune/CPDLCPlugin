using CPDLCServer.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Auth.Administrators;

// The changes are saved when the unit of work is committed.
// Queries load the rows into the context, then read the local entities, so that the results include entities that are
// added in the unit of work and exclude entities that are removed in it.
public class AdministratorRepository(CpdlcDbContext dbContext) : IAdministratorRepository
{
    public Task Add(Administrator administrator, CancellationToken cancellationToken)
    {
        dbContext.Administrators.Add(administrator);
        return Task.CompletedTask;
    }

    public Task Remove(Administrator administrator, CancellationToken cancellationToken)
    {
        dbContext.Administrators.Remove(administrator);
        return Task.CompletedTask;
    }

    public async Task<Administrator?> FindByCid(int cid, CancellationToken cancellationToken)
    {
        return await dbContext.Administrators.FindAsync([cid], cancellationToken);
    }

    public async Task<Administrator[]> All(CancellationToken cancellationToken)
    {
        await dbContext.Administrators.LoadAsync(cancellationToken);
        return dbContext.Administrators.Local.OrderBy(a => a.Cid).ToArray();
    }

    public async Task<bool> Any(CancellationToken cancellationToken)
    {
        await dbContext.Administrators.LoadAsync(cancellationToken);
        return dbContext.Administrators.Local.Count > 0;
    }
}
