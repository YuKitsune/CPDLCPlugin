namespace CPDLCServer.Auth;

// A VATSIM CID. The server keeps it as text, and accepts only digits.
public static class VatsimCid
{
    public const int MaxLength = 16;

    // For the RegularExpression attribute on the input fields
    public const string Pattern = "^[0-9]+$";

    public const string ErrorMessage = "Enter a CID that contains only digits.";

    public static bool IsValid(string? value) =>
        value is { Length: > 0 and <= MaxLength } && value.All(char.IsAsciiDigit);
}
