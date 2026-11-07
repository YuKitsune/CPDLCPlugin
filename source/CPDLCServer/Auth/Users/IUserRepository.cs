namespace CPDLCServer.Auth.Users;

public interface IUserRepository
{
    Task Add(User user, CancellationToken cancellationToken);
    Task<User?> FindByUsername(string username, CancellationToken cancellationToken);
    Task<User?> FindById(Guid id, CancellationToken cancellationToken);
    Task<User[]> All(CancellationToken cancellationToken);
    Task<bool> Any(CancellationToken cancellationToken);
}
