namespace CPDLCServer.Model;

// Raw ADS-C data as parsed from an ACARS packet, before being assigned to a contract.
public abstract record ReceivedAdscMessage(string Sender, DateTimeOffset Received);

public record ReceivedAdscReport(
    string Sender,
    AdscReport Report,
    DateTimeOffset Received)
    : ReceivedAdscMessage(Sender, Received);

// The aircraft cannot accept the contract, or the pilot has switched ADS-C off.
public record ReceivedAdscRejection(
    string Sender,
    DateTimeOffset Received)
    : ReceivedAdscMessage(Sender, Received);
