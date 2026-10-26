using LoomEditor.Core.Markers;

namespace LoomEditor.Core.Tests.Markers;

public class MarkerServiceTests
{
    private const int Seed = 1234;

    private static MarkerData RandomData(MarkerSeverity severity = MarkerSeverity.Error, Random? random = null)
    {
        random ??= new Random(Seed);
        var line = random.Next(1, 50);
        var column = random.Next(1, 50);
        return new MarkerData(severity, random.Next().ToString("x", System.Globalization.CultureInfo.InvariantCulture), line, column, line, column + random.Next(0, 10));
    }

    // Monaco: 'query'
    [Fact]
    public void Query()
    {
        var service = new MarkerService();
        service.Change("far", [RandomData(MarkerSeverity.Error)]);

        Assert.Single(service.Read());
        Assert.Single(service.Read("far"));

        service.Change("boo", [RandomData(MarkerSeverity.Warning)]);

        Assert.Equal(2, service.Read().Count);
        Assert.Single(service.Read("far"));
        Assert.Single(service.Read("boo"));

        Assert.Single(service.Read(severities: (int)MarkerSeverity.Error));
        Assert.Single(service.Read(severities: (int)MarkerSeverity.Warning));
        Assert.Empty(service.Read(severities: (int)MarkerSeverity.Hint));
        Assert.Equal(2, service.Read(severities: (int)MarkerSeverity.Error | (int)MarkerSeverity.Warning).Count);
    }

    // Monaco: 'changeOne override'
    [Fact]
    public void ChangeOne_Override()
    {
        var service = new MarkerService();
        service.Change("far", [RandomData()]);
        Assert.Single(service.Read());
        Assert.Single(service.Read("far"));

        service.Change("boo", [RandomData()]);
        Assert.Equal(2, service.Read().Count);
        Assert.Single(service.Read("far"));
        Assert.Single(service.Read("boo"));

        service.Change("far", [RandomData(), RandomData()]);
        Assert.Equal(2, service.Read("far").Count);
        Assert.Single(service.Read("boo"));
    }

    // Monaco: 'changeOne/All clears'
    [Fact]
    public void ChangeOne_AllClears()
    {
        var service = new MarkerService();
        service.Change("far", [RandomData()]);
        service.Change("boo", [RandomData()]);
        Assert.Single(service.Read("far"));
        Assert.Single(service.Read("boo"));
        Assert.Equal(2, service.Read().Count);

        service.Change("far", []);
        Assert.Empty(service.Read("far"));
        Assert.Single(service.Read("boo"));
        Assert.Single(service.Read());

        service.Change("boo", []);
        Assert.Empty(service.Read("far"));
        Assert.Empty(service.Read("boo"));
        Assert.Empty(service.Read());
    }

    // Monaco: 'invalid marker data'
    [Fact]
    public void InvalidMarkerData()
    {
        var data = RandomData();
        var service = new MarkerService();

        service.Change("far", [data with { Message = null! }]);
        Assert.Empty(service.Read("far"));

        service.Change("far", [data with { Message = "" }]);
        Assert.Empty(service.Read("far"));

        service.Change("far", [data with { Message = "null" }]);
        Assert.Single(service.Read("far"));
    }

    // Monaco: 'Error code of zero in markers get removed, #31275'
    [Fact]
    public void ErrorCodeOfZero_IsKept()
    {
        var data = new MarkerData((MarkerSeverity)0, "test", 1, 2, 1, 5, Code: "0", Source: "me");
        var service = new MarkerService();

        service.Change("far", [data]);
        var marker = Assert.Single(service.Read());

        Assert.Equal("0", marker.Code);
    }

    // Monaco: 'modelVersionId is preserved on IMarker when present in IMarkerData'
    [Fact]
    public void ModelVersionId_IsPreserved()
    {
        var service = new MarkerService();

        service.Change("owner", [RandomData() with { ModelVersionId = 42 }]);
        Assert.Equal(42, Assert.Single(service.Read()).ModelVersionId);

        service.Change("owner", [RandomData()]);
        Assert.Null(Assert.Single(service.Read()).ModelVersionId);
    }

    [Fact]
    public void Change_SanitizesPositions()
    {
        var service = new MarkerService();
        service.Change("o", [new MarkerData(MarkerSeverity.Error, "m", 0, 0, -1, 0)]);

        var marker = Assert.Single(service.Read());
        Assert.Equal((1, 1, 1, 1), (marker.StartLineNumber, marker.StartColumn, marker.EndLineNumber, marker.EndColumn));
    }

    [Fact]
    public void Read_ReturnsTheSameInstancesUntilTheOwnerChanges()
    {
        var service = new MarkerService();
        service.Change("a", [RandomData()]);
        service.Change("b", [RandomData()]);
        var before = service.Read();

        service.Change("a", [RandomData()]);
        var after = service.Read();

        Assert.NotSame(before[0], after[0]);
        Assert.Same(before[1], after[1]);
        Assert.Same(service.Read("b")[0], service.Read("b")[0]);
    }

    [Fact]
    public void Change_Empty_RaisesOnlyWhenOwnerHadMarkers()
    {
        var service = new MarkerService();
        var count = 0;
        service.MarkersChanged += (_, _) => count++;

        service.Change("a", []);
        service.Change("a", null);
        Assert.Equal(0, count);

        service.Change("a", [RandomData()]);
        service.Change("a", []);
        Assert.Equal(2, count);
    }

    [Fact]
    public void Change_RaisesEvenWhenEqual()
    {
        var service = new MarkerService();
        var count = 0;
        service.MarkersChanged += (_, _) => count++;

        service.Change("a", [RandomData()]);
        service.Change("a", [RandomData()]);

        Assert.Equal(2, count);
    }

    [Fact]
    public void Read_Take_Limits()
    {
        var service = new MarkerService();
        service.Change("a", [RandomData(), RandomData()]);
        service.Change("b", [RandomData()]);

        Assert.Equal(2, service.Read(take: 2).Count);
        Assert.Equal(3, service.Read(take: 0).Count);
        Assert.Equal(3, service.Read(take: -1).Count);
    }

    [Fact]
    public void GetStatistics_CountsBySeverity()
    {
        var service = new MarkerService();
        service.Change("a", [RandomData(MarkerSeverity.Error), RandomData(MarkerSeverity.Error), RandomData(MarkerSeverity.Warning)]);
        service.Change("b", [RandomData(MarkerSeverity.Info), RandomData(MarkerSeverity.Hint), RandomData((MarkerSeverity)0)]);

        Assert.Equal(new MarkerStatistics(2, 1, 1, 2), service.GetStatistics());
    }

    [Fact]
    public void Remove_Unknown_NoEvent()
    {
        var service = new MarkerService();
        var raised = false;
        service.MarkersChanged += (_, _) => raised = true;

        service.Remove("nobody");

        Assert.False(raised);
    }

    [Fact]
    public void Change_NullOwner_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MarkerService().Change(null!, []));
    }
}
