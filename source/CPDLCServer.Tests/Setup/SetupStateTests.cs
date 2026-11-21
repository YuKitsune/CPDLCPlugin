using CPDLCServer.Auth.Administrators;
using CPDLCServer.Setup;
using CPDLCServer.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;

namespace CPDLCServer.Tests.Setup;

public class SetupStateTests
{
    readonly TestAdministratorRepository _administratorRepository = new();

    [Fact]
    public async Task Load_WhenNoAdministrator_IsNotConfigured()
    {
        // Arrange
        var setupState = CreateSetupState();

        // Act
        await setupState.Load(CancellationToken.None);

        // Assert
        Assert.False(setupState.IsConfigured);
    }

    [Fact]
    public async Task Load_WhenAdministratorExists_IsConfigured()
    {
        // Arrange
        await _administratorRepository.Add(new Administrator { Cid = "1234567" }, CancellationToken.None);
        var setupState = CreateSetupState();

        // Act
        await setupState.Load(CancellationToken.None);

        // Assert
        Assert.True(setupState.IsConfigured);
    }

    [Fact]
    public async Task MarkConfigured_CompletesTheSetup()
    {
        // Arrange
        var setupState = CreateSetupState();
        await setupState.Load(CancellationToken.None);

        // Act
        setupState.MarkConfigured();

        // Assert
        Assert.True(setupState.IsConfigured);
    }

    SetupState CreateSetupState()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAdministratorRepository>(_administratorRepository);
        var serviceProvider = services.BuildServiceProvider();

        return new SetupState(serviceProvider.GetRequiredService<IServiceScopeFactory>());
    }
}
