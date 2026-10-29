using LoomEditor.Core.Markers;
using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.GotoError;

namespace LoomEditor.View.Tests.Features.GotoError;

// No upstream test of MarkerList exists.
public sealed class MarkerListTests : IDisposable
{
    private readonly EditorContext _context = new();

    public MarkerListTests()
    {
        _context.SetText("aaa bbb\nccc ddd\neee fff\nggg hhh\niii jjj");
    }

    public void Dispose() => _context.Dispose();

    private static MarkerData At(MarkerSeverity severity, int line, string message = "m") => new(severity, message, line, 1, line, 4);

    private MarkerList List(MarkerSortOrder order, params MarkerData[] markers)
    {
        _context.Markers.Change("test", markers);
        return new MarkerList(_context.Markers, order);
    }

    // Three errors on lines 2, 3 and 5, in position order.
    private MarkerList Errors() => List(MarkerSortOrder.Position, At(MarkerSeverity.Error, 3), At(MarkerSeverity.Error, 5), At(MarkerSeverity.Error, 2));

    private static int SelectedLine(MarkerList list) => list.Selected!.Marker.StartLineNumber;

    [Fact]
    public void Constructor_SortsBySeverityThenPosition()
    {
        using MarkerList list = List(
            MarkerSortOrder.Severity,
            At(MarkerSeverity.Info, 1),
            At(MarkerSeverity.Error, 5),
            At(MarkerSeverity.Warning, 2),
            At(MarkerSeverity.Error, 3));

        var lines = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            list.Move(true, _context.Model, new Position(1, 1));
            lines.Add(SelectedLine(list));
        }

