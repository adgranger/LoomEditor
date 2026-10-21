using LoomEditor.Core.Text;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Tests.Features.Find.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.Find;

public partial class FindModelNavigationTests
{
    // Monaco: 'issue #288515: Wrong current index in find widget if matches > 1000'
    [Fact]
    public void Issue288515_WrongCurrentIndexInFindWidgetIfMatchesOver1000() =>
        TestCodeEditor.WithTestCodeEditor(Enumerable.Repeat("hello", 1001).ToArray(), editor =>
        {
            // Place cursor at line 900, selecting 'hello'
            editor.SetSelection(new Selection(900, 1, 900, 6));

            var findState = new FindReplaceState();
            findState.Change(new NewFindReplaceState { SearchString = "hello" }, false);
            _ = new FindModelBoundToEditorModel(editor.ViewModel, findState, new Features.TestHelpers.ManualEditorScheduler());

            Assert.Equal(1001, findState.MatchesCount);
            // With cursor selecting 'hello' at line 900, matchesPosition should be 900
            Assert.Equal(900, findState.MatchesPosition);
        });

    [Fact]
    public void ContentChange_RunsResearchAfter100Ms() => FindModelTestHelpers.FindTest(false, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello" }, false);
        _ = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);
        Assert.Equal(5, findState.MatchesCount);

        editor.Model.ApplyEdits([new TextEdit(new Range(1, 1, 1, 1), "hello ")]);

        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(99));
        Assert.Equal(5, findState.MatchesCount);
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1));
        Assert.Equal(6, findState.MatchesCount);
    });

    [Fact]
    public void Dispose_CancelsPendingResearch() => FindModelTestHelpers.FindTest(false, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello" }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);
        editor.Model.ApplyEdits([new TextEdit(new Range(1, 1, 1, 1), "hello ")]);

        findModel.Dispose();
        scheduler.RunAll();

        Assert.Equal(5, findState.MatchesCount);
    });
}
