using CPDLCServer.Auth.Administrators;
using CPDLCServer.Settings;
using CPDLCServer.Setup;
using CPDLCServer.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;

namespace CPDLCServer.Tests.Setup;

public class CompleteSetupCommandHandlerTests
{
    readonly TestAdministratorRepository _administratorRepository = new();
    readonly TestServerSettingsProvider _settingsProvider = new(new ServerSettings());
    readonly TestClock _clock = new();

    [Fact]
    public async Task Handle_AddsTheAdministratorAndTheOperatorAndCompletesTheSetup()
    {
        // Arrange
        var setupState = CreateSetupState();
        var handler = CreateHandler(setupState);

        // Act
        await handler.Handle(new CompleteSetupCommand(1234567, " VATPAC ", " privacy@example.com "), CancellationToken.None);

        // Assert
        Assert.NotNull(await _administratorRepository.FindByCid(1234567, CancellationToken.None));

        var settings = await _settingsProvider.Load(CancellationToken.None);
        Assert.Equal("VATPAC", settings.OperatorName);
        Assert.Equal("privacy@example.com", settings.PrivacyContact);

        Assert.True(setupState.IsConfigured);
    }

    [Fact]
    public async Task Handle_WhenAnAdministratorExists_Throws()
    {
        // Arrange
        await _administratorRepository.Add(new Administrator { Cid = 1111111 }, CancellationToken.None);
        var handler = CreateHandler(CreateSetupState());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CompleteSetupCommand(1234567, "VATPAC", "privacy@example.com"), CancellationToken.None));
        Assert.Null(await _administratorRepository.FindByCid(1234567, CancellationToken.None));
        Assert.Equal(string.Empty, (await _settingsProvider.Load(CancellationToken.None)).OperatorName);
    }

    CompleteSetupCommandHandler CreateHandler(SetupState setupState) =>
        new(_administratorRepository, _settingsProvider, setupState, _clock, new TestUnitOfWork(), Logger.None);

    SetupState CreateSetupState()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAdministratorRepository>(_administratorRepository);
        return new SetupState(services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());
    }
}
