namespace CPDLCServer.Settings;

// Finds the type of an image from its first bytes. The type from the browser is not used, because the browser can send any type.
// SVG is not accepted, because an SVG file can contain scripts.
public static class BannerImageFormat
{
    public const int MaxSize = 1024 * 1024;

    static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    static readonly byte[] Riff = "RIFF"u8.ToArray();
    static readonly byte[] Webp = "WEBP"u8.ToArray();

    // Returns null when the image is not PNG, JPEG or WebP
    public static string? FindContentType(ReadOnlySpan<byte> image)
    {
        if (image.StartsWith(Png))
            return "image/png";

        if (image.StartsWith(Jpeg))
            return "image/jpeg";

        if (image.Length >= 12 && image[..4].SequenceEqual(Riff) && image[8..12].SequenceEqual(Webp))
            return "image/webp";

        return null;
    }
}
