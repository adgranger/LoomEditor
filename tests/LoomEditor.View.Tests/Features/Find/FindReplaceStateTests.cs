using LoomEditor.View.Features;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Features.MultiCursor;

namespace LoomEditor.View.Tests.Features.Find;

public class FindReplaceStateTests
{
    private static List<FindReplaceStateChangedEventArgs> Record(FindReplaceState state)
    {
        List<FindReplaceStateChangedEventArgs> events = [];
        state.Changed += (_, e) => events.Add(e);
        return events;
    }

    [Fact]
    public void Change_SameValues_RaisesNoEvent()
    {
        var state = new FindReplaceState();
        state.Change(new NewFindReplaceState { SearchString = "abc", IsRegex = true }, false);
        var events = Record(state);

        state.Change(new NewFindReplaceState { SearchString = "abc", IsRegex = true }, false);

        Assert.Empty(events);
    }

    [Fact]
    public void Change_MatchCaseOverride_ChangesEffectiveValueOnly()
    {
        var state = new FindReplaceState();
        var events = Record(state);

        state.Change(new NewFindReplaceState { MatchCaseOverride = FindOptionOverride.True }, false);

        Assert.True(state.MatchCase);
        Assert.False(state.ActualMatchCase);
        Assert.True(Assert.Single(events).MatchCase);

        // the override is reset by the next change
        state.Change(new NewFindReplaceState { SearchString = "x" }, false);
        Assert.False(state.MatchCase);
    }

    [Fact]
    public void Change_SearchScopeNull_ClearsScope()
    {
        var state = new FindReplaceState();
        state.Change(new NewFindReplaceState { SearchScope = [new Range(1, 1, 2, 1)] }, false);
        Assert.NotNull(state.SearchScope);
        var events = Record(state);

        state.Change(new NewFindReplaceState { SearchScope = null }, false);

        Assert.Null(state.SearchScope);
        Assert.True(Assert.Single(events).SearchScope);
    }

    [Fact]
    public void Change_SearchScopeAbsent_KeepsScope()
    {
        var state = new FindReplaceState();
        state.Change(new NewFindReplaceState { SearchScope = [new Range(1, 1, 2, 1)] }, false);

        state.Change(new NewFindReplaceState { SearchString = "x" }, false);

        Assert.NotNull(state.SearchScope);
    }

    [Fact]
    public void ChangeMatchInfo_PositionAboveCount_IsClamped()
    {
        var state = new FindReplaceState();

        state.ChangeMatchInfo(9, 3, new Range(1, 1, 1, 2), true);

        Assert.Equal(3, state.MatchesPosition);
        Assert.Equal(3, state.MatchesCount);
        Assert.Equal(new Range(1, 1, 1, 2), state.CurrentMatch);

        state.ChangeMatchInfo(2, 0, null, false);
        Assert.Equal(0, state.MatchesPosition);
        Assert.Equal(new Range(1, 1, 1, 2), state.CurrentMatch);
    }

    [Fact]
    public void CanNavigateForward_LoopOff_FalseOnLastMatch()
    {
        var state = new FindReplaceState();
        state.Change(new NewFindReplaceState { Loop = false }, false);
        state.ChangeMatchInfo(3, 3, null, false);

        Assert.False(state.CanNavigateForward());
        Assert.True(state.CanNavigateBack());

        state.ChangeMatchInfo(1, 3, null, false);
        Assert.True(state.CanNavigateForward());
        Assert.False(state.CanNavigateBack());

        state.Change(new NewFindReplaceState { Loop = true }, false);
        Assert.True(state.CanNavigateBack());
    }

    [Fact]
    public void FindFeature_IsListedBeforeMultiCursorFeature()
    {
        var features = EditorFeatures.Default.Select(f => f()).ToList();

        int find = features.FindIndex(f => f is FindFeature);
        int multiCursor = features.FindIndex(f => f is MultiCursorFeature);

        Assert.True(find >= 0);
        Assert.True(find < multiCursor);
    }
}
