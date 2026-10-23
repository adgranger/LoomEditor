using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Search;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.Find;

// No upstream test file: the class is covered by findModel.test.ts (P5-68, P5-69).
public class FindDecorationsTests
{
    private static readonly string Lf = ((char)10).ToString();

    private static FindMatch[] Matches(params Range[] ranges) => [.. ranges.Select(r => new FindMatch(r, null))];

    private static IReadOnlyList<ModelDecoration> All(TextModel model) => model.GetDecorationsInRange(model.FullModelRange);

    private static IEnumerable<ModelDecoration> WithClass(TextModel model, string className) =>
        All(model).Where(d => d.Options.ClassName == className);

    private static void WithDecorations(string text, Action<TextModel, FindDecorations> test) =>
        TestCodeEditor.WithTestCodeEditor(text, editor =>
        {
            using var decorations = new FindDecorations(editor.ViewModel);
            test(editor.Model, decorations);
        });

    [Fact]
    public void Set_AddsOneFindMatchDecorationPerMatch() => WithDecorations("foo foo foo", (model, decorations) =>
    {
        decorations.Set(Matches(new Range(1, 1, 1, 4), new Range(1, 5, 1, 8), new Range(1, 9, 1, 12)), null);

        Assert.Equal(3, decorations.GetCount());
        Assert.Equal(3, WithClass(model, "findMatch").Count());
        Assert.All(WithClass(model, "findMatch"), d => Assert.Equal(10, d.Options.ZIndex));
    });

    [Fact]
    public void Set_MoreThan1000Matches_UsesNoOverviewOptions() => WithDecorations(new string('a', 1001), (model, decorations) =>
    {
        decorations.Set(Matches([.. Enumerable.Range(1, 1001).Select(i => new Range(1, i, 1, i + 1))]), null);

        Assert.Equal(1001, decorations.GetCount());
        Assert.All(WithClass(model, "findMatch"), d => Assert.Equal("find-match-no-overview", d.Options.Description));
    });

    [Fact]
    public void Set_FindScopes_AddsWholeLineScopeDecorations() => WithDecorations("a" + Lf + "b" + Lf + "c", (model, decorations) =>
    {
        decorations.Set([], [new Range(1, 1, 1, 2), new Range(3, 1, 3, 2)]);

        Assert.Equal([new Range(1, 1, 1, 2), new Range(3, 1, 3, 2)], decorations.GetFindScopes());
        Assert.Equal(new Range(1, 1, 1, 2), decorations.GetFindScope());
        Assert.All(WithClass(model, "findScope"), d => Assert.True(d.Options.IsWholeLine));
        Assert.Equal(2, WithClass(model, "findScope").Count());

        decorations.Set([], null);
        Assert.Null(decorations.GetFindScopes());
    });

    [Fact]
    public void SetCurrentFindMatch_ReturnsOneBasedPositionAndSwapsOptions() => WithDecorations("foo foo foo", (model, decorations) =>
    {
        decorations.Set(Matches(new Range(1, 1, 1, 4), new Range(1, 5, 1, 8), new Range(1, 9, 1, 12)), null);

        Assert.Equal(2, decorations.SetCurrentFindMatch(new Range(1, 5, 1, 8)));
        Assert.Equal(new Range(1, 5, 1, 8), Assert.Single(WithClass(model, "currentFindMatch")).Range);
        Assert.Single(WithClass(model, "rangeHighlight"));
        Assert.Equal(2, WithClass(model, "findMatch").Count());
        Assert.Equal(2, decorations.GetCurrentMatchesPosition(new Range(1, 5, 1, 8)));

        Assert.Equal(0, decorations.SetCurrentFindMatch(null));
        Assert.Empty(WithClass(model, "currentFindMatch"));
        Assert.Empty(WithClass(model, "rangeHighlight"));
        Assert.Equal(3, WithClass(model, "findMatch").Count());
    });

    [Fact]
    public void SetCurrentFindMatch_MultilineMatchEndingAtColumn1_RangeHighlightStopsOnPreviousLine() =>
        WithDecorations("abc" + Lf + "def", (model, decorations) =>
        {
            decorations.Set(Matches(new Range(1, 2, 2, 1)), null);

            decorations.SetCurrentFindMatch(new Range(1, 2, 2, 1));

            Assert.Equal(new Range(1, 2, 1, 4), Assert.Single(WithClass(model, "rangeHighlight")).Range);
        });

    [Fact]
    public void MatchAfterPosition_AfterLastMatch_WrapsToFirst() => WithDecorations("foo foo foo", (_, decorations) =>
    {
        decorations.Set(Matches(new Range(1, 1, 1, 4), new Range(1, 5, 1, 8), new Range(1, 9, 1, 12)), null);

        Assert.Equal(new Range(1, 5, 1, 8), decorations.MatchAfterPosition(new Position(1, 2)));
        Assert.Equal(new Range(1, 9, 1, 12), decorations.MatchAfterPosition(new Position(1, 9)));
        Assert.Equal(new Range(1, 1, 1, 4), decorations.MatchAfterPosition(new Position(1, 10)));
    });

