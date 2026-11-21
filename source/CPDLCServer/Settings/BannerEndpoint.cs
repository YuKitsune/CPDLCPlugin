using Microsoft.Net.Http.Headers;

namespace CPDLCServer.Settings;

// Sends the banner image. The URL contains the banner version, so the browser can keep the image in its cache.
public static class BannerEndpoint
{
    public static async Task<IResult> Handle(HttpContext context, IServerSettingsProvider settingsProvider, CancellationToken cancellationToken)
    {
        var banner = await settingsProvider.LoadBanner(cancellationToken);
        if (banner is null)
            return Results.NotFound();

        context.Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
        context.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";

        return Results.File(banner.Value.Image, banner.Value.ContentType);
    }
}
