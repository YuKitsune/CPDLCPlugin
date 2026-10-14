using CPDLCServer.Contracts;
using vatsys;

namespace CPDLCPlugin;

/// <summary>
/// Normal position extrapolation for ADS-C tracks.
/// Moves the reported position along the flight plan route, from the report time to now.
/// The speed on each route leg comes from the route estimates, as for flight plan tracks.
/// </summary>
public static class AdscExtrapolation
{
    public static Coordinate Extrapolate(
        AdscReportDto report,
        IReadOnlyList<FDP2.FDR.ExtractedRoute.Segment> route,
        DateTimeOffset now)
    {
        var position = new Coordinate(report.Latitude, report.Longitude);
        if (route.Count < 2)
            return position;

        var remainingHours = Math.Max(0, (now - report.Timestamp).TotalHours);
        for (var waypointIndex = FindNextWaypointIndex(route, position); waypointIndex < route.Count; waypointIndex++)
        {
            var speed = EstimatedSpeed(route, waypointIndex);
            if (speed is null or <= 0)
                return position;

            var waypoint = route[waypointIndex].Intersection.LatLong;
            var hoursToWaypoint = Conversions.CalculateDistance(position, waypoint) / speed.Value;
            if (remainingHours < hoursToWaypoint)
                return Conversions.CalculateLLFromFractionOfRoute(position, waypoint, remainingHours / hoursToWaypoint);

            remainingHours -= hoursToWaypoint;
            position = waypoint;
        }

        return position;
    }

    // The next waypoint is the end of the route leg that is nearest to the position.
    // The nearest leg is the leg with the smallest detour when the aircraft flies from the start of the leg, through the position, to the end of the leg.
    static int FindNextWaypointIndex(IReadOnlyList<FDP2.FDR.ExtractedRoute.Segment> route, Coordinate position)
    {
        var nextWaypointIndex = 1;
        var smallestDetour = double.MaxValue;
        for (var i = 1; i < route.Count; i++)
        {
            var legStart = route[i - 1].Intersection.LatLong;
            var legEnd = route[i].Intersection.LatLong;
            var detour = Conversions.CalculateDistance(legStart, position)
                + Conversions.CalculateDistance(position, legEnd)
                - Conversions.CalculateDistance(legStart, legEnd);

            if (detour < smallestDetour)
            {
                smallestDetour = detour;
                nextWaypointIndex = i;
            }
        }

        return nextWaypointIndex;
    }

    // The speed in knots on the route leg that ends at the waypoint, from the route estimates.
    static double? EstimatedSpeed(IReadOnlyList<FDP2.FDR.ExtractedRoute.Segment> route, int waypointIndex)
    {
        var legStart = route[waypointIndex - 1];
        var legEnd = route[waypointIndex];
        if (legStart.ETO == DateTime.MaxValue || legEnd.ETO == DateTime.MaxValue)
            return null;

        var legTime = legEnd.ETO - legStart.ETO;
        if (legTime <= TimeSpan.Zero)
            return null;

        return Conversions.CalculateDistance(legStart.Intersection.LatLong, legEnd.Intersection.LatLong) / legTime.TotalHours;
    }
}
