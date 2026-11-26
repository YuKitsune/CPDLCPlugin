using CPDLCServer.Auth.Administrators;
using CPDLCServer.Auth.Login;
using CPDLCServer.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Serilog.Core;

namespace CPDLCServer.Tests.Auth.Login;

public class LoginCommandHandlerTests
{
    const string Password = "correct horse battery";
    const string IpAddress = "203.0.113.10";

    readonly TestAdministratorRepository _repository = new();
    readonly PasswordHasher<Administrator> _passwordHasher = new();
    readonly TestClock _clock = new();
    readonly LoginLockout _loginLockout;
    readonly Administrator _administrator;

    public LoginCommandHandlerTests()
    {
        _loginLockout = new LoginLockout(_clock);
        _administrator = new Administrator { Cid = "1234567", Name = "Test Controller" };
        _administrator.PasswordHash = _passwordHasher.HashPassword(_administrator, Password);
        _repository.Add(_administrator, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_WithCorrectPassword_ReturnsTheAdministrator()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new LoginCommand(" 1234567 ", Password, IpAddress), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1234567", result.Cid);
        Assert.Equal("Test Controller", result.Name);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ReturnsNull()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new LoginCommand("1234567", "wrong password", IpAddress), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WithUnknownCid_ReturnsNull()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new LoginCommand("7654321", Password, IpAddress), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenAdministratorHasNoPassword_ReturnsNull()
    {
        // Arrange - an administrator that was added with VATSIM Connect
        await _repository.Add(new Administrator { Cid = "2222222" }, CancellationToken.None);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new LoginCommand("2222222", string.Empty, IpAddress), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_AfterTooManyFailures_LocksOutTheIpAddress()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        await FailLogins(handler, "1234567", LoginLockout.MaxFailedLogins);

        // Assert
        Assert.True(_loginLockout.IsLockedOut(IpAddress));
    }

    [Fact]
    public async Task Handle_FailuresWithUnknownCids_LockOutTheIpAddress()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        await FailLogins(handler, "7654321", LoginLockout.MaxFailedLogins);

        // Assert
        Assert.True(_loginLockout.IsLockedOut(IpAddress));
    }

    [Fact]
    public async Task Handle_DuringLockout_RejectsTheCorrectPassword()
    {
        // Arrange
        var handler = CreateHandler();
        await FailLogins(handler, "1234567", LoginLockout.MaxFailedLogins);

        // Act
        var result = await handler.Handle(new LoginCommand("1234567", Password, IpAddress), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_DuringLockout_AcceptsTheCorrectPasswordFromAnotherIpAddress()
    {
        // Arrange
        var handler = CreateHandler();
        await FailLogins(handler, "1234567", LoginLockout.MaxFailedLogins);

        // Act
        var result = await handler.Handle(new LoginCommand("1234567", Password, "198.51.100.20"), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_AfterLockout_AcceptsTheCorrectPassword()
    {
        // Arrange
        var handler = CreateHandler();
        await FailLogins(handler, "1234567", LoginLockout.MaxFailedLogins);
        _clock.SetUtcNow(_clock.UtcNow() + LoginLockout.LockoutDuration);

        // Act
        var result = await handler.Handle(new LoginCommand("1234567", Password, IpAddress), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_WithCorrectPassword_ResetsTheFailedLogins()
    {
        // Arrange
        var handler = CreateHandler();
        await FailLogins(handler, "1234567", LoginLockout.MaxFailedLogins - 1);

        // Act
        await handler.Handle(new LoginCommand("1234567", Password, IpAddress), CancellationToken.None);
        await FailLogins(handler, "1234567", 1);

        // Assert
        Assert.False(_loginLockout.IsLockedOut(IpAddress));
    }

    static async Task FailLogins(LoginCommandHandler handler, string cid, int count)
    {
        for (var i = 0; i < count; i++)
            await handler.Handle(new LoginCommand(cid, "wrong password", IpAddress), CancellationToken.None);
    }

    LoginCommandHandler CreateHandler()
    {
        return new LoginCommandHandler(_repository, _passwordHasher, _loginLockout, Logger.None);
    }
}
