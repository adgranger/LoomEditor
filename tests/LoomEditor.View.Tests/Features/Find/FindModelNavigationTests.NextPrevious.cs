using LoomEditor.View.Features.Find;
using LoomEditor.View.Tests.Cursors.TestHelpers;
using LoomEditor.View.Tests.Features.Find.TestHelpers;

namespace LoomEditor.View.Tests.Features.Find;
public partial class FindModelNavigationTests
{
    // Monaco: 'find model prev stays in scope'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindModelPrevStaysInScope(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", WholeWord = true, SearchScope = [new Range(7, 1, 9, 1)] }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToPrevMatch();
        FindModelTestHelpers.AssertFindState(editor, [8, 14, 8, 19], [8, 14, 8, 19], [[7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToPrevMatch();
        FindModelTestHelpers.AssertFindState(editor, [7, 14, 7, 19], [7, 14, 7, 19], [[7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToPrevMatch();
        FindModelTestHelpers.AssertFindState(editor, [8, 14, 8, 19], [8, 14, 8, 19], [[7, 14, 7, 19], [8, 14, 8, 19]]);
        findModel.Dispose();
    });

    // Monaco: 'find model next/prev with no matches'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindModelNextPrevWithNoMatches(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "helloo", WholeWord = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, []);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, []);

        findModel.MoveToPrevMatch();
        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, []);
        findModel.Dispose();
    });

    // Monaco: 'find model next/prev respects cursor position'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindModelNextPrevRespectsCursorPosition(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", WholeWord = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        CursorTestHelpers.MoveTo(editor.ViewModel, 6, 20);
        FindModelTestHelpers.AssertFindState(editor, [6, 20, 6, 20], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [6, 27, 6, 32], [6, 27, 6, 32], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);
        findModel.Dispose();
    });

    // Monaco: 'find ^'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindCaret(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "^", IsRegex = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[1, 1, 1, 1], [2, 1, 2, 1], [3, 1, 3, 1], [4, 1, 4, 1], [5, 1, 5, 1], [6, 1, 6, 1], [7, 1, 7, 1], [8, 1, 8, 1], [9, 1, 9, 1], [10, 1, 10, 1], [11, 1, 11, 1], [12, 1, 12, 1],]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [2, 1, 2, 1], [2, 1, 2, 1], [[1, 1, 1, 1], [2, 1, 2, 1], [3, 1, 3, 1], [4, 1, 4, 1], [5, 1, 5, 1], [6, 1, 6, 1], [7, 1, 7, 1], [8, 1, 8, 1], [9, 1, 9, 1], [10, 1, 10, 1], [11, 1, 11, 1], [12, 1, 12, 1],]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [3, 1, 3, 1], [3, 1, 3, 1], [[1, 1, 1, 1], [2, 1, 2, 1], [3, 1, 3, 1], [4, 1, 4, 1], [5, 1, 5, 1], [6, 1, 6, 1], [7, 1, 7, 1], [8, 1, 8, 1], [9, 1, 9, 1], [10, 1, 10, 1], [11, 1, 11, 1], [12, 1, 12, 1],]);
        findModel.Dispose();
    });

    // Monaco: 'find $'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindDollar(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "$", IsRegex = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[1, 18, 1, 18], [2, 18, 2, 18], [3, 20, 3, 20], [4, 1, 4, 1], [5, 13, 5, 13], [6, 43, 6, 43], [7, 41, 7, 41], [8, 41, 8, 41], [9, 40, 9, 40], [10, 2, 10, 2], [11, 17, 11, 17], [12, 1, 12, 1],]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [1, 18, 1, 18], [1, 18, 1, 18], [[1, 18, 1, 18], [2, 18, 2, 18], [3, 20, 3, 20], [4, 1, 4, 1], [5, 13, 5, 13], [6, 43, 6, 43], [7, 41, 7, 41], [8, 41, 8, 41], [9, 40, 9, 40], [10, 2, 10, 2], [11, 17, 11, 17], [12, 1, 12, 1],]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [2, 18, 2, 18], [2, 18, 2, 18], [[1, 18, 1, 18], [2, 18, 2, 18], [3, 20, 3, 20], [4, 1, 4, 1], [5, 13, 5, 13], [6, 43, 6, 43], [7, 41, 7, 41], [8, 41, 8, 41], [9, 40, 9, 40], [10, 2, 10, 2], [11, 17, 11, 17], [12, 1, 12, 1],]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [3, 20, 3, 20], [3, 20, 3, 20], [[1, 18, 1, 18], [2, 18, 2, 18], [3, 20, 3, 20], [4, 1, 4, 1], [5, 13, 5, 13], [6, 43, 6, 43], [7, 41, 7, 41], [8, 41, 8, 41], [9, 40, 9, 40], [10, 2, 10, 2], [11, 17, 11, 17], [12, 1, 12, 1],]);
        findModel.Dispose();
    });

    // Monaco: 'find next ^$'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindNextCaretDollar(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "^$", IsRegex = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[4, 1, 4, 1], [12, 1, 12, 1],]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [4, 1, 4, 1], [4, 1, 4, 1], [[4, 1, 4, 1], [12, 1, 12, 1],]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [12, 1, 12, 1], [12, 1, 12, 1], [[4, 1, 4, 1], [12, 1, 12, 1],]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [4, 1, 4, 1], [4, 1, 4, 1], [[4, 1, 4, 1], [12, 1, 12, 1],]);
        findModel.Dispose();
    });

    // Monaco: 'find .*'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindDotStar(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = ".*", IsRegex = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[1, 1, 1, 18], [2, 1, 2, 18], [3, 1, 3, 20], [4, 1, 4, 1], [5, 1, 5, 13], [6, 1, 6, 43], [7, 1, 7, 41], [8, 1, 8, 41], [9, 1, 9, 40], [10, 1, 10, 2], [11, 1, 11, 17], [12, 1, 12, 1],]);
        findModel.Dispose();
    });

    // Monaco: 'find next ^.*$'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindNextCaretDotStarDollar(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "^.*$", IsRegex = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[1, 1, 1, 18], [2, 1, 2, 18], [3, 1, 3, 20], [4, 1, 4, 1], [5, 1, 5, 13], [6, 1, 6, 43], [7, 1, 7, 41], [8, 1, 8, 41], [9, 1, 9, 40], [10, 1, 10, 2], [11, 1, 11, 17], [12, 1, 12, 1],]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 18], [1, 1, 1, 18], [[1, 1, 1, 18], [2, 1, 2, 18], [3, 1, 3, 20], [4, 1, 4, 1], [5, 1, 5, 13], [6, 1, 6, 43], [7, 1, 7, 41], [8, 1, 8, 41], [9, 1, 9, 40], [10, 1, 10, 2], [11, 1, 11, 17], [12, 1, 12, 1],]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [2, 1, 2, 18], [2, 1, 2, 18], [[1, 1, 1, 18], [2, 1, 2, 18], [3, 1, 3, 20], [4, 1, 4, 1], [5, 1, 5, 13], [6, 1, 6, 43], [7, 1, 7, 41], [8, 1, 8, 41], [9, 1, 9, 40], [10, 1, 10, 2], [11, 1, 11, 17], [12, 1, 12, 1],]);
        findModel.Dispose();
    });

    // Monaco: 'find prev ^.*$'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindPrevCaretDotStarDollar(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "^.*$", IsRegex = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[1, 1, 1, 18], [2, 1, 2, 18], [3, 1, 3, 20], [4, 1, 4, 1], [5, 1, 5, 13], [6, 1, 6, 43], [7, 1, 7, 41], [8, 1, 8, 41], [9, 1, 9, 40], [10, 1, 10, 2], [11, 1, 11, 17], [12, 1, 12, 1],]);

        findModel.MoveToPrevMatch();
        FindModelTestHelpers.AssertFindState(editor, [12, 1, 12, 1], [12, 1, 12, 1], [[1, 1, 1, 18], [2, 1, 2, 18], [3, 1, 3, 20], [4, 1, 4, 1], [5, 1, 5, 13], [6, 1, 6, 43], [7, 1, 7, 41], [8, 1, 8, 41], [9, 1, 9, 40], [10, 1, 10, 2], [11, 1, 11, 17], [12, 1, 12, 1],]);

        findModel.MoveToPrevMatch();
        FindModelTestHelpers.AssertFindState(editor, [11, 1, 11, 17], [11, 1, 11, 17], [[1, 1, 1, 18], [2, 1, 2, 18], [3, 1, 3, 20], [4, 1, 4, 1], [5, 1, 5, 13], [6, 1, 6, 43], [7, 1, 7, 41], [8, 1, 8, 41], [9, 1, 9, 40], [10, 1, 10, 2], [11, 1, 11, 17], [12, 1, 12, 1],]);
        findModel.Dispose();
    });

    // Monaco: 'find prev ^$'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindPrevCaretDollar(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "^$", IsRegex = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[4, 1, 4, 1], [12, 1, 12, 1],]);

        findModel.MoveToPrevMatch();
        FindModelTestHelpers.AssertFindState(editor, [12, 1, 12, 1], [12, 1, 12, 1], [[4, 1, 4, 1], [12, 1, 12, 1],]);

        findModel.MoveToPrevMatch();
        FindModelTestHelpers.AssertFindState(editor, [4, 1, 4, 1], [4, 1, 4, 1], [[4, 1, 4, 1], [12, 1, 12, 1],]);

        findModel.MoveToPrevMatch();
        FindModelTestHelpers.AssertFindState(editor, [12, 1, 12, 1], [12, 1, 12, 1], [[4, 1, 4, 1], [12, 1, 12, 1],]);
        findModel.Dispose();
    });

    // Monaco: 'listens to model content changes'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ListensToModelContentChanges(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", ReplaceString = "hi", WholeWord = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        editor.Model.SetValue("hello\nhi");
        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, []);
        findModel.Dispose();
    });
}
