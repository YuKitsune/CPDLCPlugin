namespace CPDLCServer.Logging;

/// <summary>
/// The part of the server that wrote a log entry. The Logs page can show only the entries of one area.
/// </summary>
public static class LogArea
{
    // The names of the log event properties
    public const string PropertyName = "Area";
    public const string AcarsConnectionIdPropertyName = "AcarsConnectionId";

    public const string Cpdlc = "CPDLC";
    public const string Adsc = "ADS-C";
    public const string Acars = "ACARS";
    public const string System = "System";

    public static readonly string[] All = [Cpdlc, Adsc, Acars, System];

    // The area of a request or a notification comes from its namespace
    public static string ForType(Type type)
    {
        var typeNamespace = type.Namespace ?? string.Empty;

        if (IsIn(typeNamespace, "CPDLCServer.Cpdlc"))
            return Cpdlc;

        if (IsIn(typeNamespace, "CPDLCServer.AdscContracts") || IsIn(typeNamespace, "CPDLCServer.VatsimStatus"))
            return Adsc;

        if (IsIn(typeNamespace, "CPDLCServer.AcarsConnections"))
            return Acars;

        return System;
    }

    static bool IsIn(string typeNamespace, string areaNamespace)
    {
        return typeNamespace == areaNamespace || typeNamespace.StartsWith(areaNamespace + ".", StringComparison.Ordinal);
    }
}
