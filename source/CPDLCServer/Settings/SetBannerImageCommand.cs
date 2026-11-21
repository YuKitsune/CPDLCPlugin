using MediatR;

namespace CPDLCServer.Settings;

// A null image removes the banner
public record SetBannerImageCommand(byte[]? Image) : IRequest;
