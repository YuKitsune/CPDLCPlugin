namespace CPDLCServer.AcarsConnections.Hoppie;

public class MessageParseException(string message, Exception? innerException = null)
    : Exception(message, innerException);