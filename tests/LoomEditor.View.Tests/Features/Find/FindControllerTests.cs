// Ported from microsoft/vscode: src/vs/editor/contrib/find/test/browser/findController.test.ts (suites 'FindController', 'FindController query options persistence')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Tests.Features.Find.TestHelpers;
using LoomEditor.View.Tests.Features.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.Find;

public class FindControllerTests
{
    private static void WithController(string[] lines, Action<TestCodeEditor, FindController, TestFindWidget> test, EditorFindOptions? options = null) =>
        TestCodeEditor.WithTestCodeEditor(lines, editor =>
        {
            var widget = new TestFindWidget();
            using var controller = new FindController(editor.ViewModel, new ManualEditorScheduler(), options) { Widget = widget };
            test(editor, controller, widget);
        });

    private static int[] FromSelection(Selection s) => [s.Range.StartLineNumber, s.Range.StartColumn, s.Range.EndLineNumber, s.Range.EndColumn];

    private static void Select(TestCodeEditor editor, int startLine, int startColumn, int endLine, int endColumn) =>
        editor.ViewModel.SetSelections("test", [new Selection(startLine, startColumn, endLine, endColumn)]);

    private static FindStartOptions NoSeedStart(bool updateSearchScope = false, FindStartFocusAction focus = FindStartFocusAction.NoFocusChange) => new(
        ForceRevealReplace: false,
        SeedSearchStringFromSelection: SeedSearchStringFromSelection.None,
        SeedSearchStringFromNonEmptySelection: false,
        SeedSearchStringFromGlobalClipboard: false,
        ShouldFocus: focus,
        ShouldAnimate: false,
        UpdateSearchScope: updateSearchScope,
        Loop: true);

    // Monaco: 'issue #1857: F3, Find Next, acts like "Find Under Cursor"'
    [Fact]
    public void NextMatch_Issue1857_ActsLikeFindUnderCursor() => WithController(["ABC", "ABC", "XYZ", "ABC"], (editor, controller, widget) =>
    {
        FindReplaceState findState = controller.State;

        // I hit Ctrl+F to show the Find dialog
        FindActions.StartFind(controller);

        // I type ABC.
        findState.Change(new NewFindReplaceState { SearchString = "A" }, true);
        findState.Change(new NewFindReplaceState { SearchString = "AB" }, true);
        findState.Change(new NewFindReplaceState { SearchString = "ABC" }, true);

        // The first ABC is highlighted.
        Assert.Equal([1, 1, 1, 4], FromSelection(editor.ViewModel.GetSelection()));

        // I hit Esc to exit the Find dialog.
        controller.CloseFindWidget();
        widget.HasFocus = false;

        Assert.Equal([1, 1, 1, 4], FromSelection(editor.ViewModel.GetSelection()));

        // I hit delete to remove it and change the text to XYZ.
        editor.Model.PushStackElement();
        editor.ViewModel.ExecuteEdits("test", [new IdentifiedEditOperation(new Range(1, 1, 1, 4), null)], _ => null);
        editor.ViewModel.ExecuteEdits("test", [new IdentifiedEditOperation(new Range(1, 1, 1, 1), "XYZ")], _ => null);
        editor.Model.PushStackElement();

        Assert.Equal("XYZ", editor.Model.GetLineContent(1));
        // The cursor is at end of the first line.
        Assert.Equal([1, 4, 1, 4], FromSelection(editor.ViewModel.GetSelection()));

        // I hit F3 to "Find Next" to find the next occurrence of ABC, but instead it searches for XYZ.
        FindActions.NextMatch(controller);

        Assert.Equal("ABC", findState.SearchString);
        Assert.False(widget.HasFocus);
    });

    // Monaco: 'issue #3090: F3 does not loop with two matches on a single line'
    [Fact]
    public void NextMatch_Issue3090_DoesNotLoopWithTwoMatchesOnASingleLine() => WithController(["import nls = require('vs/nls');"], (editor, controller, _) =>
    {
        Select(editor, 1, 9, 1, 9);

        FindActions.NextMatch(controller);
        Assert.Equal([1, 26, 1, 29], FromSelection(editor.ViewModel.GetSelection()));

        FindActions.NextMatch(controller);
        Assert.Equal([1, 8, 1, 11], FromSelection(editor.ViewModel.GetSelection()));
    });

