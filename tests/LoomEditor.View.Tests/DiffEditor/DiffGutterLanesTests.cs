using LoomEditor.View.DiffEditor;

namespace LoomEditor.View.Tests.DiffEditor;

public class DiffGutterLanesTests
{
    [Fact]
    public void Compute_NothingOn_HasNoLane()
    {
        DiffGutterLanes lanes = DiffGutterLanes.Compute(true, false, false, false, false, false);

        Assert.Equal(new DiffGutterLanes(0, -1, -1, -1, -1), lanes);
    }

    [Fact]
    public void Compute_AllOn_OrdersLanesAsDecision1()
    {
        DiffGutterLanes lanes = DiffGutterLanes.Compute(true, true, true, true, true, true);

        Assert.Equal(new DiffGutterLanes(4, 0, 1, 2, 3), lanes);
    }

    [Fact]
    public void Compute_NotSideBySide_HasNoLane()
    {
        DiffGutterLanes lanes = DiffGutterLanes.Compute(false, true, true, true, true, true);

        Assert.Equal(0, lanes.Count);
    }

    [Fact]
    public void Compute_LineActionWithoutSelection_HasNoLineActionLane()
    {
        DiffGutterLanes lanes = DiffGutterLanes.Compute(true, false, true, true, true, false);

        Assert.Equal(new DiffGutterLanes(1, -1, -1, 0, -1), lanes);
    }
}
