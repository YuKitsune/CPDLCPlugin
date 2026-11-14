using CPDLCServer.Auth.Users;
using CPDLCServer.Setup;
using CPDLCServer.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;

namespace CPDLCServer.Tests.Setup;

public class SetupStateTests
{
    readonly TestUserRepository _userRepository = new();

    [Fact]
    public async Task Load_WhenNoUser_IsNotConfigured()
    {
        // Arrange
        var setupState = CreateSetupState();

        // Act
        await setupState.Load(CancellationToken.None);

        // Assert
        Assert.False(setupState.IsConfigured);
    }

    [Fact]
    public async Task Load_WhenUserExists_IsConfigured()
    {
        // Arrange
        await _userRepository.Add(new User { Id = Guid.NewGuid(), Username = "admin" }, CancellationToken.None);
        var setupState = CreateSetupState();

        // Act
        await setupState.Load(CancellationToken.None);

        // Assert
        Assert.True(setupState.IsConfigured);
    }

    [Fact]
    public async Task MarkAdminCreated_CompletesTheSetup()
    {
        // Arrange
        var setupState = CreateSetupState();
        await setupState.Load(CancellationToken.None);

        // Act
        setupState.MarkAdminCreated();

        // Assert
        Assert.True(setupState.IsConfigured);
    }

    [Fact]
    public void IsValidSetupToken_AcceptsOnlyTheSetupToken()
    {
        // Arrange
        var setupState = CreateSetupState();

        // Act & Assert
        Assert.True(setupState.IsValidSetupToken(setupState.SetupToken));
        Assert.False(setupState.IsValidSetupToken("wrong-token"));
        Assert.False(setupState.IsValidSetupToken(null));
        Assert.False(setupState.IsValidSetupToken(string.Empty));
    }

    SetupState CreateSetupState()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUserRepository>(_userRepository);
        var serviceProvider = services.BuildServiceProvider();

        return new SetupState(serviceProvider.GetRequiredService<IServiceScopeFactory>());
    }
}
