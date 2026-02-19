using vatsys;

namespace CPDLCPlugin;

public static class NextSectorCalculator
{
    public static SectorsVolumes.Sector? GetNextSector(FDP2.FDR fdr)
    {
        var segment = (from s in fdr.ParsedRoute.ToList()
            where s.Type == FDP2.FDR.ExtractedRoute.Segment.SegmentTypes.ZPOINT &&
                  fdr.ControllingSector != SectorsVolumes.FindSector((SectorsVolumes.Volume)s.Tag)
            select s).FirstOrDefault(s => s.ETO > DateTime.UtcNow);

        SectorsVolumes.Volume? volume = null;
        if (segment != null) volume = (SectorsVolumes.Volume)segment.Tag;

        var nextSector = volume != null ? SectorsVolumes.FindSector(volume) : null;

        if (nextSector == null) return nextSector;

        SectorsVolumes.Sector? sector = null;
        foreach (var s2 in SectorsVolumes.SectorGroupings.Keys)
            if (s2.SubSectors.Contains(nextSector) &&
                (sector == null || sector.SubSectors.Count > s2.SubSectors.Count))
                sector = s2;
        if (sector != null) nextSector = sector;

        return nextSector;
    }

    public static DateTime GetBoundaryTime(FDP2.FDR fdr)
    {
        var boundary = (from s in fdr.ParsedRoute.ToList()
            where s.Type == FDP2.FDR.ExtractedRoute.Segment.SegmentTypes.ZPOINT
            select s).FirstOrDefault(s => s.ETO > DateTime.UtcNow);

        return boundary?.ETO ?? DateTime.MaxValue;
    }
}
