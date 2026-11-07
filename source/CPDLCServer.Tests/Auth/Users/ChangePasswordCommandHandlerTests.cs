using CPDLCServer.Auth.Users;
using CPDLCServer.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Serilog.Core;

namespace CPDLCServer.Tests.Auth.Users;

public class ChangePasswordCommandHandlerTests
{
    const string CurrentPassword = "correct horse battery";
    const string NewPassword = "another long password";

    readonly TestUserRepository _repository = new();
    readonly PasswordHasher<User> _passwordHasher = new();
    readonly User _user;

    public ChangePasswordCommandHandlerTests()
    {
        _user = new User { Id = Guid.NewGuid(), Username = "admin" };
        _user.PasswordHash = _passwordHasher.HashPassword(_user, CurrentPassword);
        _repository.Add(_user, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ChangesThePassword()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        await handler.Handle(new ChangePasswordCommand(_user.Id, NewPassword), CancellationToken.None);

        // Assert
        Assert.Equal(PasswordVerificationResult.Success, Verify(NewPassword));
        Assert.Equal(PasswordVerificationResult.Failed, Verify(CurrentPassword));
    }

    [Fact]
    public async Task Handle_WithUnknownUser_Throws()
    {
        // Arrange
        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new ChangePasswordCommand(Guid.NewGuid(), NewPassword),
            CancellationToken.None));
    }

    PasswordVerificationResult Verify(string password)
    {
        return _passwordHasher.VerifyHashedPassword(_user, _user.PasswordHash, password);
    }

    ChangePasswordCommandHandler CreateHandler()
    {
        return new ChangePasswordCommandHandler(_repository, _passwordHasher, Logger.None);
    }
}