    // Monaco: 'issue #6149: Auto-escape highlighted text for search and replace regex mode'
    [Fact]
    public void NextMatch_Issue6149_AutoEscapesHighlightedTextInRegexMode() => WithController(["var x = (3 * 5)", "var y = (3 * 5)", "var z = (3  * 5)"], (editor, controller, _) =>
    {
        Select(editor, 1, 9, 1, 13);
        controller.ToggleRegex();
        FindActions.StartFind(controller);

        FindActions.NextMatch(controller);
        Assert.Equal([2, 9, 2, 13], FromSelection(editor.ViewModel.GetSelection()));

        FindActions.NextMatch(controller);
        Assert.Equal([1, 9, 1, 13], FromSelection(editor.ViewModel.GetSelection()));
    });

    // Monaco: 'issue #41027: Don't replace find input value on replace action if find input is active'
    [Fact]
    public void StartFindReplace_Issue41027_KeepsFindInputValueIfFindInputIsActive() => WithController(["test"], (_, controller, _) =>
    {
        const string testRegexString = "tes.";
        // Monaco calls toggleRegex() here, but its controller restores isRegex = true from the storage written by the previous
        // test, so the toggle turns it off. Without storage the state starts off: the toggle is dropped to keep the same state.
        controller.SetSearchString(testRegexString);
        controller.Start(NoSeedStart(focus: FindStartFocusAction.FocusFindInput));

        FindActions.NextMatch(controller);
        FindActions.StartFindReplace(controller);

        Assert.Equal(testRegexString, controller.State.SearchString);
    });

    // Monaco: 'editor.find.closeOnResult: closes find widget when a match is found from explicit navigation'
    [Fact]
    public void NextMatch_CloseOnResult_ClosesFindWidgetWhenAMatchIsFound() => WithController(["ABC", "ABC", "XYZ"], (_, controller, _) =>
    {
        FindActions.StartFind(controller);
        Assert.True(controller.State.IsRevealed);
        controller.State.Change(new NewFindReplaceState { SearchString = "ABC" }, true);

        FindActions.NextMatch(controller);

        Assert.False(controller.State.IsRevealed);
    }, new EditorFindOptions(CloseOnResult: true));

    // Monaco: 'editor.find.closeOnResult: keeps find widget open when no match is found'
    [Fact]
    public void NextMatch_CloseOnResult_KeepsFindWidgetOpenWhenNoMatchIsFound() => WithController(["ABC", "DEF", "XYZ"], (_, controller, _) =>
    {
        FindActions.StartFind(controller);
        Assert.True(controller.State.IsRevealed);
        controller.State.Change(new NewFindReplaceState { SearchString = "NO_MATCH" }, true);

        FindActions.NextMatch(controller);

        Assert.Equal(0, controller.State.MatchesCount);
        Assert.True(controller.State.IsRevealed);
    }, new EditorFindOptions(CloseOnResult: true));

    // Monaco: 'editor.find.closeOnResult: disabled keeps find widget open after navigation'
    [Fact]
    public void NextMatch_CloseOnResultDisabled_KeepsFindWidgetOpen() => WithController(["ABC", "ABC", "XYZ"], (_, controller, _) =>
    {
        FindActions.StartFind(controller);
        Assert.True(controller.State.IsRevealed);
        controller.State.Change(new NewFindReplaceState { SearchString = "ABC" }, true);

        FindActions.NextMatch(controller);

        Assert.True(controller.State.IsRevealed);
    }, new EditorFindOptions(CloseOnResult: false));

    // Monaco: 'issue #9043: Clear search scope when find widget is hidden'
    [Fact]
    public void CloseFindWidget_Issue9043_ClearsSearchScope() => WithController(["var x = (3 * 5)", "var y = (3 * 5)", "var z = (3 * 5)"], (_, controller, _) =>
    {
        controller.Start(NoSeedStart());
        Assert.Null(controller.State.SearchScope);

        controller.State.Change(new NewFindReplaceState { SearchScope = [new Range(1, 1, 1, 5)] }, false);
        Assert.Equal([new Range(1, 1, 1, 5)], controller.State.SearchScope);

        controller.CloseFindWidget();
        Assert.Null(controller.State.SearchScope);
    });

