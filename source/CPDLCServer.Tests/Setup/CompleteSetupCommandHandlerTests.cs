using CPDLCServer.Auth.Administrators;
using CPDLCServer.Settings;
using CPDLCServer.Setup;
using CPDLCServer.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;

namespace CPDLCServer.Tests.Setup;

public class CompleteSetupCommandHandlerTests
{
    readonly TestAdministratorRepository _administratorRepository = new();
    readonly TestServerSettingsProvider _settingsProvider = new(new ServerSettings());
    readonly TestClock _clock = new();
    readonly PasswordHasher<Administrator> _passwordHasher = new();

    [Fact]
    public async Task Handle_AddsTheAdministratorAndTheOperatorAndCompletesTheSetup()
    {
        // Arrange
        var setupState = CreateSetupState();
        var handler = CreateHandler(setupState);

        // Act
        await handler.Handle(new CompleteSetupCommand("1234567", null, " VATPAC ", " privacy@example.com "), CancellationToken.None);

        // Assert
        var administrator = await _administratorRepository.FindByCid("1234567", CancellationToken.None);
        Assert.NotNull(administrator);
        Assert.Null(administrator.PasswordHash);

        var settings = await _settingsProvider.Load(CancellationToken.None);
        Assert.Equal("VATPAC", settings.OperatorName);
        Assert.Equal("privacy@example.com", settings.PrivacyContact);

        Assert.True(setupState.IsConfigured);
    }

    [Fact]
    public async Task Handle_WhenAnAdministratorExists_Throws()
    {
        // Arrange
        await _administratorRepository.Add(new Administrator { Cid = "1111111" }, CancellationToken.None);
        var handler = CreateHandler(CreateSetupState());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CompleteSetupCommand("1234567", null, "VATPAC", "privacy@example.com"), CancellationToken.None));
        Assert.Null(await _administratorRepository.FindByCid("1234567", CancellationToken.None));
        Assert.Equal(string.Empty, (await _settingsProvider.Load(CancellationToken.None)).OperatorName);
    }

    [Fact]
    public async Task Handle_WithPassword_KeepsAHashOfThePassword()
    {
        // Arrange
        var handler = CreateHandler(CreateSetupState());

        // Act
        await handler.Handle(
            new CompleteSetupCommand("1234567", "correct horse battery", "VATPAC", "privacy@example.com"),
            CancellationToken.None);

        // Assert
        var administrator = await _administratorRepository.FindByCid("1234567", CancellationToken.None);
        Assert.NotNull(administrator);
        Assert.Equal(
            PasswordVerificationResult.Success,
            _passwordHasher.VerifyHashedPassword(administrator, administrator.PasswordHash!, "correct horse battery"));
    }

    CompleteSetupCommandHandler CreateHandler(SetupState setupState) =>
        new(_administratorRepository, _passwordHasher, _settingsProvider, setupState, _clock, new TestUnitOfWork(), Logger.None);

    SetupState CreateSetupState()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAdministratorRepository>(_administratorRepository);
        return new SetupState(services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());
    }
}
