namespace CPDLCServer.Infrastructure;

// The time that the server started.
// The server does not update the saved data while it is stopped, so a timeout applies only when the server has been
// online for longer than the timeout.
public class ServerStartTime(DateTimeOffset started)
{
    public DateTimeOffset Started { get; } = started;
}