    // Monaco: 'issue #18111: Regex replace with single space replaces with no space'
    [Fact]
    public void Replace_Issue18111_RegexReplaceWithSingleSpace() => WithController(["HRESULT OnAmbientPropertyChange(DISPID   dispid);"], (editor, controller, _) =>
    {
        FindActions.StartFind(controller);
        controller.State.Change(new NewFindReplaceState { SearchString = @"\b\s{3}\b", ReplaceString = " ", IsRegex = true }, false);

        controller.MoveToNextMatch();
        Assert.Equal([1, 39, 1, 42], FromSelection(editor.ViewModel.GetSelection()));

        controller.Replace();
        Assert.Equal("HRESULT OnAmbientPropertyChange(DISPID dispid);", editor.Model.GetValue());
    });

    // Monaco: 'issue #24714: Regular expression with ^ in search & replace'
    [Fact]
    public void Replace_Issue24714_RegularExpressionWithCaret() => WithController(["", "line2", "line3"], (editor, controller, _) =>
    {
        FindActions.StartFind(controller);
        controller.State.Change(new NewFindReplaceState { SearchString = "^", ReplaceString = "x", IsRegex = true }, false);

        controller.MoveToNextMatch();
        Assert.Equal([2, 1, 2, 1], FromSelection(editor.ViewModel.GetSelection()));

        controller.Replace();
        Assert.Equal(((char)10) + "xline2" + (char)10 + "line3", editor.Model.GetValue());
    });

    // Monaco: 'issue #38232: Find Next Selection, regex enabled'
    [Fact]
    public void NextSelectionMatch_Issue38232_RegexEnabled() => WithController(["([funny]", "", "([funny]"], (editor, controller, _) =>
    {
        controller.State.Change(new NewFindReplaceState { IsRegex = true }, false);
        Select(editor, 1, 1, 1, 9);

        FindActions.NextSelectionMatch(controller);

        Assert.Equal([3, 1, 3, 9], FromSelection(editor.ViewModel.GetSelection()));
    });

    // Monaco: 'issue #38232: Find Next Selection, regex enabled, find widget open'
    [Fact]
    public void NextSelectionMatch_Issue38232_RegexEnabledFindWidgetOpen() => WithController(["([funny]", "", "([funny]"], (editor, controller, _) =>
    {
        FindActions.StartFind(controller);
        controller.State.Change(new NewFindReplaceState { IsRegex = true }, false);
        Select(editor, 1, 1, 1, 9);

        FindActions.NextSelectionMatch(controller);

        Assert.Equal([3, 1, 3, 9], FromSelection(editor.ViewModel.GetSelection()));
    });

    // Monaco: 'issue #47400, CMD+E supports feeding multiple line of text into the find widget'
    [Fact]
    public void StartFindWithSelection_Issue47400_FeedsMultipleLinesIntoTheFindWidget() => WithController(["ABC", "ABC", "XYZ", "ABC", "ABC"], (editor, controller, _) =>
    {
        // change selection
        Select(editor, 1, 1, 1, 1);

        // cmd+f - open find widget
        FindActions.StartFind(controller);

        Select(editor, 1, 1, 2, 4);
        FindActions.StartFindWithSelection(controller);

        Assert.Equal(["ABC", "ABC"], controller.State.SearchString.Split(["\r\n", "\r", ((char)10).ToString()], StringSplitOptions.None));

        Select(editor, 3, 1, 3, 1);
        FindActions.StartFindWithSelection(controller);
    });

    // Monaco: 'issue #109756, CMD+E with empty cursor should always work'
    [Fact]
    public void StartFindWithSelection_Issue109756_EmptyCursorAlwaysWorks() => WithController(["ABC", "ABC", "XYZ", "ABC", "ABC"], (editor, controller, _) =>
    {
        Select(editor, 1, 2, 1, 2);

        FindActions.StartFindWithSelection(controller);

        Assert.Equal("ABC", controller.State.SearchString);
    });

