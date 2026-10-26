using LoomEditor.View.Suggest;
using LoomEditor.View.Tests.Suggest.TestHelpers;

namespace LoomEditor.View.Tests.Suggest;

public class SuggestListStateTests
{
    private static SuggestItem[] Items(int count) =>
        [.. Enumerable.Range(0, count).Select(i => TestSuggestItems.CreateSuggestItem("item" + i, 0))];

    private static SuggestListState State(int count, int selected = 0)
    {
        var state = new SuggestListState();
        state.Reset(Items(count), selected, false);
        return state;
    }

    [Fact]
    public void Reset_SelectsAndClamps()
    {
        SuggestListState state = State(5, 99);

        Assert.Equal(4, state.SelectedIndex);
        Assert.Same(state.Items[4], state.SelectedItem);

        state.Reset(state.Items, -3, true);
        Assert.Equal(0, state.SelectedIndex);
        Assert.True(state.IsFrozen);
    }

    [Fact]
    public void Reset_Empty_SelectionMinusOne()
    {
        SuggestListState state = State(5);

        state.Reset([], 0, false);

        Assert.Equal(-1, state.SelectedIndex);
        Assert.Null(state.SelectedItem);
        Assert.True(state.IsEmpty);
    }

    [Fact]
    public void Reset_SelectionBeyondWindow_Reveals()
    {
        SuggestListState state = State(30, 20);

        Assert.Equal(9, state.FirstVisibleIndex);
        Assert.Equal(20, state.LastVisibleIndex);
    }

    [Fact]
    public void Reset_SameContent_RaisesNothing()
    {
        SuggestListState state = State(5, 2);
        var raised = 0;
        state.Changed += (_, _) => raised++;
        state.SelectionChanged += (_, _) => raised++;

        state.Reset(state.Items, 2, false);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void SelectNext_Wraps()
    {
        SuggestListState state = State(3, 2);

        Assert.True(state.SelectNext());

        Assert.Equal(0, state.SelectedIndex);
    }

    [Fact]
    public void SelectPrevious_Wraps()
    {
        SuggestListState state = State(3);

        Assert.True(state.SelectPrevious());

        Assert.Equal(2, state.SelectedIndex);
    }

    [Fact]
    public void SelectNext_BelowWindow_Scrolls()
    {
        SuggestListState state = State(30, 11);
        Assert.Equal(0, state.FirstVisibleIndex);

        state.SelectNext();

        Assert.Equal(12, state.SelectedIndex);
        Assert.Equal(1, state.FirstVisibleIndex);
    }

    [Fact]
    public void SelectFirst_SelectLast()
    {
        SuggestListState state = State(30, 5);

        Assert.True(state.SelectLast());
        Assert.Equal(29, state.SelectedIndex);
        Assert.Equal(18, state.FirstVisibleIndex);

        Assert.True(state.SelectFirst());
        Assert.Equal(0, state.SelectedIndex);
        Assert.Equal(0, state.FirstVisibleIndex);
    }

    [Fact]
    public void SelectNextPage_ToLastVisibleThenSlides()
    {
        SuggestListState state = State(30);
        var seen = new List<int>();

        for (var i = 0; i < 3; i++)
        {
            state.SelectNextPage();
            seen.Add(state.SelectedIndex);
        }

        Assert.Equal([11, 22, 29], seen);
    }

    [Fact]
    public void SelectPreviousPage_ToFirstVisibleThenSlides()
    {
        SuggestListState state = State(30, 29);
        var seen = new List<int>();

        for (var i = 0; i < 3; i++)
        {
            state.SelectPreviousPage();
            seen.Add(state.SelectedIndex);
        }

        Assert.Equal([18, 7, 0], seen);
    }

    [Fact]
    public void SelectNextPage_FewItems()
    {
        SuggestListState state = State(5);

        state.SelectNextPage();

        Assert.Equal(4, state.SelectedIndex);
    }

    [Fact]
    public void ScrollBy_DoesNotChangeSelection()
    {
        SuggestListState state = State(30, 2);

        Assert.True(state.ScrollBy(5));

        Assert.Equal(5, state.FirstVisibleIndex);
        Assert.Equal(2, state.SelectedIndex);
    }

    [Fact]
    public void ScrollBy_Clamps()
    {
        SuggestListState state = State(30);

        Assert.True(state.ScrollBy(100));
        Assert.Equal(18, state.FirstVisibleIndex);
        Assert.False(state.ScrollBy(1));

        Assert.True(state.ScrollBy(-100));
        Assert.Equal(0, state.FirstVisibleIndex);
        Assert.False(state.ScrollBy(-1));
    }

    [Fact]
    public void Select_ByIndex_RevealsAndRaises()
    {
        SuggestListState state = State(30);
        SuggestItem? raisedItem = null;
        state.SelectionChanged += (_, item) => raisedItem = item;

        Assert.True(state.Select(20));

        Assert.Equal(9, state.FirstVisibleIndex);
        Assert.Same(state.Items[20], raisedItem);
    }

    [Fact]
    public void Select_Invalid_Clamps()
    {
        SuggestListState state = State(5, 2);

        Assert.True(state.Select(99));
        Assert.Equal(4, state.SelectedIndex);

        Assert.True(state.Select(-5));
        Assert.Equal(0, state.SelectedIndex);
    }

    [Fact]
    public void Clear_Resets()
    {
        SuggestListState state = State(30, 20);
        state.Reset(state.Items, 20, true);

        state.Clear();

        Assert.True(state.IsEmpty);
        Assert.Equal(-1, state.SelectedIndex);
        Assert.Equal(0, state.FirstVisibleIndex);
        Assert.False(state.IsFrozen);
    }

    [Fact]
    public void Changed_RaisedOncePerCall()
    {
        SuggestListState state = State(30);
        var changed = 0;
        state.Changed += (_, _) => changed++;

        state.SelectNextPage();
        state.Reset(Items(5), 1, false);
        state.ScrollBy(0);
        state.Clear();

        Assert.Equal(3, changed);
    }

    [Fact]
    public void SelectionChanged_RaisedOnlyWhenTheItemChanges()
    {
        SuggestListState state = State(5);
        var raised = new List<SuggestItem?>();
        state.SelectionChanged += (_, item) => raised.Add(item);

        state.Select(0);
        state.Select(1);
        state.Clear();

        Assert.Equal(2, raised.Count);
        Assert.Equal("item1", raised[0]!.TextLabel);
        Assert.Null(raised[1]);
    }

    [Fact]
    public void VisibleItems_MatchWindow()
    {
        SuggestListState state = State(30, 20);

        Assert.Equal(12, state.VisibleItems.Count);
        Assert.Equal("item9", state.VisibleItems[0].TextLabel);
        Assert.Equal("item20", state.VisibleItems[^1].TextLabel);
    }

    [Fact]
    public void Moves_WhenEmpty_ReturnFalse()
    {
        var state = new SuggestListState();

        Assert.False(state.SelectNext());
        Assert.False(state.SelectPrevious());
        Assert.False(state.SelectFirst());
        Assert.False(state.SelectLast());
        Assert.False(state.SelectNextPage());
        Assert.False(state.SelectPreviousPage());
        Assert.False(state.ScrollBy(3));
        Assert.False(state.Select(0));
    }
}
