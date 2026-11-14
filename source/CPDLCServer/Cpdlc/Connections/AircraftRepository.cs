using CPDLCServer.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CPDLCServer.Cpdlc.Connections;

// The changes are saved when the unit of work is committed.
// Queries load the rows into the context, then read the local entities, so that the results include entities that are
// added in the unit of work and exclude entities that are removed in it.
public class AircraftRepository(CpdlcDbContext dbContext) : IAircraftRepository
{
    public async Task Add(AircraftKey aircraftKey, AircraftConnection connection, CancellationToken cancellationToken)
    {
        // A new connection replaces the connection with the same key
        var existing = await Find(aircraftKey, cancellationToken);
        if (existing is not null)
            dbContext.AircraftConnections.Remove(existing);

        dbContext.AircraftConnections.Add(connection);
    }

    public async Task<AircraftConnection?> Find(AircraftKey aircraftKey, CancellationToken cancellationToken)
    {
        await dbContext.AircraftConnections
            .Where(c => c.Callsign == aircraftKey.Callsign && c.AcarsConnectionId == aircraftKey.AcarsConnectionId)
            .LoadAsync(cancellationToken);

        return dbContext.AircraftConnections.Local
            .FirstOrDefault(c => c.Callsign == aircraftKey.Callsign && c.AcarsConnectionId == aircraftKey.AcarsConnectionId);
    }

    public async Task<AircraftConnection[]> All(CancellationToken cancellationToken)
    {
        await dbContext.AircraftConnections.LoadAsync(cancellationToken);
        return dbContext.AircraftConnections.Local.ToArray();
    }

    public async Task<bool> Remove(AircraftKey aircraftKey, CancellationToken cancellationToken)
    {
        var connection = await Find(aircraftKey, cancellationToken);
        if (connection is null)
            return false;

        dbContext.AircraftConnections.Remove(connection);
        return true;
    }
}
