using CPDLCServer.Auth.Users;

namespace CPDLCServer.Tests.Mocks;

public class TestUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public Task Add(User user, CancellationToken cancellationToken)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task<User?> FindByUsername(string username, CancellationToken cancellationToken)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.Username == username));
    }

    public Task<User?> FindById(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
    }

    public Task<User[]> All(CancellationToken cancellationToken)
    {
        return Task.FromResult(_users.OrderBy(u => u.Username).ToArray());
    }

    public Task<bool> Any(CancellationToken cancellationToken)
    {
        return Task.FromResult(_users.Count > 0);
    }
}
