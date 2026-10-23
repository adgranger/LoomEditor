using LoomEditor.Core.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Accessibility.TestHelpers.AutomationTestHelpers;

namespace LoomEditor.View.Tests.Accessibility;

public class AutomationTextSelectionTests
{
    private static Selection Sel(int sl, int sc, int el, int ec) => new(sl, sc, el, ec);

    [Fact]
    public void Select_ReplacesEverything() =>
        Assert.Equal([Sel(1, 2, 1, 4)], AutomationTextSelection.Select([Sel(2, 1, 2, 3), Sel(3, 1, 3, 2)], Rng(1, 2, 1, 4)));

    [Fact]
    public void Select_UsesTheStartAsAnchor()
    {
        Selection s = Assert.Single(AutomationTextSelection.Select([], Rng(1, 2, 3, 4)));
        Assert.Equal(Pos(1, 2), s.SelectionStart);
        Assert.Equal(Pos(3, 4), s.Position);
    }

    [Fact]
    public void Add_AppendsTheRange() =>
        Assert.Equal([Sel(2, 1, 2, 3), Sel(1, 2, 1, 4)], AutomationTextSelection.Add([Sel(2, 1, 2, 3)], Rng(1, 2, 1, 4)));

    [Fact]
    public void Remove_AnEqualRange_RemovesIt() =>
        Assert.Equal([Sel(3, 1, 3, 2)], AutomationTextSelection.Remove([Sel(2, 1, 2, 3), Sel(3, 1, 3, 2)], Rng(2, 1, 2, 3)));

    [Fact]
    public void Remove_NoEqualRange_ReturnsTheSameSelections()
    {
        Selection[] current = [Sel(2, 1, 2, 3), Sel(3, 1, 3, 2)];
        Assert.Same(current, AutomationTextSelection.Remove(current, Rng(1, 1, 1, 2)));
    }

    [Fact]
    public void Remove_TheOnlySelection_CollapsesIt() =>
        Assert.Equal([Selection.Collapsed(Pos(2, 3))], AutomationTextSelection.Remove([Sel(2, 1, 2, 3)], Rng(2, 1, 2, 3)));

    [Fact]
    public void Remove_FirstOfTwoEqual_RemovesOnlyOne() =>
        Assert.Equal([Sel(2, 1, 2, 3)], AutomationTextSelection.Remove([Sel(2, 1, 2, 3), Sel(2, 1, 2, 3)], Rng(2, 1, 2, 3)));

    [Fact]
    public void Remove_ASelectionWithTheOppositeDirection_StillMatches() =>
        Assert.Equal([Sel(3, 1, 3, 2)], AutomationTextSelection.Remove([Sel(2, 3, 2, 1), Sel(3, 1, 3, 2)], Rng(2, 1, 2, 3)));

    [Fact]
    public void Add_ThroughTheViewModel_MergesOverlappingSelections() =>
        TestCodeEditor.WithTestCodeEditor("hello world", editor =>
        {
            var vm = editor.ViewModel;
            vm.SetSelections("test", [Sel(1, 1, 1, 6)]);
            vm.SetSelections("automation", AutomationTextSelection.Add(vm.GetSelections(), Rng(1, 4, 1, 9)));
            Assert.Equal([Sel(1, 1, 1, 9)], vm.GetSelections());
        });
}
