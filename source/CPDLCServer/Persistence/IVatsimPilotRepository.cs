using CPDLCServer.Model;

namespace CPDLCServer.Persistence;

// Holds the pilots from the latest download of the VATSIM status data
public interface IVatsimPilotRepository
{
    // Replaces all the pilots
    Task Replace(VatsimPilot[] pilots, CancellationToken cancellationToken);
    Task<VatsimPilot?> Find(string callsign, CancellationToken cancellationToken);
}
