using static CPDLC.Messages.ParameterType;

namespace CPDLC.Messages;

/// <summary>
/// The FANS 1/A downlink message elements (DM0 to DM80), as RTCA DO-258A defines them.
/// </summary>
public static class DownlinkElements
{
    public static readonly IReadOnlyList<DownlinkElementDefinition> All =
    [
        new(0, "WILCO", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium),
        new(1, "UNABLE", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium),
        new(2, "STANDBY", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium),
        new(3, "ROGER", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium),
        new(4, "AFFIRM", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium),
        new(5, "NEGATIVE", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium),
        new(6, "REQUEST [altitude]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Altitude),
        new(7, "REQUEST BLOCK [altitude] TO [altitude]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Altitude, Altitude),
        new(8, "REQUEST CRUISE CLIMB TO [altitude]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Altitude),
        new(9, "REQUEST CLIMB TO [altitude]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Altitude),
        new(10, "REQUEST DESCENT TO [altitude]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Altitude),
        new(11, "AT [position] REQUEST CLIMB TO [altitude]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Position, Altitude),
        new(12, "AT [position] REQUEST DESCENT TO [altitude]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Position, Altitude),
        new(13, "AT [time] REQUEST CLIMB TO [altitude]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Time, Altitude),
        new(14, "AT [time] REQUEST DESCENT TO [altitude]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Time, Altitude),
        new(15, "REQUEST OFFSET [distance offset] [direction] OF ROUTE", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, DistanceOffset, Direction),
        new(16, "AT [position] REQUEST OFFSET [distance offset] [direction] OF ROUTE", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Position, DistanceOffset, Direction),
        new(17, "AT [time] REQUEST OFFSET [distance offset] [direction] OF ROUTE", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Time, DistanceOffset, Direction),
        new(18, "REQUEST [speed]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Speed),
        new(19, "REQUEST [speed] TO [speed]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Speed, Speed),
        new(20, "REQUEST VOICE CONTACT", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium),
        new(21, "REQUEST VOICE CONTACT [frequency]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Frequency),
        new(22, "REQUEST DIRECT TO [position]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Position),
        new(23, "REQUEST [procedure name]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, ProcedureName),
        new(24, "REQUEST [route clearance]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, RouteClearance),
        new(25, "REQUEST CLEARANCE", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium),
        new(26, "REQUEST WEATHER DEVIATION TO [position] VIA [route clearance]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Position, RouteClearance),
        new(27, "REQUEST WEATHER DEVIATION UP TO [distance offset] [direction] OF ROUTE", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, DistanceOffset, Direction),
        new(28, "LEAVING [altitude]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Altitude),
        new(29, "CLIMBING TO [altitude]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Altitude),
        new(30, "DESCENDING TO [altitude]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Altitude),
        new(31, "PASSING [position]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Position),
        new(32, "PRESENT ALTITUDE [altitude]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Altitude),
        new(33, "PRESENT POSITION [position]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Position),
        new(34, "PRESENT SPEED [speed]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Speed),
        new(35, "PRESENT HEADING [degrees]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Degrees),
        new(36, "PRESENT GROUND TRACK [degrees]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Degrees),
        new(37, "LEVEL [altitude]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Altitude),
        new(38, "ASSIGNED ALTITUDE [altitude]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Altitude),
        new(39, "ASSIGNED SPEED [speed]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Speed),
        new(40, "ASSIGNED ROUTE [route clearance]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, RouteClearance),
        new(41, "BACK ON ROUTE", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium),
        new(42, "NEXT WAYPOINT [position]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Position),
        new(43, "NEXT WAYPOINT ETA [time]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Time),
        new(44, "ENSUING WAYPOINT [position]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Position),
        new(45, "REPORTED WAYPOINT [position]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Position),
        new(46, "REPORTED WAYPOINT [time]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Time),
        new(47, "SQUAWKING [beacon code]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, BeaconCode),
        new(48, "POSITION REPORT [position report]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, PositionReport),
        new(49, "WHEN CAN WE EXPECT [speed]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Speed),
        new(50, "WHEN CAN WE EXPECT [speed] TO [speed]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Speed, Speed),
        new(51, "WHEN CAN WE EXPECT BACK ON ROUTE", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium),
        new(52, "WHEN CAN WE EXPECT LOWER ALTITUDE", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium),
        new(53, "WHEN CAN WE EXPECT HIGHER ALTITUDE", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium),
        new(54, "WHEN CAN WE EXPECT CRUISE CLIMB TO [altitude]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Altitude),
        new(55, "PAN PAN PAN", DownlinkResponseAttribute.NoResponse, AlertAttribute.High),
        new(56, "MAYDAY MAYDAY MAYDAY", DownlinkResponseAttribute.NoResponse, AlertAttribute.High),
        new(57, "[remaining fuel] OF FUEL REMAINING AND [remaining souls] SOULS ON BOARD", DownlinkResponseAttribute.NoResponse, AlertAttribute.High, RemainingFuel, RemainingSouls),
        new(58, "CANCEL EMERGENCY", DownlinkResponseAttribute.NoResponse, AlertAttribute.High),
        new(59, "DIVERTING TO [position] VIA [route clearance]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Position, RouteClearance),
        new(60, "OFFSETTING [distance offset] [direction] OF ROUTE", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, DistanceOffset, Direction),
        new(61, "DESCENDING TO [altitude]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Altitude),
        new(62, "ERROR [error information]", DownlinkResponseAttribute.NoResponse, AlertAttribute.None, ErrorInformation),
        new(63, "NOT CURRENT DATA AUTHORITY", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium),
        new(64, "[icao facility designation]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, IcaoFacilityDesignation),
        new(65, "DUE TO WEATHER", DownlinkResponseAttribute.NoResponse, AlertAttribute.None),
        new(66, "DUE TO AIRCRAFT PERFORMANCE", DownlinkResponseAttribute.NoResponse, AlertAttribute.None),
        new(67, "[free text]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, FreeText),
        new(68, "[free text]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, FreeText),
        new(69, "REQUEST VMC DESCENT", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium),
        new(70, "REQUEST HEADING [degrees]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Degrees),
        new(71, "REQUEST GROUND TRACK [degrees]", DownlinkResponseAttribute.ResponseRequired, AlertAttribute.Medium, Degrees),
        new(72, "REACHING [altitude]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Altitude),
        new(73, "[version number]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, VersionNumber),
        new(74, "MAINTAIN OWN SEPARATION AND VMC", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium),
        new(75, "AT PILOTS DISCRETION", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium),
        new(76, "REACHING BLOCK [altitude] TO [altitude]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Altitude, Altitude),
        new(77, "ASSIGNED BLOCK [altitude] TO [altitude]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Altitude, Altitude),
        new(78, "AT [time] [distance] [to/from] [position]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, Time, Distance, ToFrom, Position),
        new(79, "ATIS [atis code]", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, AtisCode),
        new(80, "DEVIATING [distance offset] [direction] OF ROUTE", DownlinkResponseAttribute.NoResponse, AlertAttribute.Medium, DistanceOffset, Direction),
    ];

    static readonly Dictionary<int, DownlinkElementDefinition> ById = All.ToDictionary(e => e.Id);

    public static DownlinkElementDefinition? Find(int id)
    {
        return ById.TryGetValue(id, out var definition) ? definition : null;
    }

    public static DownlinkElementDefinition Get(int id)
    {
        if (!ById.TryGetValue(id, out var definition))
        {
            throw new KeyNotFoundException($"DM{id} is not a FANS 1/A downlink message element.");
        }

        return definition;
    }
}
