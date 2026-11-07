using System.Net;
using CPDLCServer.Auth.Login;
using Microsoft.AspNetCore.Http;

namespace CPDLCServer.Tests.Auth.Login;

public class ClientIpAddressTests
{
    [Fact]
    public void Get_WithoutHeaders_ReturnsTheConnectionAddress()
    {
        var httpContext = CreateHttpContext();

        Assert.Equal("10.0.0.1", ClientIpAddress.Get(httpContext));
    }

    [Fact]
    public void Get_WithDigitalOceanHeader_ReturnsTheHeaderAddress()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["do-connecting-ip"] = "203.0.113.10";
        httpContext.Request.Headers["x-forwarded-for"] = "10.0.0.2";

        Assert.Equal("203.0.113.10", ClientIpAddress.Get(httpContext));
    }

    [Fact]
    public void Get_WithForwardedForList_ReturnsTheFirstAddress()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["x-forwarded-for"] = "203.0.113.10, 10.0.0.2";

        Assert.Equal("203.0.113.10", ClientIpAddress.Get(httpContext));
    }

    [Fact]
    public void Get_WithInvalidHeaderValue_UsesTheNextHeader()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["do-connecting-ip"] = "not an address";
        httpContext.Request.Headers["x-real-ip"] = "2001:db8::1";

        Assert.Equal("2001:db8::1", ClientIpAddress.Get(httpContext));
    }

    static DefaultHttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        return httpContext;
    }
}
