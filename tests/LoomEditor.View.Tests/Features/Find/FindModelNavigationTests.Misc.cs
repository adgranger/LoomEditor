using LoomEditor.Core.Text;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Tests.Features.Find.TestHelpers;

namespace LoomEditor.View.Tests.Features.Find;
public partial class FindModelNavigationTests
{
    // Monaco: 'selectAllMatches'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectAllMatches(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", ReplaceString = "hi", WholeWord = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.SelectAllMatches();

        FindModelTestHelpers.AssertSelections(editor, new Selection(6, 14, 6, 19),
            new Selection(6, 27, 6, 32),
            new Selection(7, 14, 7, 19),
            new Selection(8, 14, 8, 19));

        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);
        findModel.Dispose();
    });

    // Monaco: 'issue #14143 selectAllMatches should maintain primary cursor if feasible'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Issue14143SelectAllMatchesShouldMaintainPrimaryCursorIfFeasible(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", ReplaceString = "hi", WholeWord = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        editor.SetSelection(new Range(7, 14, 7, 19));

        findModel.SelectAllMatches();

        FindModelTestHelpers.AssertSelections(editor, new Selection(7, 14, 7, 19),
            new Selection(6, 14, 6, 19),
            new Selection(6, 27, 6, 32),
            new Selection(8, 14, 8, 19));

        Assert.Equal(new Selection(7, 14, 7, 19).ToString(), editor.GetSelection().ToString());

        FindModelTestHelpers.AssertFindState(editor, [7, 14, 7, 19], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);
        findModel.Dispose();
    });

    // Monaco: 'issue #1914: NPE when there is only one find match'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Issue1914NPEWhenThereIsOnlyOneFindMatch(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "cool.h" }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[2, 11, 2, 17]]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [2, 11, 2, 17], [2, 11, 2, 17], [[2, 11, 2, 17]]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [2, 11, 2, 17], [2, 11, 2, 17], [[2, 11, 2, 17]]);
        findModel.Dispose();
    });

    // Monaco: 'issue #27083. search scope works even if it is a single line'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Issue27083SearchScopeWorksEvenIfItIsASingleLine(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", WholeWord = true, SearchScope = [new Range(7, 1, 8, 1)] }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[7, 14, 7, 19]]);
        findModel.Dispose();
    });

    // Monaco: 'issue #3516: Control behavior of "Next" operations (not looping back to beginning)'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Issue3516ControlBehaviorOfNextOperationsNotLoopingBackToBeginning(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", Loop = false }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        Assert.Equal(5, findState.MatchesCount);

        // Test next operations
        Assert.Equal(0, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToNextMatch();
        Assert.Equal(1, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.False(findState.CanNavigateBack());

        findModel.MoveToNextMatch();
        Assert.Equal(2, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToNextMatch();
        Assert.Equal(3, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToNextMatch();
        Assert.Equal(4, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToNextMatch();
        Assert.Equal(5, findState.MatchesPosition);
        Assert.False(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToNextMatch();
        Assert.Equal(5, findState.MatchesPosition);
        Assert.False(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToNextMatch();
        Assert.Equal(5, findState.MatchesPosition);
        Assert.False(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        // Test previous operations
        findModel.MoveToPrevMatch();
        Assert.Equal(4, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToPrevMatch();
        Assert.Equal(3, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToPrevMatch();
        Assert.Equal(2, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToPrevMatch();
        Assert.Equal(1, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.False(findState.CanNavigateBack());

        findModel.MoveToPrevMatch();
        Assert.Equal(1, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.False(findState.CanNavigateBack());

        findModel.MoveToPrevMatch();
        Assert.Equal(1, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.False(findState.CanNavigateBack());
    });

    // Monaco: 'issue #3516: Control behavior of "Next" operations (looping back to beginning)'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Issue3516ControlBehaviorOfNextOperationsLoopingBackToBeginning(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello" }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        Assert.Equal(5, findState.MatchesCount);

        // Test next operations
        Assert.Equal(0, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToNextMatch();
        Assert.Equal(1, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToNextMatch();
        Assert.Equal(2, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToNextMatch();
        Assert.Equal(3, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToNextMatch();
        Assert.Equal(4, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToNextMatch();
        Assert.Equal(5, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToNextMatch();
        Assert.Equal(1, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToNextMatch();
        Assert.Equal(2, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        // Test previous operations
        findModel.MoveToPrevMatch();
        Assert.Equal(1, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToPrevMatch();
        Assert.Equal(5, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToPrevMatch();
        Assert.Equal(4, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToPrevMatch();
        Assert.Equal(3, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToPrevMatch();
        Assert.Equal(2, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());

        findModel.MoveToPrevMatch();
        Assert.Equal(1, findState.MatchesPosition);
        Assert.True(findState.CanNavigateForward());
        Assert.True(findState.CanNavigateBack());
    });
}
