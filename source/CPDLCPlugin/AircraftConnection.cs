using CPDLCServer.Contracts;

namespace CPDLCPlugin;

public class AircraftConnection
{
    public AircraftConnection(AircraftConnectionDto dto)
    {
        Dto = dto;
        NextDataAuthorityInfo = NoneNextDataAuthorityInfo.Instance;
    }

    public AircraftConnectionDto Dto { get; private set; }

    public string Callsign => Dto.Callsign;
    public string StationId => Dto.StationId;
    public DataAuthorityState DataAuthorityState => Dto.DataAuthorityState;

    public INextDataAuthorityInfo NextDataAuthorityInfo { get; private set; }

    public void UpdateDto(AircraftConnectionDto dto)
    {
        Dto = dto;
    }

    public void SetNextDataAuthorityInfo(INextDataAuthorityInfo info)
    {
        NextDataAuthorityInfo = info;
    }
}
