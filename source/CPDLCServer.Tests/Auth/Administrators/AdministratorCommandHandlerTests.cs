using CPDLCServer.Auth.Administrators;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Auth.Administrators;

public class AdministratorCommandHandlerTests
{
    readonly TestAdministratorRepository _administratorRepository = new();
    readonly TestClock _clock = new();

    [Fact]
    public async Task Add_AddsTheAdministrator()
    {
        // Arrange
        var handler = new AddAdministratorCommandHandler(_administratorRepository, _clock, new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new AddAdministratorCommand(1234567), CancellationToken.None);

        // Assert
        var administrator = await _administratorRepository.FindByCid(1234567, CancellationToken.None);
        Assert.NotNull(administrator);
        Assert.Null(administrator.Name);
        Assert.Equal(_clock.UtcNow(), administrator.Added);
    }

    [Fact]
    public async Task Add_WhenAlreadyAdministrator_Throws()
    {
        // Arrange
        await _administratorRepository.Add(new Administrator { Cid = 1234567 }, CancellationToken.None);
        var handler = new AddAdministratorCommandHandler(_administratorRepository, _clock, new TestUnitOfWork(), Logger.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new AddAdministratorCommand(1234567), CancellationToken.None));
    }

    [Fact]
    public async Task Remove_RemovesTheAdministrator()
    {
        // Arrange
        await _administratorRepository.Add(new Administrator { Cid = 1111111 }, CancellationToken.None);
        await _administratorRepository.Add(new Administrator { Cid = 2222222 }, CancellationToken.None);
        var handler = new RemoveAdministratorCommandHandler(_administratorRepository, new TestUnitOfWork(), Logger.None);

        // Act
        await handler.Handle(new RemoveAdministratorCommand(2222222, CurrentUserCid: 1111111), CancellationToken.None);

        // Assert
        Assert.Null(await _administratorRepository.FindByCid(2222222, CancellationToken.None));
    }

    [Fact]
    public async Task Remove_Yourself_Throws()
    {
        // Arrange
        await _administratorRepository.Add(new Administrator { Cid = 1111111 }, CancellationToken.None);
        await _administratorRepository.Add(new Administrator { Cid = 2222222 }, CancellationToken.None);
        var handler = new RemoveAdministratorCommandHandler(_administratorRepository, new TestUnitOfWork(), Logger.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RemoveAdministratorCommand(1111111, CurrentUserCid: 1111111), CancellationToken.None));
        Assert.NotNull(await _administratorRepository.FindByCid(1111111, CancellationToken.None));
    }

    [Fact]
    public async Task Remove_LastAdministrator_Throws()
    {
        // Arrange
        await _administratorRepository.Add(new Administrator { Cid = 1111111 }, CancellationToken.None);
        var handler = new RemoveAdministratorCommandHandler(_administratorRepository, new TestUnitOfWork(), Logger.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RemoveAdministratorCommand(1111111, CurrentUserCid: 9999999), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateName_ChangesTheNameOfAnAdministrator()
    {
        // Arrange
        await _administratorRepository.Add(new Administrator { Cid = 1234567 }, CancellationToken.None);
        var handler = new UpdateAdministratorNameCommandHandler(_administratorRepository);

        // Act
        await handler.Handle(new UpdateAdministratorNameCommand(1234567, "Test Controller"), CancellationToken.None);

        // Assert
        var administrator = await _administratorRepository.FindByCid(1234567, CancellationToken.None);
        Assert.Equal("Test Controller", administrator!.Name);
    }
}
