using CPDLCServer.Model;

namespace CPDLCServer.Tests.Model;

public class VatsimPilotTests
{
    [Theory]
    [InlineData("B77W/H-SDE1E3FGHIJ2J3J5M1RWXY/LB1D1", true)]
    [InlineData("A359/H-SDE1E2E3FGHIJ2J3J4J5M1RWXYZ/SB1D1G1", true)]
    [InlineData("A388/H-SDE2E3FGHIJ3J5M1RWXY/LB1G1", true)]
    [InlineData("B738/M-SDE2E3FGHIRWY/LB1", false)]
    [InlineData("B738/M-SD1E2E3FGHIRWY/LB1", false)] // D1 in Item 10a is not ADS-C
    [InlineData("B738/M-SDFGRWY", false)]
    [InlineData("B738", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void HasAdscEquipment_ChecksItem10b(string? aircraft, bool expected)
    {
        Assert.Equal(expected, VatsimPilot.HasAdscEquipment(aircraft));
    }
}
