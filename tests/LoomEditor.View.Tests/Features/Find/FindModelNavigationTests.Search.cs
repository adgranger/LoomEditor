// Ported from microsoft/vscode: src/vs/editor/contrib/find/test/browser/findModel.test.ts (suite 'FindModel')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Features.Find;
using LoomEditor.View.Tests.Cursors.TestHelpers;
using LoomEditor.View.Tests.Features.Find.TestHelpers;

namespace LoomEditor.View.Tests.Features.Find;
public partial class FindModelNavigationTests
{
    // Monaco: 'incremental find from beginning of file'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IncrementalFindFromBeginningOfFile(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        editor.SetPosition(1, 1);
        var findState = new FindReplaceState();
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        // simulate typing the search string
        findState.Change(new NewFindReplaceState { SearchString = "H" }, true);
        FindModelTestHelpers.AssertFindState(editor, [1, 12, 1, 13], [1, 12, 1, 13], [[1, 12, 1, 13], [2, 16, 2, 17], [6, 14, 6, 15], [6, 27, 6, 28], [7, 14, 7, 15], [8, 14, 8, 15], [9, 14, 9, 15]]);

        // simulate typing the search string
        findState.Change(new NewFindReplaceState { SearchString = "He" }, true);
        FindModelTestHelpers.AssertFindState(editor, [1, 12, 1, 14], [1, 12, 1, 14], [[1, 12, 1, 14], [6, 14, 6, 16], [6, 27, 6, 29], [7, 14, 7, 16], [8, 14, 8, 16], [9, 14, 9, 16]]);

