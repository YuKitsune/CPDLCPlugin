namespace CPDLCServer.AcarsConnections;

public sealed class ConfigurationNotFoundException(string acarsConnectionId)
    : Exception($"No ACARS configuration found for ACARS connection ID '{acarsConnectionId}'");