namespace CPDLCPlugin;

/// <summary>
/// A point along the route where the aircraft enters a sector.
/// </summary>
/// <param name="SectorIds">The sector and its parent sector groups, from the most-specific to the least-specific.</param>
/// <param name="EntryTime">The estimated time that the aircraft enters the sector.</param>
public record SectorEntry(IReadOnlyList<string> SectorIds, DateTimeOffset EntryTime);
