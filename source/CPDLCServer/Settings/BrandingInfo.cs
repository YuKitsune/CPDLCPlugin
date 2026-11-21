namespace CPDLCServer.Settings;

// The branding of the server settings without the image, for each page of the dashboard
public record BrandingInfo(string ServerName, string? Motd, bool HasBanner, int BannerVersion);