    [Fact]
    public void MatchBeforePosition_BeforeFirstMatch_WrapsToLast() => WithDecorations("foo foo foo", (_, decorations) =>
    {
        decorations.Set(Matches(new Range(1, 1, 1, 4), new Range(1, 5, 1, 8), new Range(1, 9, 1, 12)), null);

        Assert.Equal(new Range(1, 1, 1, 4), decorations.MatchBeforePosition(new Position(1, 5)));
        Assert.Equal(new Range(1, 9, 1, 12), decorations.MatchBeforePosition(new Position(1, 3)));

        decorations.Set([], null);
        Assert.Null(decorations.MatchBeforePosition(new Position(1, 3)));
        Assert.Null(decorations.MatchAfterPosition(new Position(1, 3)));
    });

    [Fact]
    public void Reset_RemovesNothingAndForgetsIds() => WithDecorations("foo foo", (model, decorations) =>
    {
        decorations.Set(Matches(new Range(1, 1, 1, 4), new Range(1, 5, 1, 8)), null);

        decorations.Reset();

        Assert.Equal(0, decorations.GetCount());
        Assert.Equal(2, WithClass(model, "findMatch").Count());
    });

    [Fact]
    public void Dispose_RemovesAllDecorations() => TestCodeEditor.WithTestCodeEditor("foo foo", editor =>
    {
        var decorations = new FindDecorations(editor.ViewModel);
        decorations.Set(Matches(new Range(1, 1, 1, 4), new Range(1, 5, 1, 8)), [new Range(1, 1, 1, 8)]);
        decorations.SetCurrentFindMatch(new Range(1, 1, 1, 4));

        decorations.Dispose();

        // the editor's own tracked ranges (the cursors) stay
        Assert.DoesNotContain(All(editor.Model), d => d.Options.ClassName is "findMatch" or "currentFindMatch" or "findScope" or "rangeHighlight");
    });

    [Fact]
    public void FindDecorations_Matches_HaveTheRulerColor() => WithDecorations("foo foo", (model, decorations) =>
    {
        decorations.Set(Matches(new Range(1, 1, 1, 4), new Range(1, 5, 1, 8)), null);
        decorations.SetCurrentFindMatch(new Range(1, 1, 1, 4));

        Assert.Equal(2, All(model).Count(d => d.Options.OverviewRuler?.Color?.ThemeColorId == "editorOverviewRuler.findMatchForeground"));
        Assert.All(All(model).Where(d => d.Options.ClassName is "findMatch" or "currentFindMatch"), d =>
            Assert.Equal(OverviewRulerLane.Center, d.Options.OverviewRuler!.Position));
    });

    [Fact]
    public void FindDecorations_MoreThan1000Matches_AddsApproximateRulerDecorations() => WithDecorations(string.Join(Lf, Enumerable.Repeat("a", 3000)), (model, decorations) =>
    {
        // lines 1 and 2 merge into one decoration, line 2000 is far enough to get its own
        decorations.Set(Matches([.. Enumerable.Range(0, 1001).Select(_ => new Range(1, 1, 1, 2)), new Range(2, 1, 2, 2), new Range(2000, 1, 2000, 2)]), null);

        List<ModelDecoration> approximate = [.. All(model).Where(d => d.Options.Description == "find-match-only-overview").OrderBy(d => d.Range.StartLineNumber)];
        Assert.Equal([new Range(1, 1, 2, 1), new Range(2000, 1, 2000, 1)], approximate.Select(d => d.Range));
        Assert.All(approximate, d => Assert.Equal("editorOverviewRuler.findMatchForeground", d.Options.OverviewRuler!.Color!.ThemeColorId));

        decorations.Set([], null);
        Assert.DoesNotContain(All(model), d => d.Options.Description == "find-match-only-overview");
    });

    [Fact]
    public void FindDecorations_Matches_HaveTheMinimapColor() => WithDecorations("foo foo", (model, decorations) =>
    {
        decorations.Set(Matches(new Range(1, 1, 1, 4), new Range(1, 5, 1, 8)), null);

        Assert.All(WithClass(model, "findMatch").Concat(WithClass(model, "currentFindMatch")), d =>
        {
            Assert.Equal("minimap.findMatchHighlight", d.Options.Minimap!.Color!.ThemeColorId);
            Assert.Equal(MinimapPosition.Inline, d.Options.Minimap.Position);
        });
        Assert.Equal(2, WithClass(model, "findMatch").Count() + WithClass(model, "currentFindMatch").Count());
    });
}