        Assert.Equal([3, 5, 2, 1], lines);
    }

    [Fact]
    public void Constructor_PositionOrder_SortsByPositionThenSeverity()
    {
        using MarkerList list = List(
            MarkerSortOrder.Position,
            At(MarkerSeverity.Info, 2),
            At(MarkerSeverity.Error, 5),
            At(MarkerSeverity.Warning, 2),
            At(MarkerSeverity.Error, 1));

        var order = new List<(int Line, MarkerSeverity Severity)>();
        for (int i = 0; i < 4; i++)
        {
            list.Move(true, _context.Model, new Position(1, 1));
            order.Add((SelectedLine(list), list.Selected!.Marker.Severity));
        }

        Assert.Equal([(1, MarkerSeverity.Error), (2, MarkerSeverity.Warning), (2, MarkerSeverity.Info), (5, MarkerSeverity.Error)], order);
    }

    [Fact]
    public void Constructor_ExcludesHints()
    {
        using MarkerList list = List(MarkerSortOrder.Severity, At(MarkerSeverity.Hint, 1), At(MarkerSeverity.Info, 2));

        list.Move(true, _context.Model, new Position(1, 1));

        Assert.Equal(1, list.Selected!.Total);
        Assert.Equal(2, SelectedLine(list));
    }

    [Fact]
    public void Move_Forward_FromPositionBeforeFirst_SelectsFirst()
    {
        using MarkerList list = Errors();

        Assert.True(list.Move(true, _context.Model, new Position(1, 1)));

        Assert.Equal(2, SelectedLine(list));
    }

    [Fact]
    public void Move_Forward_Repeatedly_WrapsAround()
    {
        using MarkerList list = Errors();
        var lines = new List<int>();

        for (int i = 0; i < 4; i++)
        {
            list.Move(true, _context.Model, new Position(1, 1));
            lines.Add(SelectedLine(list));
        }

        Assert.Equal([2, 3, 5, 2], lines);
    }

    [Fact]
    public void Move_Backward_FromPositionAfterAll_SelectsLast()
    {
        using MarkerList list = Errors();

        list.Move(false, _context.Model, new Position(5, 5));

        Assert.Equal(5, SelectedLine(list));
    }

    [Fact]
    public void Move_Backward_Wraps()
    {
        using MarkerList list = Errors();

        list.Move(false, _context.Model, new Position(1, 1));
        Assert.Equal(5, SelectedLine(list));
        list.Move(false, _context.Model, new Position(1, 1));
        Assert.Equal(3, SelectedLine(list));
        list.Move(false, _context.Model, new Position(1, 1));
        Assert.Equal(2, SelectedLine(list));
        list.Move(false, _context.Model, new Position(1, 1));
        Assert.Equal(5, SelectedLine(list));
    }

    [Fact]
    public void Move_PositionInsideMarker_ForwardSelectsThatMarker()
    {
        using MarkerList list = Errors();

        list.Move(true, _context.Model, new Position(3, 2));

        Assert.Equal(3, SelectedLine(list));
    }

    [Fact]
    public void Move_Backward_WentPast_GoesOneBack()
    {
        using MarkerList list = Errors();

        list.Move(false, _context.Model, new Position(4, 1));

        Assert.Equal(3, SelectedLine(list));
    }

    [Fact]
    public void Move_EmptyMarkerRange_UsesWordAtPosition()
    {
        using MarkerList list = List(MarkerSortOrder.Position, At(MarkerSeverity.Error, 1), new MarkerData(MarkerSeverity.Error, "m", 2, 2, 2, 2));

        // The empty range at (2,2) is widened to the word "ccc" (2,1)-(2,4), which contains (2,1).
        list.Move(false, _context.Model, new Position(2, 1));

        Assert.Equal(2, SelectedLine(list));
    }

    [Fact]
    public void Move_NoMarkers_ReturnsFalse()
    {
        using MarkerList list = List(MarkerSortOrder.Severity);

        Assert.False(list.Move(true, _context.Model, new Position(1, 1)));
        Assert.Null(list.Selected);
    }

    [Fact]
    public void Move_SingleMarker_SecondMoveReturnsFalse()
    {
        using MarkerList list = List(MarkerSortOrder.Severity, At(MarkerSeverity.Error, 2));

        Assert.True(list.Move(true, _context.Model, new Position(1, 1)));
        Assert.False(list.Move(true, _context.Model, new Position(1, 1)));
    }

    [Fact]
    public void Selected_HasIndexAndTotal()
    {
        using MarkerList list = Errors();
        Assert.Null(list.Selected);

        list.Move(true, _context.Model, new Position(1, 1));
        list.Move(true, _context.Model, new Position(1, 1));

        Assert.Equal(2, list.Selected!.Index);
        Assert.Equal(3, list.Selected.Total);
    }

    [Fact]
    public void Find_ReturnsMarkerContainingPosition()
    {
        using MarkerList list = Errors();

        MarkerCoordinate? found = list.Find(new Position(3, 2));

        Assert.Equal(3, found!.Marker.StartLineNumber);
        Assert.Equal(2, found.Index);
        Assert.Null(list.Find(new Position(4, 1)));
    }

    [Fact]
    public void Changed_FiresWhenMarkersChange_AndResetsIndex()
    {
        using MarkerList list = Errors();
        list.Move(true, _context.Model, new Position(1, 1));
        int fired = 0;
        list.Changed += (_, _) => fired++;

        _context.Markers.Change("test", [At(MarkerSeverity.Error, 2), At(MarkerSeverity.Error, 3)]);

        Assert.Equal(1, fired);
        Assert.Null(list.Selected);
    }

    [Fact]
    public void Changed_DoesNotFireWhenSameMarkersReSet()
    {
        using MarkerList list = Errors();
        list.Move(true, _context.Model, new Position(1, 1));
        int fired = 0;
        list.Changed += (_, _) => fired++;

        _context.Markers.Change("test", [At(MarkerSeverity.Error, 2), At(MarkerSeverity.Error, 3), At(MarkerSeverity.Error, 5)]);

        Assert.Equal(0, fired);
        Assert.NotNull(list.Selected);
    }

    [Fact]
    public void Dispose_StopsListening()
    {
        MarkerList list = Errors();
        int fired = 0;
        list.Changed += (_, _) => fired++;
        list.Dispose();

        _context.Markers.Change("test", [At(MarkerSeverity.Error, 1)]);

        Assert.Equal(0, fired);
    }
}
