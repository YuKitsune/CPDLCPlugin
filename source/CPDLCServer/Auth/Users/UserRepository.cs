using CPDLCServer.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Auth.Users;

// The changes are saved when the unit of work is committed.
// Queries load the rows into the context, then read the local entities, so that the results include entities that are
// added in the unit of work and exclude entities that are removed in it.
public class UserRepository(CpdlcDbContext dbContext) : IUserRepository
{
    public Task Add(User user, CancellationToken cancellationToken)
    {
        dbContext.Users.Add(user);
        return Task.CompletedTask;
    }

    public async Task<User?> FindByUsername(string username, CancellationToken cancellationToken)
    {
        await dbContext.Users
            .Where(u => u.Username == username)
            .LoadAsync(cancellationToken);

        return dbContext.Users.Local.FirstOrDefault(u => u.Username == username);
    }

    public async Task<User?> FindById(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Users.FindAsync([id], cancellationToken);
    }

    public async Task<User[]> All(CancellationToken cancellationToken)
    {
        await dbContext.Users.LoadAsync(cancellationToken);
        return dbContext.Users.Local.OrderBy(u => u.Username).ToArray();
    }

    public async Task<bool> Any(CancellationToken cancellationToken)
    {
        await dbContext.Users.LoadAsync(cancellationToken);
        return dbContext.Users.Local.Count > 0;
    }
}
