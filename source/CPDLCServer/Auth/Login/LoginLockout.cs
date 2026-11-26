using CPDLCServer.Infrastructure;

namespace CPDLCServer.Auth.Login;

// Locks the login for an IP address after too many failed logins.
// The failed logins are kept only in memory, so a restart clears them.
public class LoginLockout(IClock clock)
{
    public const int MaxFailedLogins = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    readonly Lock _lock = new();
    readonly Dictionary<string, FailedLogins> _failedLogins = new();

    public bool IsLockedOut(string ipAddress)
    {
        lock (_lock)
        {
            var now = clock.UtcNow();
            RemoveExpired(now);

            return _failedLogins.TryGetValue(ipAddress, out var failedLogins) && failedLogins.LockoutEnd > now;
        }
    }

    // Returns the end of the lockout when this failure locks the IP address
    public DateTimeOffset? RecordFailure(string ipAddress)
    {
        lock (_lock)
        {
            var now = clock.UtcNow();
            RemoveExpired(now);

            if (!_failedLogins.TryGetValue(ipAddress, out var failedLogins))
            {
                failedLogins = new FailedLogins();
                _failedLogins[ipAddress] = failedLogins;
            }

            failedLogins.Count++;
            failedLogins.LastFailure = now;

            if (failedLogins.Count < MaxFailedLogins)
                return null;

            failedLogins.Count = 0;
            failedLogins.LockoutEnd = now + LockoutDuration;
            return failedLogins.LockoutEnd;
        }
    }

    public void Reset(string ipAddress)
    {
        lock (_lock)
        {
            _failedLogins.Remove(ipAddress);
        }
    }

    // The failed logins of an IP address expire after the lockout duration without a new failure.
    // This also ends a lockout, because a lockout starts at a failure.
    void RemoveExpired(DateTimeOffset now)
    {
        var expired = _failedLogins
            .Where(pair => pair.Value.LastFailure + LockoutDuration <= now)
            .Select(pair => pair.Key)
            .ToList();

        foreach (var ipAddress in expired)
            _failedLogins.Remove(ipAddress);
    }

    class FailedLogins
    {
        public int Count { get; set; }
        public DateTimeOffset LastFailure { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
    }
}
