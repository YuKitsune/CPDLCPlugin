namespace CPDLCServer.VatsimStatus;

public interface IVatsimPilotRepository
{
    Task Replace(VatsimPilot[] pilots, CancellationToken cancellationToken);
    Task<VatsimPilot?> Find(string callsign, CancellationToken cancellationToken);
}
