using CPDLCServer.Model;

namespace CPDLCServer.Tests.Model;

public class AdscContractTests
{
    static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(10);
    static readonly TimeSpan SelectedInterval = TimeSpan.FromMinutes(2);
    static readonly DateTimeOffset Ends = new(2026, 1, 1, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RequestDefaultContract_SendsDefaultContract()
    {
        // Arrange
        var contract = new AdscContract("UAL123", "YBBB");

        // Act
        contract.RequestDefaultContract(DefaultInterval);

        // Assert
        Assert.Equal(DefaultInterval, contract.ReportingInterval);
        Assert.Equal(AdscContractState.Sent, contract.DefaultContractState);
        Assert.True(contract.HasPeriodicContract);
        Assert.True(contract.IsActive);
    }

    [Fact]
    public void RequestDefaultContract_WhenSelectedContractIsAccepted_EndsSelectedContract()
    {
        // Arrange
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestSelectedContract(SelectedInterval, Ends);
        contract.ReportReceived();

        // Act
        contract.RequestDefaultContract(DefaultInterval);

        // Assert
        Assert.Equal(DefaultInterval, contract.ReportingInterval);
        Assert.Equal(AdscContractState.Sent, contract.DefaultContractState);
        Assert.Null(contract.SelectedReportingInterval);
        Assert.Null(contract.SelectedContractState);
        Assert.Null(contract.SelectedContractEnds);
    }

    [Fact]
    public void RequestDefaultContract_WhenSelectedContractIsRejected_KeepsRejection()
    {
        // Arrange
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestSelectedContract(SelectedInterval, Ends);
        contract.Reject();

        // Act
        contract.RequestDefaultContract(DefaultInterval);

        // Assert
        Assert.Equal(AdscContractState.Sent, contract.DefaultContractState);
        Assert.Equal(SelectedInterval, contract.SelectedReportingInterval);
        Assert.Equal(AdscContractState.Rejected, contract.SelectedContractState);
    }

    [Fact]
    public void RequestSelectedContract_SendsSelectedContractAndKeepsDefaultContract()
    {
        // Arrange
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestDefaultContract(DefaultInterval);
        contract.ReportReceived();

        // Act
        contract.RequestSelectedContract(SelectedInterval, Ends);

        // Assert
        Assert.Equal(SelectedInterval, contract.ReportingInterval);
        Assert.Equal(SelectedInterval, contract.SelectedReportingInterval);
        Assert.Equal(AdscContractState.Sent, contract.SelectedContractState);
        Assert.Equal(Ends, contract.SelectedContractEnds);
        Assert.Equal(AdscContractState.Accepted, contract.DefaultContractState);
    }

    [Fact]
    public void ReportReceived_WhenDefaultContractIsSent_AcceptsDefaultContract()
    {
        // Arrange
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestDefaultContract(DefaultInterval);

        // Act
        var changed = contract.ReportReceived();

        // Assert
        Assert.True(changed);
        Assert.Equal(AdscContractState.Accepted, contract.DefaultContractState);
    }

    [Fact]
    public void ReportReceived_WhenSelectedContractIsSent_AcceptsSelectedContractAndEndsDefaultContract()
    {
        // Arrange
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestDefaultContract(DefaultInterval);
        contract.RequestSelectedContract(SelectedInterval, Ends);

        // Act
        var changed = contract.ReportReceived();

        // Assert
        Assert.True(changed);
        Assert.Equal(AdscContractState.Accepted, contract.SelectedContractState);
        Assert.Null(contract.DefaultContractState);
    }

    [Fact]
    public void ReportReceived_WhenDemandReportIsSent_AcceptsDemandReport()
    {
        // Arrange
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestDemandReport();

        // Act
        var changed = contract.ReportReceived();

        // Assert
        Assert.True(changed);
        Assert.Equal(AdscContractState.Accepted, contract.DemandContractState);
        Assert.False(contract.HasPeriodicContract);
        Assert.True(contract.IsActive);
    }

    [Fact]
    public void ReportReceived_WhenNothingIsSent_ReturnsFalse()
    {
        // Arrange
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestDefaultContract(DefaultInterval);
        contract.ReportReceived();

        // Act
        var changed = contract.ReportReceived();

        // Assert
        Assert.False(changed);
        Assert.Equal(AdscContractState.Accepted, contract.DefaultContractState);
    }

    [Fact]
    public void Reject_WhenSelectedContractIsSent_RejectsSelectedContract()
    {
        // Arrange
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestDefaultContract(DefaultInterval);
        contract.ReportReceived();
        contract.RequestSelectedContract(SelectedInterval, Ends);

        // Act
        var selectedContractRejected = contract.Reject();

        // Assert
        Assert.True(selectedContractRejected);
        Assert.Equal(AdscContractState.Rejected, contract.SelectedContractState);
        Assert.Null(contract.SelectedContractEnds);
        Assert.Equal(AdscContractState.Accepted, contract.DefaultContractState);
    }

    [Fact]
    public void Reject_WhenSelectedContractIsAccepted_RejectsSelectedContract()
    {
        // Arrange
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestSelectedContract(SelectedInterval, Ends);
        contract.ReportReceived();

        // Act
        var selectedContractRejected = contract.Reject();

        // Assert
        Assert.True(selectedContractRejected);
        Assert.Equal(AdscContractState.Rejected, contract.SelectedContractState);
        Assert.False(contract.HasPeriodicContract);
    }

    [Fact]
    public void Reject_WhenDefaultContractIsSent_RejectsDefaultContract()
    {
        // Arrange
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestDefaultContract(DefaultInterval);

        // Act
        var selectedContractRejected = contract.Reject();

        // Assert
        Assert.False(selectedContractRejected);
        Assert.Equal(AdscContractState.Rejected, contract.DefaultContractState);
        Assert.False(contract.IsActive);
    }

    [Fact]
    public void Reject_WhenDemandReportIsSent_RejectsDemandReport()
    {
        // Arrange
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestDefaultContract(DefaultInterval);
        contract.ReportReceived();
        contract.RequestDemandReport();

        // Act
        contract.Reject();

        // Assert
        Assert.Equal(AdscContractState.Rejected, contract.DefaultContractState);
        Assert.Equal(AdscContractState.Rejected, contract.DemandContractState);
    }

    [Fact]
    public void Reject_WhenDemandReportIsAccepted_KeepsDemandReport()
    {
        // Arrange
        var contract = new AdscContract("UAL123", "YBBB");
        contract.RequestDemandReport();
        contract.ReportReceived();
        contract.RequestDefaultContract(DefaultInterval);

        // Act
        contract.Reject();

        // Assert
        Assert.Equal(AdscContractState.Rejected, contract.DefaultContractState);
        Assert.Equal(AdscContractState.Accepted, contract.DemandContractState);
    }
}
