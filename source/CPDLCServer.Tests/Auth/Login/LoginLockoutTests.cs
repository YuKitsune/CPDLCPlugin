using CPDLCServer.Auth.Login;
using CPDLCServer.Tests.Mocks;

namespace CPDLCServer.Tests.Auth.Login;

public class LoginLockoutTests
{
    const string IpAddress = "203.0.113.10";

    readonly TestClock _clock = new();
    readonly LoginLockout _loginLockout;

    public LoginLockoutTests()
    {
        _loginLockout = new LoginLockout(_clock);
    }

    [Fact]
    public void RecordFailure_BelowTheLimit_DoesNotLockOut()
    {
        // Act
        for (var i = 0; i < LoginLockout.MaxFailedLogins - 1; i++)
            Assert.Null(_loginLockout.RecordFailure(IpAddress));

        // Assert
        Assert.False(_loginLockout.IsLockedOut(IpAddress));
    }

    [Fact]
    public void RecordFailure_AtTheLimit_LocksOutForTheLockoutDuration()
    {
        // Act
        DateTimeOffset? lockoutEnd = null;
        for (var i = 0; i < LoginLockout.MaxFailedLogins; i++)
            lockoutEnd = _loginLockout.RecordFailure(IpAddress);

        // Assert
        Assert.Equal(_clock.UtcNow() + LoginLockout.LockoutDuration, lockoutEnd);
        Assert.True(_loginLockout.IsLockedOut(IpAddress));
    }

    [Fact]
    public void IsLockedOut_AfterTheLockoutDuration_IsFalse()
    {
        // Arrange
        LockOut();

        // Act
        _clock.SetUtcNow(_clock.UtcNow() + LoginLockout.LockoutDuration);

        // Assert
        Assert.False(_loginLockout.IsLockedOut(IpAddress));
    }

    [Fact]
    public void RecordFailure_AfterTheFailuresExpire_StartsTheCountAgain()
    {
        // Arrange
        for (var i = 0; i < LoginLockout.MaxFailedLogins - 1; i++)
            _loginLockout.RecordFailure(IpAddress);
        _clock.SetUtcNow(_clock.UtcNow() + LoginLockout.LockoutDuration);

        // Act
        var lockoutEnd = _loginLockout.RecordFailure(IpAddress);

        // Assert
        Assert.Null(lockoutEnd);
        Assert.False(_loginLockout.IsLockedOut(IpAddress));
    }

    [Fact]
    public void Reset_EndsTheLockout()
    {
        // Arrange
        LockOut();

        // Act
        _loginLockout.Reset(IpAddress);

        // Assert
        Assert.False(_loginLockout.IsLockedOut(IpAddress));
    }

    [Fact]
    public void LockOut_DoesNotAffectOtherIpAddresses()
    {
        // Act
        LockOut();

        // Assert
        Assert.False(_loginLockout.IsLockedOut("198.51.100.20"));
    }

    void LockOut()
    {
        for (var i = 0; i < LoginLockout.MaxFailedLogins; i++)
            _loginLockout.RecordFailure(IpAddress);
    }
}
