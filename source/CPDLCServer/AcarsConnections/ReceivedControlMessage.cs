namespace CPDLCServer.AcarsConnections;

// A message that controls the connection to an aircraft. It is not a CPDLC message, so it does not go in a dialogue.
public abstract record ReceivedControlMessage(string Callsign, DateTimeOffset Received);

public record ReceivedLogonRequest(string Callsign, DateTimeOffset Received) : ReceivedControlMessage(Callsign, Received);

public record ReceivedLogoff(string Callsign, DateTimeOffset Received) : ReceivedControlMessage(Callsign, Received);
