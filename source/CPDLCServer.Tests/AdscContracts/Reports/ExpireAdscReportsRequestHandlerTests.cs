using CPDLCServer.AdscContracts;
using CPDLCServer.AdscContracts.Reports;
using CPDLCServer.Tests.Mocks;
using MediatR;
using NSubstitute;
using Serilog.Core;

namespace CPDLCServer.Tests.AdscContracts.Reports;

public class ExpireAdscReportsRequestHandlerTests
{
    static readonly TimeSpan ReportingInterval = TimeSpan.FromMinutes(10);

    // Two reporting intervals and the delivery tolerance
    static readonly TimeSpan ReportExpiry = TimeSpan.FromMinutes(23);

    [Fact]
    public async Task Handle_WhenLastReportIsOlderThanReportExpiry_RemovesReportAndPublishesNotification()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(ReportingInterval);

        var reportRepository = new TestAdscReportRepository();
        await ReceiveReport(reportRepository, contract, clock.UtcNow() - ReportExpiry - TimeSpan.FromSeconds(1));

        var mediator = Substitute.For<IMediator>();
        var handler = new ExpireAdscReportsRequestHandler(reportRepository, contractRepository, clock, mediator, Logger.None);

        // Act
        await handler.Handle(new ExpireAdscReportsRequest(), CancellationToken.None);

        // Assert
        Assert.Empty(await reportRepository.All(CancellationToken.None));
        await mediator.Received(1).Publish(
            Arg.Is<AdscReportExpiredNotification>(n => n.Callsign == "UAL123" && n.StationId == "YBBB"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLastReportIsOlderThanReportExpiry_TimesOutContractReport()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(ReportingInterval);

        var reportRepository = new TestAdscReportRepository();
        var report = await ReceiveReport(reportRepository, contract, clock.UtcNow() - ReportExpiry - TimeSpan.FromSeconds(1));

        var handler = new ExpireAdscReportsRequestHandler(reportRepository, contractRepository, clock, Substitute.For<IMediator>(), Logger.None);

        // Act
        await handler.Handle(new ExpireAdscReportsRequest(), CancellationToken.None);

        // Assert
        Assert.Null(contract.ReportingSource);
        Assert.Equal(report.Report.Timestamp, contract.LastReportReceived);
    }

    [Fact]
    public async Task Handle_WhenLastReportIsNotOlderThanReportExpiry_KeepsReport()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(ReportingInterval);

        var reportRepository = new TestAdscReportRepository();
        var report = await ReceiveReport(reportRepository, contract, clock.UtcNow() - ReportExpiry);

        var mediator = Substitute.For<IMediator>();
        var handler = new ExpireAdscReportsRequestHandler(reportRepository, contractRepository, clock, mediator, Logger.None);

        // Act
        await handler.Handle(new ExpireAdscReportsRequest(), CancellationToken.None);

        // Assert
        Assert.Equal([report], await reportRepository.All(CancellationToken.None));
        await mediator.DidNotReceive().Publish(Arg.Any<AdscReportExpiredNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSelectedContractHasShorterInterval_UsesSelectedInterval()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(ReportingInterval);
        contract.RequestSelectedContract(TimeSpan.FromMinutes(2));

        // Two selected intervals and the delivery tolerance is 7 minutes
        var reportRepository = new TestAdscReportRepository();
        await ReceiveReport(reportRepository, contract, clock.UtcNow() - TimeSpan.FromMinutes(7) - TimeSpan.FromSeconds(1));

        var handler = new ExpireAdscReportsRequestHandler(reportRepository, contractRepository, clock, Substitute.For<IMediator>(), Logger.None);

        // Act
        await handler.Handle(new ExpireAdscReportsRequest(), CancellationToken.None);

        // Assert
        Assert.Empty(await reportRepository.All(CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenContractHasOnlyDemandRequest_UsesDefaultInterval()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDemandReport();

        var reportRepository = new TestAdscReportRepository();
        var report = await ReceiveReport(reportRepository, contract, clock.UtcNow() - ReportExpiry);

        var handler = new ExpireAdscReportsRequestHandler(reportRepository, contractRepository, clock, Substitute.For<IMediator>(), Logger.None);

        // Act
        await handler.Handle(new ExpireAdscReportsRequest(), CancellationToken.None);

        // Assert
        Assert.Equal([report], await reportRepository.All(CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenContractHasNoLastReport_RemovesReport()
    {
        // Arrange
        var clock = new TestClock();
        var contractRepository = new TestAdscContractRepository();
        var contract = await AddContract(contractRepository);
        contract.RequestDefaultContract(ReportingInterval);

        var reportRepository = new TestAdscReportRepository();
        await AddReport(reportRepository, clock.UtcNow());

        var handler = new ExpireAdscReportsRequestHandler(reportRepository, contractRepository, clock, Substitute.For<IMediator>(), Logger.None);

        // Act
        await handler.Handle(new ExpireAdscReportsRequest(), CancellationToken.None);

        // Assert
        Assert.Empty(await reportRepository.All(CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenReportHasNoContract_RemovesReportAndPublishesNotification()
    {
        // Arrange
        var clock = new TestClock();
        var reportRepository = new TestAdscReportRepository();
        await AddReport(reportRepository, clock.UtcNow());

        var mediator = Substitute.For<IMediator>();
        var handler = new ExpireAdscReportsRequestHandler(
            reportRepository, new TestAdscContractRepository(), clock, mediator, Logger.None);

        // Act
        await handler.Handle(new ExpireAdscReportsRequest(), CancellationToken.None);

        // Assert
        Assert.Empty(await reportRepository.All(CancellationToken.None));
        await mediator.Received(1).Publish(
            Arg.Is<AdscReportExpiredNotification>(n => n.Callsign == "UAL123" && n.StationId == "YBBB"),
            Arg.Any<CancellationToken>());
    }

    static async Task<AdscContract> AddContract(TestAdscContractRepository contractRepository)
    {
        var contract = new AdscContract("UAL123", "YBBB", ReportingInterval, DateTimeOffset.MinValue);
        await contractRepository.Add(new AdscContractKey("UAL123", "YBBB"), contract, CancellationToken.None);
        return contract;
    }

    // Adds the report to the repository and gives it to the contract, as the server does when a report arrives
    static async Task<AircraftAdscReport> ReceiveReport(
        TestAdscReportRepository reportRepository,
        AdscContract contract,
        DateTimeOffset timestamp)
    {
        var report = await AddReport(reportRepository, timestamp);
        contract.ReportReceived(report.Report);
        return report;
    }

    static async Task<AircraftAdscReport> AddReport(TestAdscReportRepository reportRepository, DateTimeOffset timestamp)
    {
        var report = new AircraftAdscReport(
            "UAL123",
            "YBBB",
            new AdscReport(timestamp, -33.9461, 151.1772, 37000, AdscReportSource.Hoppie),
            timestamp);
        await reportRepository.Add(report, CancellationToken.None);
        return report;
    }
}
