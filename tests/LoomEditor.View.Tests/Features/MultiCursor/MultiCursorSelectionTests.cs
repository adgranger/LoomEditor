// Ported from microsoft/vscode: src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts (suite 'Multicursor selection' and 'Find state disassociation', without 'Selected text occurrence matching')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Events;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Features.MultiCursor;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Features.MultiCursor.TestHelpers.MultiCursorTestHelpers;

namespace LoomEditor.View.Tests.Features.MultiCursor;

public class MultiCursorSelectionTests
{
    private static void RemoveSecondaryCursors(TestCodeEditor editor) =>
        CoreNavigationCommands.RemoveSecondaryCursors.RunCoreEditorCommand(editor.ViewModel, new BaseCommandArgs("test"));

    private static void AddNext(TestCodeEditor editor, FindController find, MultiCursorSelectionController controller) =>
        controller.AddSelectionToNextFindMatch(find);

    private static Selection[] Sel(TestCodeEditor editor) => [.. editor.ViewModel.GetSelections()];

    private static string FindOptions(FindReplaceState state) => $"{state.MatchCase},{state.WholeWord},{state.IsRegex}";

    private static string StoredFindOptions(FindReplaceState state) => $"{state.ActualMatchCase},{state.ActualWholeWord},{state.ActualIsRegex}";

    // Monaco: 'issue #8817: Cursor position changes when you cancel multicursor'
    [Fact]
    public void SelectHighlights_Issue8817_CancelKeepsThePrimaryCursor() => TestMulticursor(
        ["var x = (3 * 5)", "var y = (3 * 5)", "var z = (3 * 5)"],
        (editor, find, controller, _) =>
        {
            Select(editor, new Selection(2, 9, 2, 16));

            controller.SelectAll(find);
            Assert.Equal([[2, 9, 2, 16], [1, 9, 1, 16], [3, 9, 3, 16]], Selections(editor));

            RemoveSecondaryCursors(editor);

            Assert.Equal([2, 9, 2, 16], FromRange(editor.ViewModel.GetSelection()));
        });

    // Monaco: 'issue #5400: "Select All Occurrences of Find Match" does not select all if find uses regex'
    [Fact]
    public void SelectHighlights_Issue5400_SelectsAllIfFindUsesRegex() => TestMulticursor(
        ["something", "someething", "someeething", "nothing"],
        (editor, find, controller, _) =>
        {
            Select(editor, new Selection(1, 1, 1, 1));
            find.State.Change(new NewFindReplaceState { SearchString = "some+thing", IsRegex = true, IsRevealed = true }, false);

            controller.SelectAll(find);

            Assert.Equal([[1, 1, 1, 10], [2, 1, 2, 11], [3, 1, 3, 12]], Selections(editor));
            Assert.Equal("some+thing", find.State.SearchString);
        },
        hasTextFocus: false);

    // Monaco: 'AddSelectionToNextFindMatchAction can work with multiline'
    [Fact]
    public void AddSelectionToNextFindMatch_CanWorkWithMultiline() => TestMulticursor(
        ["", "qwe", "rty", "", "qwe", "", "rty", "qwe", "rty"],
        (editor, find, controller, _) =>
        {
            Select(editor, new Selection(2, 1, 3, 4));

            controller.AddSelectionToNextFindMatch(find);
            Assert.Equal([[2, 1, 3, 4], [8, 1, 9, 4]], Selections(editor));

            RemoveSecondaryCursors(editor);

            Assert.Equal([2, 1, 3, 4], FromRange(editor.ViewModel.GetSelection()));
        });

    // Monaco: 'issue #6661: AddSelectionToNextFindMatchAction can work with touching ranges'
    [Fact]
    public void AddSelectionToNextFindMatch_Issue6661_CanWorkWithTouchingRanges() => TestMulticursor(
        ["abcabc", "abc", "abcabc"],
        (editor, find, controller, _) =>
        {
            Select(editor, new Selection(1, 1, 1, 4));

            controller.AddSelectionToNextFindMatch(find);
            Assert.Equal([[1, 1, 1, 4], [1, 4, 1, 7]], Selections(editor));

            controller.AddSelectionToNextFindMatch(find);
            controller.AddSelectionToNextFindMatch(find);
            controller.AddSelectionToNextFindMatch(find);
            Assert.Equal([[1, 1, 1, 4], [1, 4, 1, 7], [2, 1, 2, 4], [3, 1, 3, 4], [3, 4, 3, 7]], Selections(editor));

            editor.ViewModel.Type("z", "test");
            Assert.Equal([[1, 2, 1, 2], [1, 3, 1, 3], [2, 2, 2, 2], [3, 2, 3, 2], [3, 3, 3, 3]], Selections(editor));
            Assert.Equal("zz\nz\nzz", editor.Model.GetValue());
        });

