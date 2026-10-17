using CPDLCServer.Handlers;
using CPDLCServer.Messages;
using CPDLCServer.Model;
using CPDLCServer.Persistence;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.Handlers;

public class EndExpiredAdscContractsRequestHandlerTests
{
    [Fact]
    public async Task Handle_WhenSelectedContractHasEnded_RequestsDefaultContract()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestSelectedContract(TimeSpan.FromMinutes(2), clock.UtcNow());
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(contractRepository, clock, mediator, Logger.None);

        // Act
        await handler.Handle(new EndExpiredAdscContractsRequest(), CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<RequestDefaultAdscContractCommand>(c => c.StationId == "YBBB" && c.Callsign == "UAL123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSelectedContractHasNotEnded_DoesNothing()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestSelectedContract(TimeSpan.FromMinutes(2), clock.UtcNow().AddSeconds(1));
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(contractRepository, clock, mediator, Logger.None);

        // Act
        await handler.Handle(new EndExpiredAdscContractsRequest(), CancellationToken.None);

        // Assert
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_WhenNoSelectedContract_DoesNothing()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestDefaultContract(TimeSpan.FromMinutes(10));
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);

        var mediator = Substitute.For<IMediator>();
        var handler = new EndExpiredAdscContractsRequestHandler(contractRepository, clock, mediator, Logger.None);

        // Act
        await handler.Handle(new EndExpiredAdscContractsRequest(), CancellationToken.None);

        // Assert
        Assert.Empty(mediator.ReceivedCalls());
    }

    [Fact]
    public async Task Handle_WhenRequestFails_ContinuesWithOtherContracts()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        foreach (var callsign in new[] { "UAL123", "QFA1" })
        {
            var contract = new AdscContract(callsign, "YBBB");
            contract.RequestSelectedContract(TimeSpan.FromMinutes(2), clock.UtcNow());
            await contractRepository.Add(new AdscContractKey(callsign, "YBBB"), contract, CancellationToken.None);
        }

        var mediator = Substitute.For<IMediator>();
        mediator
            .Send(Arg.Is<RequestDefaultAdscContractCommand>(c => c.Callsign == "UAL123"), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new HttpRequestException("Hoppie is down")));

        var handler = new EndExpiredAdscContractsRequestHandler(contractRepository, clock, mediator, Logger.None);

        // Act
        await handler.Handle(new EndExpiredAdscContractsRequest(), CancellationToken.None);

        // Assert
        await mediator.Received(1).Send(
            Arg.Is<RequestDefaultAdscContractCommand>(c => c.Callsign == "QFA1"),
            Arg.Any<CancellationToken>());
    }
}
