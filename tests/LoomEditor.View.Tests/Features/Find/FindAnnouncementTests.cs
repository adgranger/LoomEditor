using LoomEditor.View.Accessibility;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Tests.Accessibility.TestHelpers;
using LoomEditor.View.Tests.Features.Find.TestHelpers;
using LoomEditor.View.Tests.Features.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.Find;

public class FindAnnouncementTests
{
    private static void With(string[] lines, Action<FindController, RecordingAnnouncer> test) =>
        TestCodeEditor.WithTestCodeEditor(lines, editor =>
        {
            var recorder = new RecordingAnnouncer();
            using var controller = new FindController(editor.ViewModel, new ManualEditorScheduler()) { Widget = new TestFindWidget(), Announcer = recorder };
            test(controller, recorder);
        });

    private static string Expected(FindController c) =>
        AccessibilityMessages.FindResult(FindMatchesLabel.Format(c.State.MatchesPosition, c.State.MatchesCount), c.State.CurrentMatch, c.State.SearchString, c.ViewModel.Model);

    private static void Search(FindController c, string text)
    {
        if (!c.State.IsRevealed)
        {
            FindActions.StartFind(c);
        }

        c.State.Change(new NewFindReplaceState { SearchString = text }, true);
    }

    [Fact]
    public void Announces_WhenTheMatchesCountChanges() => With(["foo bar foo"], (c, r) =>
    {
        Search(c, "foo");

        Assert.NotEmpty(r.Messages);
        Assert.Equal(Expected(c), r.Messages[^1]);
        Assert.Contains("found for 'foo'", r.Messages[^1]);
    });

    [Fact]
    public void Announces_WhenTheCurrentMatchMoves() => With(["foo bar foo"], (c, r) =>
    {
        Search(c, "foo");
        c.MoveToNextMatch();
        c.MoveToNextMatch();

        Assert.Equal(Expected(c), r.Messages[^1]);
        Assert.Contains("at 1:9", r.Messages[^1]);
    });

    [Fact]
    public void Announces_NoResults_ForASearchWithoutMatch() => With(["foo bar foo"], (c, r) =>
    {
        Search(c, "zzz");

        Assert.Equal("No results found for 'zzz'", r.Messages[^1]);
    });

    [Fact]
    public void Announces_NoResultsFound_ForAnEmptySearchString() => With(["foo bar foo"], (c, r) =>
    {
        Search(c, "x");
        Search(c, "");

        Assert.Equal("No results found", r.Messages[^1]);
    });

    [Fact]
    public void DoesNotAnnounce_WhenTheWidgetIsNotRevealed() => With(["foo bar foo"], (c, r) =>
    {
        c.State.Change(new NewFindReplaceState { SearchString = "foo" }, true);

        Assert.Empty(r.Messages);
    });

    [Fact]
    public void NextMatchWithoutResult_RepeatsNoResults() => With(["foo bar foo"], (c, r) =>
    {
        Search(c, "zzz");
        FindActions.NextMatch(c);

        Assert.Equal(AccessibilityMessages.FindNoResultsAgain("zzz"), r.Messages[^1]);
    });

    [Fact]
    public void UsesTheAnnouncerAssignedLater()
    {
        using var context = new EditorContext();
        using var host = new EditorFeatureHost(context, [() => new FindFeature()]);
        var recorder = new RecordingAnnouncer();
        context.Announcer = recorder;

        context.FindController!.State.Change(new NewFindReplaceState { IsRevealed = true, SearchString = "zzz" }, false);

        Assert.Equal("No results found for 'zzz'", recorder.Messages[^1]);
    }
}
