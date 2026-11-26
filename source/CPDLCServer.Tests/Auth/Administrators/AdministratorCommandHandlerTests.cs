using CPDLCServer.Auth.Administrators;
using CPDLCServer.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Serilog.Core;

namespace CPDLCServer.Tests.Auth.Administrators;

public class AdministratorCommandHandlerTests
{
    readonly TestAdministratorRepository _administratorRepository = new();
    readonly TestClock _clock = new();
    readonly PasswordHasher<Administrator> _passwordHasher = new();

    [Fact]
    public async Task Add_AddsTheAdministrator()
    {
        // Arrange
        var handler = new AddAdministratorCommandHandler(_administratorRepository, _passwordHasher, _clock, new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new AddAdministratorCommand("1234567", null), CancellationToken.None);

        // Assert
        var administrator = await _administratorRepository.FindByCid("1234567", CancellationToken.None);
        Assert.NotNull(administrator);
        Assert.Null(administrator.PasswordHash);
        Assert.Equal(_clock.UtcNow(), administrator.Added);
    }

    [Fact]
    public async Task Add_WithPassword_KeepsAHashOfThePassword()
    {
        // Arrange
        var handler = new AddAdministratorCommandHandler(_administratorRepository, _passwordHasher, _clock, new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new AddAdministratorCommand("1234567", "correct horse battery"), CancellationToken.None);

        // Assert
        var administrator = await _administratorRepository.FindByCid("1234567", CancellationToken.None);
        Assert.NotNull(administrator);
        Assert.NotNull(administrator.PasswordHash);
        Assert.NotEqual("correct horse battery", administrator.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            _passwordHasher.VerifyHashedPassword(administrator, administrator.PasswordHash, "correct horse battery"));
    }

    [Fact]
    public async Task Add_WhenAlreadyAdministrator_Throws()
    {
        // Arrange
        await _administratorRepository.Add(new Administrator { Cid = "1234567" }, CancellationToken.None);
        var handler = new AddAdministratorCommandHandler(_administratorRepository, _passwordHasher, _clock, new TestUnitOfWork(), Logger.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new AddAdministratorCommand("1234567", null), CancellationToken.None));
    }

    [Fact]
    public async Task Remove_RemovesTheAdministrator()
    {
        // Arrange
        await _administratorRepository.Add(new Administrator { Cid = "1111111" }, CancellationToken.None);
        await _administratorRepository.Add(new Administrator { Cid = "2222222" }, CancellationToken.None);
        var handler = new RemoveAdministratorCommandHandler(_administratorRepository, new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new RemoveAdministratorCommand("2222222", CurrentUserCid: "1111111"), CancellationToken.None);

        // Assert
        Assert.Null(await _administratorRepository.FindByCid("2222222", CancellationToken.None));
    }

    [Fact]
    public async Task Remove_Yourself_Throws()
    {
        // Arrange
        await _administratorRepository.Add(new Administrator { Cid = "1111111" }, CancellationToken.None);
        await _administratorRepository.Add(new Administrator { Cid = "2222222" }, CancellationToken.None);
        var handler = new RemoveAdministratorCommandHandler(_administratorRepository, new TestUnitOfWork(), Logger.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RemoveAdministratorCommand("1111111", CurrentUserCid: "1111111"), CancellationToken.None));
        Assert.NotNull(await _administratorRepository.FindByCid("1111111", CancellationToken.None));
    }

    [Fact]
    public async Task Remove_LastAdministrator_Throws()
    {
        // Arrange
        await _administratorRepository.Add(new Administrator { Cid = "1111111" }, CancellationToken.None);
        var handler = new RemoveAdministratorCommandHandler(_administratorRepository, new TestUnitOfWork(), Logger.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RemoveAdministratorCommand("1111111", CurrentUserCid: "9999999"), CancellationToken.None));
    }

    [Fact]
    public async Task ChangePassword_ChangesThePassword()
    {
        // Arrange
        await _administratorRepository.Add(new Administrator { Cid = "1234567" }, CancellationToken.None);
        var handler = new ChangePasswordCommandHandler(_administratorRepository, _passwordHasher, Logger.None);

        // Act
        await handler.Handle(new ChangePasswordCommand("1234567", "new password 123"), CancellationToken.None);

        // Assert
        var administrator = await _administratorRepository.FindByCid("1234567", CancellationToken.None);
        Assert.Equal(
            PasswordVerificationResult.Success,
            _passwordHasher.VerifyHashedPassword(administrator!, administrator!.PasswordHash!, "new password 123"));
    }

    [Fact]
    public async Task ChangePassword_WhenNotAdministrator_Throws()
    {
        // Arrange
        var handler = new ChangePasswordCommandHandler(_administratorRepository, _passwordHasher, Logger.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ChangePasswordCommand("1234567", "new password 123"), CancellationToken.None));
    }
}