    // Monaco: 'issue #27083: Update search scope once find widget becomes visible'
    [Fact]
    public void Start_Issue27083_UpdatesSearchScopeOnceFindWidgetBecomesVisible() => WithController(
        ["var x = (3 * 5)", "var y = (3 * 5)", "var z = (3 * 5)"],
        (editor, controller, _) =>
        {
            Select(editor, 1, 1, 2, 1);
            controller.Start(NoSeedStart(updateSearchScope: true));
            Assert.Equal([new Range(1, 1, 2, 1)], controller.State.SearchScope);

            controller.CloseFindWidget();
            editor.ViewModel.SetSelections("test", [new Selection(1, 1, 2, 1), new Selection(2, 1, 2, 5)]);
            controller.Start(NoSeedStart(updateSearchScope: true));
            Assert.Equal([new Range(1, 1, 2, 1), new Range(2, 1, 2, 5)], controller.State.SearchScope);
        },
        new EditorFindOptions(AutoFindInSelection: AutoFindInSelectionMode.Always));

    // Monaco: 'issue #58604: Do not update searchScope if it is empty'
    [Fact]
    public void Start_Issue58604_DoesNotUpdateSearchScopeIfItIsEmpty() => WithController(
        ["var x = (3 * 5)", "var y = (3 * 5)", "var z = (3 * 5)"],
        (editor, controller, _) =>
        {
            Select(editor, 1, 2, 1, 2);

            controller.Start(NoSeedStart(updateSearchScope: true));

            Assert.Null(controller.State.SearchScope);
        },
        new EditorFindOptions(AutoFindInSelection: AutoFindInSelectionMode.Always));

    // Monaco: 'issue #58604: Update searchScope if it is not empty'
    [Fact]
    public void Start_Issue58604_UpdatesSearchScopeIfItIsNotEmpty() => WithController(
        ["var x = (3 * 5)", "var y = (3 * 5)", "var z = (3 * 5)"],
        (editor, controller, _) =>
        {
            Select(editor, 1, 2, 1, 3);

            controller.Start(NoSeedStart(updateSearchScope: true));

            Assert.Equal([new Range(1, 2, 1, 3)], controller.State.SearchScope);
        },
        new EditorFindOptions(AutoFindInSelection: AutoFindInSelectionMode.Always));

    // Monaco: 'issue #27083: Find in selection when multiple lines are selected'
    [Fact]
    public void Start_Issue27083_FindInSelectionWhenMultipleLinesAreSelected() => WithController(
        ["var x = (3 * 5)", "var y = (3 * 5)", "var z = (3 * 5)"],
        (editor, controller, _) =>
        {
            Select(editor, 1, 6, 2, 1);

            controller.Start(NoSeedStart(updateSearchScope: true));

            Assert.Equal([new Range(1, 6, 2, 1)], controller.State.SearchScope);
        },
        new EditorFindOptions(AutoFindInSelection: AutoFindInSelectionMode.Multiline));

    [Fact]
    public void CloseFindWidget_DisposesModelAndClearsDecorations() => WithController(["ABC", "ABC"], (editor, controller, widget) =>
    {
        FindActions.StartFind(controller);
        controller.State.Change(new NewFindReplaceState { SearchString = "ABC" }, false);
        Assert.Contains(editor.Model.GetAllDecorations(), d => d.Options.ClassName == "findMatch");

        controller.CloseFindWidget();

        Assert.DoesNotContain(editor.Model.GetAllDecorations(), d => d.Options.ClassName is "findMatch" or "currentFindMatch");
        Assert.Equal(1, widget.FocusEditorCount);
        Assert.False(controller.MoveToNextMatch());
    });

    [Fact]
    public void SelectAllMatches_FocusesEditor() => WithController(["ABC", "ABC"], (editor, controller, widget) =>
    {
        FindActions.StartFind(controller);
        controller.State.Change(new NewFindReplaceState { SearchString = "ABC" }, false);

        Assert.True(controller.SelectAllMatches());

        Assert.Equal(1, widget.FocusEditorCount);
        Assert.Equal(2, editor.ViewModel.GetSelections().Count);
    });

    [Fact]
    public void StartFindReplace_ReadOnly_DoesNothing() => TestCodeEditor.WithTestCodeEditor(
        "ABC",
        editor =>
        {
            using var controller = new FindController(editor.ViewModel, new ManualEditorScheduler());

            FindActions.StartFindReplace(controller);

            Assert.False(controller.State.IsRevealed);
        },
        new TestCodeEditorOptions(CursorOptions: new LoomEditor.View.Cursors.EditorCursorOptions { ReadOnly = true }));
}
