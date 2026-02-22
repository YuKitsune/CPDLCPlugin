namespace CPDLCPlugin;

public interface INextDataAuthorityInfo
{
}

public sealed class NoneNextDataAuthorityInfo : INextDataAuthorityInfo
{
    public static readonly NoneNextDataAuthorityInfo Instance = new();
    private NoneNextDataAuthorityInfo() { }
}

public sealed record ErrorNextDataAuthorityInfo(string ErrorMessage) : INextDataAuthorityInfo;

public sealed record ValidNextDataAuthorityInfo(
    string NextDataAuthority,
    DateTimeOffset ExitTime) : INextDataAuthorityInfo;
