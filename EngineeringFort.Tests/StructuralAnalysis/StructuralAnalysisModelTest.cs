using EngineeringFort.StructuralAnalysis;

namespace EngineeringFort.Tests.StructuralAnalysis;

public class StructuralAnalysisModelTest
{
    static StructuralPointConnection Connection(string label, double x, double y, double z, bool isSupport = false) => new()
    {
        Label = label,
        X = Length.FromMillimeters(x),
        Y = Length.FromMillimeters(y),
        Z = Length.FromMillimeters(z),
        IsSupport = isSupport,
    };

    static StructuralCurveMember Member(string label, string start, string end) => new() { Label = label, Start = start, End = end };

    /// <summary>Two legs and a beam across their tops.</summary>
    static StructuralAnalysisModel Portal => new()
    {
        Connections = [Connection("A", 0, 0, 0, true), Connection("B", 0, 0, 1000), Connection("C", 2000, 0, 1000), Connection("D", 2000, 0, 0, true)],
        Members = [Member("M1", "A", "B"), Member("M2", "B", "C"), Member("M3", "C", "D")],
    };

    [Fact]
    public void Portal_ShouldBeSound()
    {
        Assert.Empty(Portal.Validate());
        Assert.Empty(Portal.Warnings());
        Assert.Equal(1, Portal.PartCount());
    }

    [Fact]
    public void MemberToNoConnection_ShouldBeAProblem() =>
        Assert.Contains((Portal with { Members = [.. Portal.Members, Member("M4", "A", "Z")] }).Validate(), p => p.Contains("M4"));

    [Fact]
    public void MembersOverSameConnections_ShouldBeAProblem() =>
        Assert.Contains((Portal with { Members = [.. Portal.Members, Member("M4", "B", "A")] }).Validate(),
            p => p.Contains("M1") && p.Contains("M4"));

    [Fact]
    public void ConnectionsWithinTolerance_ShouldBeAProblem() =>
        Assert.Contains((Portal with { Connections = [.. Portal.Connections, Connection("E", 0.5, 0, 0)] }).Validate(),
            p => p.Contains("one point"));

    [Fact]
    public void ConnectionWithNoMember_ShouldBeAProblem() =>
        Assert.Contains((Portal with { Connections = [.. Portal.Connections, Connection("E", 5000, 0, 0)] }).Validate(),
            p => p.Contains("E has no member"));

    [Fact]
    public void PartWithNoSupport_ShouldBeAProblem()
    {
        var model = Portal with
        {
            Connections = [.. Portal.Connections, Connection("E", 5000, 0, 0), Connection("F", 5000, 0, 1000)],
            Members = [.. Portal.Members, Member("M4", "E", "F")],
        };

        Assert.Contains(model.Validate(), p => p.Contains("no support"));
        Assert.Equal(2, model.PartCount());
    }

    [Fact]
    public void MembersCrossingWithNoConnection_ShouldBeAWarning()
    {
        var model = Portal with
        {
            Connections = [.. Portal.Connections, Connection("E", 1000, -500, 1000, true), Connection("F", 1000, 500, 1000)],
            Members = [.. Portal.Members, Member("M4", "E", "F")],
        };

        Assert.Empty(model.Validate());
        Assert.Contains(model.Warnings(), w => w.Contains("M2") && w.Contains("M4"));
    }

    [Fact]
    public void VeryShortMember_ShouldBeAWarning()
    {
        var model = Portal with
        {
            Connections = [.. Portal.Connections, Connection("E", 2000, 0, 1025)],
            Members = [.. Portal.Members, Member("M4", "C", "E")],
        };

        Assert.Contains(model.Warnings(), w => w.Contains("M4"));
    }

    [Fact]
    public void UnknownLevel_ShouldBeUnknownRatherThanAProblem()
    {
        var model = Portal with
        {
            Connections = [.. Portal.Connections.Select(c => c.Label == "C" ? c with { Z = null } : c)],
        };

        Assert.Empty(model.Validate());
        Assert.Contains(model.Unknowns(), u => u.Contains("Connection C: z"));
    }

    [Fact]
    public void MembersWithoutProfile_ShouldBeUnknown() =>
        Assert.Contains(Portal.Unknowns(), u => u.Contains("3 members: profile"));
}