    // Monaco: 'issue #23541: Multiline Ctrl+D does not work in CRLF files'
    [Fact]
    public void AddSelectionToNextFindMatch_Issue23541_MultilineWorksInCrlfFiles() => TestMulticursor(
        ["", "qwe", "rty", "", "qwe", "", "rty", "qwe", "rty"],
        (editor, find, controller, _) =>
        {
            editor.Model.SetEol(EndOfLineSequence.CRLF);
            Select(editor, new Selection(2, 1, 3, 4));

            controller.AddSelectionToNextFindMatch(find);
            Assert.Equal([[2, 1, 3, 4], [8, 1, 9, 4]], Selections(editor));

            RemoveSecondaryCursors(editor);

            Assert.Equal([2, 1, 3, 4], FromRange(editor.ViewModel.GetSelection()));
        });

    private static readonly string[] AbcText = ["abc pizza", "abc house", "abc bar"];

    // Monaco: 'AddSelectionToNextFindMatchAction starting with single collapsed selection'
    [Fact]
    public void AddSelectionToNextFindMatch_StartingWithSingleCollapsedSelection() => TestMulticursor(AbcText, (editor, find, c, _) =>
    {
        Select(editor, new Selection(1, 2, 1, 2));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4), new Selection(3, 1, 3, 4)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4), new Selection(3, 1, 3, 4)], Sel(editor));
    });

    // Monaco: 'AddSelectionToNextFindMatchAction starting with two selections, one being collapsed 1)'
    [Fact]
    public void AddSelectionToNextFindMatch_StartingWithTwoSelectionsOneBeingCollapsed1() => TestMulticursor(AbcText, (editor, find, c, _) =>
    {
        Select(editor, new Selection(1, 1, 1, 4), new Selection(2, 2, 2, 2));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4), new Selection(3, 1, 3, 4)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4), new Selection(3, 1, 3, 4)], Sel(editor));
    });

    // Monaco: 'AddSelectionToNextFindMatchAction starting with two selections, one being collapsed 2)'
    [Fact]
    public void AddSelectionToNextFindMatch_StartingWithTwoSelectionsOneBeingCollapsed2() => TestMulticursor(AbcText, (editor, find, c, _) =>
    {
        Select(editor, new Selection(1, 2, 1, 2), new Selection(2, 1, 2, 4));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4), new Selection(3, 1, 3, 4)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4), new Selection(3, 1, 3, 4)], Sel(editor));
    });

    // Monaco: 'AddSelectionToNextFindMatchAction starting with all collapsed selections'
    [Fact]
    public void AddSelectionToNextFindMatch_StartingWithAllCollapsedSelections() => TestMulticursor(AbcText, (editor, find, c, _) =>
    {
        Select(editor, new Selection(1, 2, 1, 2), new Selection(2, 2, 2, 2), new Selection(3, 1, 3, 1));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4), new Selection(3, 1, 3, 4)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4), new Selection(3, 1, 3, 4)], Sel(editor));
    });

    // Monaco: 'AddSelectionToNextFindMatchAction starting with all collapsed selections on different words'
    [Fact]
    public void AddSelectionToNextFindMatch_StartingWithAllCollapsedSelectionsOnDifferentWords() => TestMulticursor(AbcText, (editor, find, c, _) =>
    {
        Select(editor, new Selection(1, 6, 1, 6), new Selection(2, 6, 2, 6), new Selection(3, 6, 3, 6));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 5, 1, 10), new Selection(2, 5, 2, 10), new Selection(3, 5, 3, 8)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 5, 1, 10), new Selection(2, 5, 2, 10), new Selection(3, 5, 3, 8)], Sel(editor));
    });

    // Monaco: 'issue #20651: AddSelectionToNextFindMatchAction case insensitive'
    [Fact]
    public void AddSelectionToNextFindMatch_Issue20651_CaseInsensitive() => TestMulticursor(["test", "testte", "Test", "testte", "test"], (editor, find, c, _) =>
    {
        Select(editor, new Selection(1, 1, 1, 5));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 5), new Selection(2, 1, 2, 5)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 5), new Selection(2, 1, 2, 5), new Selection(3, 1, 3, 5)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 5), new Selection(2, 1, 2, 5), new Selection(3, 1, 3, 5), new Selection(4, 1, 4, 5)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 5), new Selection(2, 1, 2, 5), new Selection(3, 1, 3, 5), new Selection(4, 1, 4, 5), new Selection(5, 1, 5, 5)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 5), new Selection(2, 1, 2, 5), new Selection(3, 1, 3, 5), new Selection(4, 1, 4, 5), new Selection(5, 1, 5, 5)], Sel(editor));
    });

    // Suite 'Find state disassociation'
    private static readonly string[] AppText = ["app", "apples", "whatsapp", "app", "App", " app"];

    private enum EndKind
    {
        Selection,
        Blur,
        Dispose,
        Configuration,
    }

    // Monaco: 'caret-started sessions temporarily override and restore Find options in every matching mode'
    [Fact]
    public void CaretStartedSessions_TemporarilyOverrideAndRestoreFindOptionsInEveryMatchingMode()
    {
        foreach (SelectedTextMatchMode mode in Enum.GetValues<SelectedTextMatchMode>())
        {
            foreach (EndKind endSession in Enum.GetValues<EndKind>())
            {
                TestMulticursor(AppText, (editor, find, c, widget) =>
                {
                    FindReplaceState state = find.State;
                    state.Change(new NewFindReplaceState { MatchCase = false, WholeWord = false, IsRegex = true }, false);
                    Select(editor, new Selection(1, 2, 1, 2));
                    c.AddSelectionToNextFindMatch(find);
                    c.AddSelectionToNextFindMatch(find);

                    string duringSession = $"{state.SearchString}|{FindOptions(state)}|{StoredFindOptions(state)}|{widget.HighlightFindOptionsCount}";
                    int[][] selectionsDuringSession = Selections(editor);

                    switch (endSession)
                    {
                        case EndKind.Selection:
                            Select(editor, new Selection(2, 1, 2, 4));
                            break;
                        case EndKind.Blur:
                            SetTextFocus(editor, false);
                            break;
                        case EndKind.Dispose:
                            c.Dispose();
                            break;
                        case EndKind.Configuration:
                            c.SetOptions(new MultiCursorOptions(c.Options.SelectedTextMatchMode == SelectedTextMatchMode.FindOptions ? SelectedTextMatchMode.CaseInsensitive : SelectedTextMatchMode.FindOptions));
                            break;
                    }

                    string message = $"{mode}: {endSession}";
                    Assert.Equal("app|True,True,False|False,False,True|1", duringSession);
                    Assert.True(new int[][] { [1, 1, 1, 4], [4, 1, 4, 4] }.Zip(selectionsDuringSession).All(p => p.First.SequenceEqual(p.Second)) && selectionsDuringSession.Length == 2, message);
                    Assert.Equal("False,False,True", FindOptions(state));
                    Assert.Equal("False,False,True", StoredFindOptions(state));
                }, matchMode: mode);
            }
        }
    }

    // Monaco: 'changing Find options ends a default caret-started session without reverting user preferences'
    [Fact]
    public void ChangingFindOptions_EndsADefaultCaretStartedSessionWithoutRevertingUserPreferences() => TestMulticursor(AppText, (editor, find, c, _) =>
    {
        FindReplaceState state = find.State;
        state.Change(new NewFindReplaceState { MatchCase = false, WholeWord = false, IsRegex = true }, false);
        Select(editor, new Selection(1, 2, 1, 2));
        c.AddSelectionToNextFindMatch(find);

        state.Change(new NewFindReplaceState { MatchCase = false, WholeWord = true }, false);
        c.AddSelectionToNextFindMatch(find);
        c.AddSelectionToNextFindMatch(find);

        Assert.Equal([[1, 1, 1, 4], [4, 1, 4, 4], [5, 1, 5, 4]], Selections(editor));
        Assert.Equal("False,True,True", FindOptions(state));
        Assert.Equal("False,True,True", StoredFindOptions(state));
    });

    // Monaco: 'enters mode'
    [Fact]
    public void EntersMode() => TestMulticursor(AppText, (editor, find, c, _) =>
    {
        Select(editor, new Selection(1, 2, 1, 2));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(4, 1, 4, 4)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(4, 1, 4, 4), new Selection(6, 2, 6, 5)], Sel(editor));
    });

    // Monaco: 'leaves mode when selection changes'
    [Fact]
    public void LeavesModeWhenSelectionChanges() => TestMulticursor(AppText, (editor, find, c, _) =>
    {
        Select(editor, new Selection(1, 2, 1, 2));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4)], Sel(editor));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(4, 1, 4, 4)], Sel(editor));

        // change selection
        Select(editor, new Selection(1, 1, 1, 4));

        c.AddSelectionToNextFindMatch(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4)], Sel(editor));
    });

    // Monaco: 'Select Highlights respects mode ' (the trailing space is in Monaco's name)
    [Fact]
    public void SelectHighlights_RespectsMode() => TestMulticursor(AppText, (editor, find, c, _) =>
    {
        Select(editor, new Selection(1, 2, 1, 2));

        c.SelectAll(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(4, 1, 4, 4), new Selection(6, 2, 6, 5)], Sel(editor));

        c.SelectAll(find);
        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(4, 1, 4, 4), new Selection(6, 2, 6, 5)], Sel(editor));
    });

    [Fact]
    public void SetHasFocus_EmitsFocusChangedEvent() => TestCodeEditor.WithTestCodeEditor(["abc"], editor =>
    {
        List<FocusChangedEvent> events = [];
        editor.ViewModel.EventDispatcher.OnEvent += (_, e) =>
        {
            if (e is FocusChangedEvent focus)
            {
                events.Add(focus);
            }
        };

        editor.ViewModel.SetHasFocus(false);
        editor.ViewModel.SetHasFocus(true);

        Assert.Equal([(true, false), (false, true)], events.Select(e => (e.OldHasFocus, e.HasFocus)));
    });

    [Fact]
    public void Attach_WithoutFindFeature_RegistersOnlyThePhase3Commands()
    {
        var context = new EditorContext();
        using var host = new EditorFeatureHost(context, [() => new MultiCursorFeature()]);

        Assert.True(context.Commands.Contains(MultiCursorCommands.InsertCursorAbove));
        Assert.False(context.Commands.Contains(MultiCursorCommands.AddSelectionToNextFindMatch));
        Assert.False(context.Commands.Contains(MultiCursorCommands.SelectHighlights));
    }

    [Fact]
    public void AddSelectionToNextFindMatch_ThroughCommandRegistry_AddsTheNextOccurrence()
    {
        var context = new EditorContext();
        context.SetText("abc\nabc");
        using var host = new EditorFeatureHost(context, [() => new FindFeature(), () => new MultiCursorFeature()]);
        context.Selection = new Selection(1, 1, 1, 4);

        Assert.True(context.Commands.Execute(MultiCursorCommands.AddSelectionToNextFindMatch));

        Assert.Equal([new Selection(1, 1, 1, 4), new Selection(2, 1, 2, 4)], context.Selections);
    }

    [Fact]
    public void CtrlD_Keybinding_ResolvesToAddSelectionToNextFindMatch()
    {
        var focused = new KeybindingContext(TextInputFocus: true, EditorTextFocus: true, Writable: true, TabDoesNotMoveFocus: true, EditorFocus: true);

        Assert.Equal(
            MultiCursorCommands.AddSelectionToNextFindMatch,
            KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyD), focused));
        Assert.Equal(
            MultiCursorCommands.SelectHighlights,
            KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.Ctrl | KeyModifiers.Shift, KeyCode.KeyL), focused));
        Assert.Equal(
            MultiCursorCommands.ChangeAll,
            KeybindingResolver.Default.Resolve(new Keybinding(KeyModifiers.Ctrl, KeyCode.F2), focused));
    }

    [Fact]
    public void CtrlK_CtrlD_Chord_ResolvesToMoveSelectionToNextFindMatch()
    {
        var focused = new KeybindingContext(TextInputFocus: true, EditorTextFocus: true, Writable: true, TabDoesNotMoveFocus: true, EditorFocus: true);
        var dispatcher = new KeybindingDispatcher(KeybindingResolver.Default);

        dispatcher.Dispatch(new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyK), focused, out _);
        Assert.True(dispatcher.Dispatch(new Keybinding(KeyModifiers.Ctrl, KeyCode.KeyD), focused, out string? commandId));

        Assert.Equal(MultiCursorCommands.MoveSelectionToNextFindMatch, commandId);
    }
}
