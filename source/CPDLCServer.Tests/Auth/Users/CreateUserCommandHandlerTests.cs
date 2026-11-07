using CPDLCServer.Auth.Users;
using CPDLCServer.Setup;
using CPDLCServer.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;

namespace CPDLCServer.Tests.Auth.Users;

public class CreateUserCommandHandlerTests
{
    readonly TestUserRepository _repository = new();
    readonly PasswordHasher<User> _passwordHasher = new();

    [Fact]
    public async Task Handle_CreatesAdminWithHashedPassword()
    {
        // Arrange
        var handler = CreateHandler(CreateSetupState());

        // Act
        var id = await handler.Handle(new CreateUserCommand(" admin ", "correct horse battery"), CancellationToken.None);

        // Assert
        var user = await _repository.FindByUsername("admin", CancellationToken.None);
        Assert.NotNull(user);
        Assert.Equal(id, user.Id);
        Assert.NotEqual("correct horse battery", user.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, "correct horse battery"));
    }

    [Fact]
    public async Task Handle_MarksAdminCreated()
    {
        // Arrange
        var setupState = CreateSetupState();
        var handler = CreateHandler(setupState);

        // Act
        await handler.Handle(new CreateUserCommand("admin", "correct horse battery"), CancellationToken.None);

        // Assert
        Assert.True(setupState.IsConfigured);
    }

    [Fact]
    public async Task Handle_WhenAdminExists_Throws()
    {
        // Arrange
        var handler = CreateHandler(CreateSetupState());
        await handler.Handle(new CreateUserCommand("admin", "correct horse battery"), CancellationToken.None);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateUserCommand("intruder", "another password"), CancellationToken.None));
        Assert.Null(await _repository.FindByUsername("intruder", CancellationToken.None));
    }

    CreateUserCommandHandler CreateHandler(SetupState setupState)
    {
        return new CreateUserCommandHandler(
            _repository,
            _passwordHasher,
            setupState,
            new TestClock(),
            new TestUnitOfWork(),
            Logger.None);
    }

    SetupState CreateSetupState()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUserRepository>(_repository);
        var serviceProvider = services.BuildServiceProvider();

        return new SetupState(serviceProvider.GetRequiredService<IServiceScopeFactory>());
    }
}
