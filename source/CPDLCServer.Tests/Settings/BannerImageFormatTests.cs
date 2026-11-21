using CPDLCServer.Settings;

namespace CPDLCServer.Tests.Settings;

public class BannerImageFormatTests
{
    [Fact]
    public void FindContentType_FindsPngJpegAndWebp()
    {
        // Arrange
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00];
        byte[] webp = [.."RIFF"u8, 0x00, 0x00, 0x00, 0x00, .."WEBP"u8, 0x00];

        // Act & Assert
        Assert.Equal("image/png", BannerImageFormat.FindContentType(png));
        Assert.Equal("image/jpeg", BannerImageFormat.FindContentType(jpeg));
        Assert.Equal("image/webp", BannerImageFormat.FindContentType(webp));
    }

    [Fact]
    public void FindContentType_DoesNotAcceptSvg()
    {
        // Arrange
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"u8.ToArray();

        // Act & Assert
        Assert.Null(BannerImageFormat.FindContentType(svg));
    }
}
