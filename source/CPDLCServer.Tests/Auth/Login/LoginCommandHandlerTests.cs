using CPDLCServer.Auth.Login;
using CPDLCServer.Auth.Users;
using CPDLCServer.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Serilog.Core;

namespace CPDLCServer.Tests.Auth.Login;

public class LoginCommandHandlerTests
{
    const string Password = "correct horse battery";
    const string IpAddress = "203.0.113.10";

    readonly TestUserRepository _repository = new();
    readonly PasswordHasher<User> _passwordHasher = new();
    readonly TestClock _clock = new();
    readonly LoginLockout _loginLockout;
    readonly User _user;

    public LoginCommandHandlerTests()
    {
        _loginLockout = new LoginLockout(_clock);
        _user = new User { Id = Guid.NewGuid(), Username = "admin" };
        _user.PasswordHash = _passwordHasher.HashPassword(_user, Password);
        _repository.Add(_user, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WithCorrectPassword_ReturnsTheUser()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new LoginCommand(" admin ", Password, IpAddress), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_user.Id, result.UserId);
        Assert.Equal("admin", result.Username);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ReturnsNull()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new LoginCommand("admin", "wrong password", IpAddress), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WithUnknownUser_ReturnsNull()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new LoginCommand("intruder", Password, IpAddress), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_AfterTooManyFailures_LocksOutTheIpAddress()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        await FailLogins(handler, "admin", LoginLockout.MaxFailedLogins);

        // Assert
        Assert.True(_loginLockout.IsLockedOut(IpAddress));
    }

    [Fact]
    public async Task Handle_FailuresWithUnknownUsers_LockOutTheIpAddress()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        await FailLogins(handler, "intruder", LoginLockout.MaxFailedLogins);

        // Assert
        Assert.True(_loginLockout.IsLockedOut(IpAddress));
    }

    [Fact]
    public async Task Handle_DuringLockout_RejectsTheCorrectPassword()
    {
        // Arrange
        var handler = CreateHandler();
        await FailLogins(handler, "admin", LoginLockout.MaxFailedLogins);

        // Act
        var result = await handler.Handle(new LoginCommand("admin", Password, IpAddress), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_DuringLockout_AcceptsTheCorrectPasswordFromAnotherIpAddress()
    {
        // Arrange
        var handler = CreateHandler();
        await FailLogins(handler, "admin", LoginLockout.MaxFailedLogins);

        // Act
        var result = await handler.Handle(new LoginCommand("admin", Password, "198.51.100.20"), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_AfterLockout_AcceptsTheCorrectPassword()
    {
        // Arrange
        var handler = CreateHandler();
        await FailLogins(handler, "admin", LoginLockout.MaxFailedLogins);
        _clock.SetUtcNow(_clock.UtcNow() + LoginLockout.LockoutDuration);

        // Act
        var result = await handler.Handle(new LoginCommand("admin", Password, IpAddress), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_WithCorrectPassword_ResetsTheFailedLogins()
    {
        // Arrange
        var handler = CreateHandler();
        await FailLogins(handler, "admin", LoginLockout.MaxFailedLogins - 1);

        // Act
        await handler.Handle(new LoginCommand("admin", Password, IpAddress), CancellationToken.None);
        await FailLogins(handler, "admin", 1);

        // Assert
        Assert.False(_loginLockout.IsLockedOut(IpAddress));
    }

    static async Task FailLogins(LoginCommandHandler handler, string username, int count)
    {
        for (var i = 0; i < count; i++)
            await handler.Handle(new LoginCommand(username, "wrong password", IpAddress), CancellationToken.None);
    }

    LoginCommandHandler CreateHandler()
    {
        return new LoginCommandHandler(_repository, _passwordHasher, _loginLockout, Logger.None);
    }
}
