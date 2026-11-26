namespace CPDLC.Messages;

/// <summary>
/// An uplink message element on the wire.
/// Id is the FANS 1/A UM number. Parameters has one value for each parameter of the element, in sequence.
/// </summary>
public record UplinkMessageElement(int Id, IReadOnlyList<string> Parameters);

/// <summary>
/// A downlink message element on the wire.
/// Id is the FANS 1/A DM number. Parameters has one value for each parameter of the element, in sequence.
/// </summary>
public record DownlinkMessageElement(int Id, IReadOnlyList<string> Parameters);