        // simulate typing the search string
        findState.Change(new NewFindReplaceState { SearchString = "Hello" }, true);
        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], [6, 14, 6, 19], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19], [9, 14, 9, 19]]);

        // simulate toggling on `matchCase`
        findState.Change(new NewFindReplaceState { MatchCase = true }, true);
        FindModelTestHelpers.AssertFindState(editor, [6, 27, 6, 32], [6, 27, 6, 32], [[6, 27, 6, 32], [8, 14, 8, 19]]);

        // simulate typing the search string
        findState.Change(new NewFindReplaceState { SearchString = "hello" }, true);
        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], [6, 14, 6, 19], [[6, 14, 6, 19], [7, 14, 7, 19], [9, 14, 9, 19]]);

        // simulate toggling on `wholeWord`
        findState.Change(new NewFindReplaceState { WholeWord = true }, true);
        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], [6, 14, 6, 19], [[6, 14, 6, 19], [7, 14, 7, 19]]);

        // simulate toggling off `matchCase`
        findState.Change(new NewFindReplaceState { MatchCase = false }, true);
        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], [6, 14, 6, 19], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        // simulate toggling off `wholeWord`
        findState.Change(new NewFindReplaceState { WholeWord = false }, true);
        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], [6, 14, 6, 19], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19], [9, 14, 9, 19]]);

        // simulate adding a search scope
        findState.Change(new NewFindReplaceState { SearchScope = [new Range(8, 1, 10, 1)] }, true);
        FindModelTestHelpers.AssertFindState(editor, [8, 14, 8, 19], [8, 14, 8, 19], [[8, 14, 8, 19], [9, 14, 9, 19]]);

        // simulate removing the search scope
        findState.Change(new NewFindReplaceState { SearchScope = null }, true);
        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], [6, 14, 6, 19], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19], [9, 14, 9, 19]]);
        findModel.Dispose();
    });

    // Monaco: 'find model removes its decorations'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindModelRemovesItsDecorations(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello" }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        Assert.Equal(5, findState.MatchesCount);
        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19], [9, 14, 9, 19]]);
        findModel.Dispose();

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, []);
    });

    // Monaco: 'find model updates state matchesCount'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindModelUpdatesStateMatchesCount(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello" }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        Assert.Equal(5, findState.MatchesCount);
        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19], [9, 14, 9, 19]]);

        findState.Change(new NewFindReplaceState { SearchString = "helloo" }, false);
        Assert.Equal(0, findState.MatchesCount);
        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, []);
        findModel.Dispose();
    });

    // Monaco: 'find model reacts to position change'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindModelReactsToPositionChange(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello" }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19], [9, 14, 9, 19]]);

        CursorTestHelpers.MoveTo(editor.ViewModel, 6, 20);

        FindModelTestHelpers.AssertFindState(editor, [6, 20, 6, 20], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19], [9, 14, 9, 19]]);

        findState.Change(new NewFindReplaceState { SearchString = "Hello" }, true);
        FindModelTestHelpers.AssertFindState(editor, [6, 27, 6, 32], [6, 27, 6, 32], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19], [9, 14, 9, 19]]);
        findModel.Dispose();
    });

    // Monaco: 'find model next'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindModelNext(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", WholeWord = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], [6, 14, 6, 19], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [6, 27, 6, 32], [6, 27, 6, 32], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [7, 14, 7, 19], [7, 14, 7, 19], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [8, 14, 8, 19], [8, 14, 8, 19], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], [6, 14, 6, 19], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);
        findModel.Dispose();
    });

    // Monaco: 'find model next stays in scope'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindModelNextStaysInScope(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", WholeWord = true, SearchScope = [new Range(7, 1, 9, 1)] }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [7, 14, 7, 19], [7, 14, 7, 19], [[7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [8, 14, 8, 19], [8, 14, 8, 19], [[7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [7, 14, 7, 19], [7, 14, 7, 19], [[7, 14, 7, 19], [8, 14, 8, 19]]);
        findModel.Dispose();
    });

    // Monaco: 'multi-selection find model next stays in scope (overlap)'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSelectionFindModelNextStaysInScopeOverlap(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", WholeWord = true, SearchScope = [new Range(7, 1, 8, 2), new Range(8, 1, 9, 1)] }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [7, 14, 7, 19], [7, 14, 7, 19], [[7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [8, 14, 8, 19], [8, 14, 8, 19], [[7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [7, 14, 7, 19], [7, 14, 7, 19], [[7, 14, 7, 19], [8, 14, 8, 19]]);
        findModel.Dispose();
    });

    // Monaco: 'multi-selection find model next stays in scope'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MultiSelectionFindModelNextStaysInScope(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", MatchCase = true, WholeWord = false, SearchScope = [new Range(6, 1, 7, 38), new Range(9, 3, 9, 38)] }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [7, 14, 7, 19], [9, 14, 9, 19]]);
        // `matchCase: false` would find the match [6, 27, 6, 32] as well; `wholeWord: true` would exclude [9, 14, 9, 19].

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], [6, 14, 6, 19], [[6, 14, 6, 19], [7, 14, 7, 19], [9, 14, 9, 19],]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [7, 14, 7, 19], [7, 14, 7, 19], [[6, 14, 6, 19], [7, 14, 7, 19], [9, 14, 9, 19],]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [9, 14, 9, 19], [9, 14, 9, 19], [[6, 14, 6, 19], [7, 14, 7, 19], [9, 14, 9, 19],]);

        findModel.MoveToNextMatch();
        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], [6, 14, 6, 19], [[6, 14, 6, 19], [7, 14, 7, 19], [9, 14, 9, 19],]);
        findModel.Dispose();
    });

    // Monaco: 'find model prev'
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FindModelPrev(bool chunked) => FindModelTestHelpers.FindTest(chunked, (editor, scheduler) =>
    {
        var findState = new FindReplaceState();
        findState.Change(new NewFindReplaceState { SearchString = "hello", WholeWord = true }, false);
        var findModel = new FindModelBoundToEditorModel(editor.ViewModel, findState, scheduler);

        FindModelTestHelpers.AssertFindState(editor, [1, 1, 1, 1], null, [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToPrevMatch();
        FindModelTestHelpers.AssertFindState(editor, [8, 14, 8, 19], [8, 14, 8, 19], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToPrevMatch();
        FindModelTestHelpers.AssertFindState(editor, [7, 14, 7, 19], [7, 14, 7, 19], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToPrevMatch();
        FindModelTestHelpers.AssertFindState(editor, [6, 27, 6, 32], [6, 27, 6, 32], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToPrevMatch();
        FindModelTestHelpers.AssertFindState(editor, [6, 14, 6, 19], [6, 14, 6, 19], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);

        findModel.MoveToPrevMatch();
        FindModelTestHelpers.AssertFindState(editor, [8, 14, 8, 19], [8, 14, 8, 19], [[6, 14, 6, 19], [6, 27, 6, 32], [7, 14, 7, 19], [8, 14, 8, 19]]);
        findModel.Dispose();
    });
}
