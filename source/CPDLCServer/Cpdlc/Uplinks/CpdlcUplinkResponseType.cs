using CPDLC.Messages;

namespace CPDLCServer.Cpdlc.Uplinks;

public enum CpdlcUplinkResponseType
{
    NoResponse,
    WilcoUnable,
    AffirmativeNegative,
    Roger
}

public static class CpdlcUplinkResponseTypeMapping
{
    public static CpdlcUplinkResponseType From(UplinkResponseAttribute responseAttribute)
    {
        return responseAttribute switch
        {
            UplinkResponseAttribute.NoResponse => CpdlcUplinkResponseType.NoResponse,
            UplinkResponseAttribute.WilcoUnable => CpdlcUplinkResponseType.WilcoUnable,
            UplinkResponseAttribute.AffirmativeNegative => CpdlcUplinkResponseType.AffirmativeNegative,
            UplinkResponseAttribute.Roger => CpdlcUplinkResponseType.Roger,
            _ => throw new ArgumentOutOfRangeException(nameof(responseAttribute), responseAttribute, null)
        };
    }
}
