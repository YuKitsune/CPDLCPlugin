using System.Text.Json;

namespace CPDLCServer.Auth.VatsimConnect;

// The user from the /api/user endpoint of VATSIM Connect
public record VatsimUser(string Cid)
{
    // The response is { "data": { "cid": "1234567", ... } }.
    // The CID can be a string or a number.
    public static VatsimUser Parse(JsonElement root)
    {
        var data = root.GetProperty("data");

        var cidElement = data.GetProperty("cid");
        var cid = cidElement.ValueKind == JsonValueKind.Number
            ? cidElement.GetRawText()
            : cidElement.GetString() ?? string.Empty;

        if (!VatsimCid.IsValid(cid))
            throw new InvalidOperationException($"VATSIM Connect gave a CID that is not valid: {cid}");

        return new VatsimUser(cid);
    }
}
