using vatsys;

namespace CPDLCPlugin.Extensions;

public static class FdrExtensionMethods
{
    public static IReadOnlyList<SectorEntry> GetSectorEntries(this FDP2.FDR fdr)
    {
        var results = new List<SectorEntry>();

        foreach (var segment in fdr.ParsedRoute.Skip(fdr.ParsedRoute.OverflownIndex))
        {
            if (segment.Type != FDP2.FDR.ExtractedRoute.Segment.SegmentTypes.ZPOINT)
                continue;

            var volume = (SectorsVolumes.Volume?)segment.Tag;
            if (volume is null)
                continue;

            var sector = SectorsVolumes.FindSector(volume);
            if (sector is null)
                continue;

            // Walk the sector hierarchy from most-specific to least-specific.
            // A sector can be in its own sub-sectors, so stop at a sector that was visited before.
            var sectorIds = new List<string>();
            var visited = new HashSet<SectorsVolumes.Sector>();
            var current = sector;
            while (current is not null && visited.Add(current))
            {
                sectorIds.Add(current.Name);
                var captured = current;
                current = SectorsVolumes.SectorGroupings.Keys
                    .Where(p => p.SubSectors.Contains(captured))
                    .OrderBy(p => p.SubSectors.Count)
                    .FirstOrDefault();
            }

            var eto = segment.ETO;
            var etoDateTimeOffset = new DateTimeOffset(
                eto.Year,
                eto.Month,
                eto.Day,
                eto.Hour,
                eto.Minute,
                eto.Second,
                offset: TimeSpan.Zero);

            results.Add(new SectorEntry(sectorIds.AsReadOnly(), etoDateTimeOffset));
        }

        return results.AsReadOnly();
    }
}
