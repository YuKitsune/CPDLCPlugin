using System.Text.Json;

namespace CPDLCServer.Auth.VatsimConnect;

// The user from the /api/user endpoint of VATSIM Connect
public record VatsimUser(int Cid, string Name)
{
    // The response is { "data": { "cid": "1234567", "personal": { "name_full": "..." } } }.
    // The CID can be a string or a number.
    public static VatsimUser Parse(JsonElement root)
    {
        var data = root.GetProperty("data");

        var cidElement = data.GetProperty("cid");
        var cid = cidElement.ValueKind == JsonValueKind.Number
            ? cidElement.GetInt32()
            : int.Parse(cidElement.GetString()!);

        // The name is empty if the user did not give the full_name scope
        var name = data.TryGetProperty("personal", out var personal) &&
                   personal.TryGetProperty("name_full", out var nameFull)
            ? nameFull.GetString() ?? string.Empty
            : string.Empty;

        return new VatsimUser(cid, string.IsNullOrWhiteSpace(name) ? cid.ToString() : name);
    }
}
