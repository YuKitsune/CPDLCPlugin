using CPDLCServer.Auth.PluginTokens;
using CPDLCServer.Tests.Mocks;
using Serilog.Core;

namespace CPDLCServer.Tests.Auth.PluginTokens;

public class PluginTokenCommandHandlerTests
{
    readonly TestPluginTokenRepository _repository = new();
    readonly TestClock _clock = new();

    [Fact]
    public async Task Delete_DeletesOnlyTheSelectedTokens()
    {
        // Arrange - one CID can have many tokens
        var first = await AddToken("1234567");
        var second = await AddToken("1234567");
        var other = await AddToken("7654321");
        var handler = new DeletePluginTokensCommandHandler(_repository, new TestUnitOfWork(), Logger.None);

        // Act
        var count = await handler.Handle(new DeletePluginTokensCommand([first.Id, second.Id], "1111111"), CancellationToken.None);

        // Assert
        Assert.Equal(2, count);
        Assert.Null(await _repository.FindById(first.Id, CancellationToken.None));
        Assert.Null(await _repository.FindById(second.Id, CancellationToken.None));
        Assert.NotNull(await _repository.FindById(other.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_IgnoresUnknownIds()
    {
        // Arrange
        var token = await AddToken("1234567");
        var handler = new DeletePluginTokensCommandHandler(_repository, new TestUnitOfWork(), Logger.None);

        // Act
        var count = await handler.Handle(new DeletePluginTokensCommand([Guid.NewGuid(), token.Id], "1111111"), CancellationToken.None);

        // Assert
        Assert.Equal(1, count);
        Assert.Null(await _repository.FindById(token.Id, CancellationToken.None));
    }

    [Fact]
    public async Task RecordUse_SetsTheLastUsedTime()
    {
        // Arrange
        var token = await AddToken("1234567");
        var handler = new RecordPluginTokenUseCommandHandler(_repository, _clock);

        // Act
        await handler.Handle(new RecordPluginTokenUseCommand(token.Id), CancellationToken.None);

        // Assert
        Assert.Equal(_clock.UtcNow(), token.LastUsed);
    }

    [Fact]
    public async Task RecordUse_WhenTokenWasDeleted_DoesNothing()
    {
        // Arrange
        var handler = new RecordPluginTokenUseCommandHandler(_repository, _clock);

        // Act & Assert
        await handler.Handle(new RecordPluginTokenUseCommand(Guid.NewGuid()), CancellationToken.None);
    }

    async Task<PluginToken> AddToken(string cid)
    {
        var token = new PluginToken
        {
            Id = Guid.NewGuid(),
            Cid = cid,
            TokenHash = PluginTokenHash.Compute(PluginTokenHash.NewToken()),
            Created = _clock.UtcNow(),
            Expires = _clock.UtcNow() + PluginToken.Lifetime
        };
        await _repository.Add(token, CancellationToken.None);
        return token;
    }
}
