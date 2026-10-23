// Ported from microsoft/vscode: src/vs/editor/test/browser/controller/cursor.test.ts (suite 'Editor Controller - Cursor', part 2)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Cursors;

public partial class CursorMovementTests
{
    private static readonly EditorWrappingOptions IndentWrap20 = new() { WrappingIndent = WrappingIndent.Indent, WordWrap = WordWrap.WordWrapColumn, WordWrapColumn = 20 };

    private static void AssertDownUpPositions(ViewModel vm, string[] expected)
    {
        vm.SetSelections("test", [new Selection(1, 1, 1, 1)]);
        var positions = new List<string>();
        void Report() => positions.Add(vm.GetCursorStates()[0].ViewState.Position.ToString());

        Report();
        for (int i = 0; i < 4; i++)
        {
            MoveDown(vm);
            Report();
        }

        for (int i = 0; i < 4; i++)
        {
            MoveUp(vm);
            Report();
        }

        Assert.Equal(expected, positions);
    }

    // Monaco: 'issue #144041: Cursor up/down works'
    [Fact]
    public void Issue144041CursorUpDownWorks() => TestCodeEditor.WithTestCodeEditor(
        "Word1 Word2 Word3 Word4\nWord5 Word6 Word7 Word8",
        editor => AssertDownUpPositions(editor.ViewModel, ["(1,1)", "(2,5)", "(3,1)", "(4,5)", "(4,10)", "(3,1)", "(2,5)", "(1,1)", "(1,1)"]),
        new TestCodeEditorOptions(Wrapping: IndentWrap20));

    // Monaco: 'issue #140195: Cursor up/down makes progress'
    [Fact]
    public void Issue140195CursorUpDownMakesProgress() => TestCodeEditor.WithTestCodeEditor(
        "Word1 Word2 Word3 Word4\nWord5 Word6 Word7 Word8",
        editor =>
        {
            editor.Model.ChangeDecorations(1, accessor => accessor.AddDecoration(
                new Range(1, 22, 1, 22),
                ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
                {
                    ShowIfCollapsed = true,
                    Description = "test",
                    After = new InjectedTextOptions { Content = "some very very very very very very very very long text" },
                })));
            AssertDownUpPositions(editor.ViewModel, ["(1,1)", "(2,5)", "(5,19)", "(6,1)", "(7,5)", "(6,1)", "(2,8)", "(1,1)", "(1,1)"]);
        },
        new TestCodeEditorOptions(Wrapping: IndentWrap20, EditorId: 1));

    // --------- move to beginning of line

    // Monaco: 'move to beginning of line'
    [Fact]
    public void MoveToBeginningOfLineTest() => RunTest((_, vm) =>
    {
        MoveToBeginningOfLine(vm);
        AssertCursor(vm, new Position(1, 6));
        MoveToBeginningOfLine(vm);
        AssertCursor(vm, new Position(1, 1));
    });

    // Monaco: 'move to beginning of line from within line'
    [Fact]
    public void MoveToBeginningOfLineFromWithinLine() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 8);
        MoveToBeginningOfLine(vm);
        AssertCursor(vm, new Position(1, 6));
        MoveToBeginningOfLine(vm);
        AssertCursor(vm, new Position(1, 1));
    });

    // Monaco: 'move to beginning of line from whitespace at beginning of line'
    [Fact]
    public void MoveToBeginningOfLineFromWhitespaceAtBeginningOfLine() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 2);
        MoveToBeginningOfLine(vm);
        AssertCursor(vm, new Position(1, 6));
        MoveToBeginningOfLine(vm);
        AssertCursor(vm, new Position(1, 1));
    });

    // Monaco: 'move to beginning of line from within line selection'
    [Fact]
    public void MoveToBeginningOfLineFromWithinLineSelection() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 8);
        MoveToBeginningOfLine(vm, true);
        AssertCursor(vm, new Selection(1, 8, 1, 6));
        MoveToBeginningOfLine(vm, true);
        AssertCursor(vm, new Selection(1, 8, 1, 1));
    });

    // Monaco: 'move to beginning of line with selection multiline forward'
    [Fact]
    public void MoveToBeginningOfLineWithSelectionMultilineForward() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 8);
        MoveTo(vm, 3, 9, true);
        MoveToBeginningOfLine(vm, false);
        AssertCursor(vm, new Selection(3, 5, 3, 5));
    });

    // Monaco: 'move to beginning of line with selection multiline backward'
    [Fact]
    public void MoveToBeginningOfLineWithSelectionMultilineBackward() => RunTest((_, vm) =>
    {
        MoveTo(vm, 3, 9);
        MoveTo(vm, 1, 8, true);
        MoveToBeginningOfLine(vm, false);
        AssertCursor(vm, new Selection(1, 6, 1, 6));
    });

    // Monaco: 'move to beginning of line with selection single line forward'
    [Fact]
    public void MoveToBeginningOfLineWithSelectionSingleLineForward() => RunTest((_, vm) =>
    {
        MoveTo(vm, 3, 2);
        MoveTo(vm, 3, 9, true);
        MoveToBeginningOfLine(vm, false);
        AssertCursor(vm, new Selection(3, 5, 3, 5));
    });

    // Monaco: 'move to beginning of line with selection single line backward'
    [Fact]
    public void MoveToBeginningOfLineWithSelectionSingleLineBackward() => RunTest((_, vm) =>
    {
        MoveTo(vm, 3, 9);
        MoveTo(vm, 3, 2, true);
        MoveToBeginningOfLine(vm, false);
        AssertCursor(vm, new Selection(3, 5, 3, 5));
    });

    // Monaco: 'issue #15401: "End" key is behaving weird when text is selected part 1'
    [Fact]
    public void Issue15401_EndKeyIsBehavingWeirdWhenTextIsSelected_Part1() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 8);
        MoveTo(vm, 3, 9, true);
        MoveToBeginningOfLine(vm, false);
        AssertCursor(vm, new Selection(3, 5, 3, 5));
    });

    // Monaco: 'issue #17011: Shift+home/end now go to the end of the selection start's line, not the selection's end'
    [Fact]
    public void Issue17011_ShiftHomeEndGoToTheEndOfTheSelectionStartLine() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 8);
        MoveTo(vm, 3, 9, true);
        MoveToBeginningOfLine(vm, true);
        AssertCursor(vm, new Selection(1, 8, 3, 5));
    });

    // --------- move to end of line

    // Monaco: 'move to end of line'
    [Fact]
    public void MoveToEndOfLineTest() => RunTest((_, vm) =>
    {
        MoveToEndOfLine(vm);
        AssertCursor(vm, new Position(1, Line1.Length + 1));
        MoveToEndOfLine(vm);
        AssertCursor(vm, new Position(1, Line1.Length + 1));
    });

    // Monaco: 'move to end of line from within line'
    [Fact]
    public void MoveToEndOfLineFromWithinLine() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 6);
        MoveToEndOfLine(vm);
        AssertCursor(vm, new Position(1, Line1.Length + 1));
        MoveToEndOfLine(vm);
        AssertCursor(vm, new Position(1, Line1.Length + 1));
    });

    // Monaco: 'move to end of line from whitespace at end of line'
    [Fact]
    public void MoveToEndOfLineFromWhitespaceAtEndOfLine() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 20);
        MoveToEndOfLine(vm);
        AssertCursor(vm, new Position(1, Line1.Length + 1));
        MoveToEndOfLine(vm);
        AssertCursor(vm, new Position(1, Line1.Length + 1));
    });

    // Monaco: 'move to end of line from within line selection'
    [Fact]
    public void MoveToEndOfLineFromWithinLineSelection() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 6);
        MoveToEndOfLine(vm, true);
        AssertCursor(vm, new Selection(1, 6, 1, Line1.Length + 1));
        MoveToEndOfLine(vm, true);
        AssertCursor(vm, new Selection(1, 6, 1, Line1.Length + 1));
    });

    // Monaco: 'move to end of line with selection multiline forward'
    [Fact]
    public void MoveToEndOfLineWithSelectionMultilineForward() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 1);
        MoveTo(vm, 3, 9, true);
        MoveToEndOfLine(vm, false);
        AssertCursor(vm, new Selection(3, 17, 3, 17));
    });

    // Monaco: 'move to end of line with selection multiline backward'
    [Fact]
    public void MoveToEndOfLineWithSelectionMultilineBackward() => RunTest((_, vm) =>
    {
        MoveTo(vm, 3, 9);
        MoveTo(vm, 1, 1, true);
        MoveToEndOfLine(vm, false);
        AssertCursor(vm, new Selection(1, 21, 1, 21));
    });

    // Monaco: 'move to end of line with selection single line forward'
    [Fact]
    public void MoveToEndOfLineWithSelectionSingleLineForward() => RunTest((_, vm) =>
    {
        MoveTo(vm, 3, 1);
        MoveTo(vm, 3, 9, true);
        MoveToEndOfLine(vm, false);
        AssertCursor(vm, new Selection(3, 17, 3, 17));
    });

    // Monaco: 'move to end of line with selection single line backward'
    [Fact]
    public void MoveToEndOfLineWithSelectionSingleLineBackward() => RunTest((_, vm) =>
    {
        MoveTo(vm, 3, 9);
        MoveTo(vm, 3, 1, true);
        MoveToEndOfLine(vm, false);
        AssertCursor(vm, new Selection(3, 17, 3, 17));
    });

    // Monaco: 'issue #15401: "End" key is behaving weird when text is selected part 2'
    [Fact]
    public void Issue15401_EndKeyIsBehavingWeirdWhenTextIsSelected_Part2() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 1);
        MoveTo(vm, 3, 9, true);
        MoveToEndOfLine(vm, false);
        AssertCursor(vm, new Selection(3, 17, 3, 17));
    });

    // --------- move to beginning of buffer

    // Monaco: 'move to beginning of buffer'
    [Fact]
    public void MoveToBeginningOfBufferTest() => RunTest((_, vm) =>
    {
        MoveToBeginningOfBuffer(vm);
        AssertCursor(vm, new Position(1, 1));
    });

    // Monaco: 'move to beginning of buffer from within first line'
    [Fact]
    public void MoveToBeginningOfBufferFromWithinFirstLine() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 3);
        MoveToBeginningOfBuffer(vm);
        AssertCursor(vm, new Position(1, 1));
    });

    // Monaco: 'move to beginning of buffer from within another line'
    [Fact]
    public void MoveToBeginningOfBufferFromWithinAnotherLine() => RunTest((_, vm) =>
    {
        MoveTo(vm, 3, 3);
        MoveToBeginningOfBuffer(vm);
        AssertCursor(vm, new Position(1, 1));
    });

    // Monaco: 'move to beginning of buffer from within first line selection'
    [Fact]
    public void MoveToBeginningOfBufferFromWithinFirstLineSelection() => RunTest((_, vm) =>
    {
        MoveTo(vm, 1, 3);
        MoveToBeginningOfBuffer(vm, true);
        AssertCursor(vm, new Selection(1, 3, 1, 1));
    });

    // Monaco: 'move to beginning of buffer from within another line selection'
    [Fact]
    public void MoveToBeginningOfBufferFromWithinAnotherLineSelection() => RunTest((_, vm) =>
    {
        MoveTo(vm, 3, 3);
        MoveToBeginningOfBuffer(vm, true);
        AssertCursor(vm, new Selection(3, 3, 1, 1));
    });

    // --------- move to end of buffer

    // Monaco: 'move to end of buffer'
    [Fact]
    public void MoveToEndOfBufferTest() => RunTest((_, vm) =>
    {
        MoveToEndOfBuffer(vm);
        AssertCursor(vm, new Position(5, Line5.Length + 1));
    });

    // Monaco: 'move to end of buffer from within last line'
    [Fact]
    public void MoveToEndOfBufferFromWithinLastLine() => RunTest((_, vm) =>
    {
        MoveTo(vm, 5, 1);
        MoveToEndOfBuffer(vm);
        AssertCursor(vm, new Position(5, Line5.Length + 1));
    });

    // Monaco: 'move to end of buffer from within another line'
    [Fact]
    public void MoveToEndOfBufferFromWithinAnotherLine() => RunTest((_, vm) =>
    {
        MoveTo(vm, 3, 3);
        MoveToEndOfBuffer(vm);
        AssertCursor(vm, new Position(5, Line5.Length + 1));
    });

    // Monaco: 'move to end of buffer from within last line selection'
    [Fact]
    public void MoveToEndOfBufferFromWithinLastLineSelection() => RunTest((_, vm) =>
    {
        MoveTo(vm, 5, 1);
        MoveToEndOfBuffer(vm, true);
        AssertCursor(vm, new Selection(5, 1, 5, Line5.Length + 1));
    });

    // Monaco: 'move to end of buffer from within another line selection'
    [Fact]
    public void MoveToEndOfBufferFromWithinAnotherLineSelection() => RunTest((_, vm) =>
    {
        MoveTo(vm, 3, 3);
        MoveToEndOfBuffer(vm, true);
        AssertCursor(vm, new Selection(3, 3, 5, Line5.Length + 1));
    });

    // --------- misc

    // Monaco: 'select all'
    [Fact]
    public void SelectAll() => RunTest((_, vm) =>
    {
        // Monaco runs CoreNavigationCommands.SelectAll; here the command id is owned by SelectAllFeature, so run its core.
        vm.SetCursorStates(null, CursorChangeReason.Explicit, [CursorMoveCommands.SelectAll(vm, vm.GetPrimaryCursorState())]);
        AssertCursor(vm, new Selection(1, 1, 5, Line5.Length + 1));
    });

    // --------- eventing

    // Monaco: 'no move doesn't trigger event'
    [Fact]
    public void NoMoveDoesNotTriggerEvent() => RunTest((_, vm) =>
    {
        int events = 0;
        void Handler(object? sender, OutgoingViewModelEvent e) => events++;
        vm.EventDispatcher.OnEvent += Handler;
        MoveTo(vm, 1, 1);
        vm.EventDispatcher.OnEvent -= Handler;

        Assert.Equal(0, events);
    });

    // Monaco: 'move eventing'
    [Fact]
    public void MoveEventing() => RunTest((_, vm) =>
    {
        int events = 0;
        void Handler(object? sender, OutgoingViewModelEvent e)
        {
            if (e is CursorStateChangedEvent cursorEvent)
            {
                events++;
                Assert.Equal([new Selection(1, 2, 1, 2)], cursorEvent.Selections);
            }
        }

        vm.EventDispatcher.OnEvent += Handler;
        MoveTo(vm, 1, 2);
        Assert.Equal(1, events);
        vm.EventDispatcher.OnEvent -= Handler;
    });

    // Monaco: 'move in selection mode eventing'
    [Fact]
    public void MoveInSelectionModeEventing() => RunTest((_, vm) =>
    {
        int events = 0;
        void Handler(object? sender, OutgoingViewModelEvent e)
        {
            if (e is CursorStateChangedEvent cursorEvent)
            {
                events++;
                Assert.Equal([new Selection(1, 1, 1, 2)], cursorEvent.Selections);
            }
        }

        vm.EventDispatcher.OnEvent += Handler;
        MoveTo(vm, 1, 2, true);
        Assert.Equal(1, events);
        vm.EventDispatcher.OnEvent -= Handler;
    });

    // --------- state save & restore

    // Monaco: 'saveState & restoreState'
    [Fact]
    public void SaveStateAndRestoreState() => RunTest((_, vm) =>
    {
        MoveTo(vm, 2, 1, true);
        AssertCursor(vm, new Selection(1, 1, 2, 1));

        // Monaco round-trips the state through JSON; the saved state is immutable here, so it is used as it is.
        List<SavedCursorState> savedState = vm.SaveCursorState();

        MoveTo(vm, 1, 1, false);
        AssertCursor(vm, new Position(1, 1));

        vm.RestoreCursorState(savedState);
        AssertCursor(vm, new Selection(1, 1, 2, 1));
    });

    // --------- updating cursor

    // Monaco: 'Independent model edit 1'
    [Fact]
    public void IndependentModelEdit1() => RunTest((editor, vm) =>
    {
        MoveTo(vm, 2, 16, true);

        editor.Model.ApplyEdits([TextEdit.Delete(new Range(2, 1, 2, 2))]);
        AssertCursor(vm, new Selection(1, 1, 2, 15));
    });

    // Monaco: 'grapheme breaking'
    [Fact]
    public void GraphemeBreaking()
    {
        // 'ãããããã' is 'a' + U+0303 six times, '辻󠄀' is U+8FBB + U+E0100, 'பு' is U+0BAA + U+0BC1.
        string[] lines =
        [
            "abcabc",
            "ãããããã",
            "辻\U000E0100辻\U000E0100辻\U000E0100",
            "பு",
        ];

        TestCodeEditor.WithTestCodeEditor(lines, editor =>
        {
            ViewModel vm = editor.ViewModel;

            vm.SetSelections("test", [new Selection(2, 1, 2, 1)]);
            MoveRight(vm);
            AssertCursor(vm, new Position(2, 3));
            MoveLeft(vm);
            AssertCursor(vm, new Position(2, 1));

            vm.SetSelections("test", [new Selection(3, 1, 3, 1)]);
            MoveRight(vm);
            AssertCursor(vm, new Position(3, 4));
            MoveLeft(vm);
            AssertCursor(vm, new Position(3, 1));

            vm.SetSelections("test", [new Selection(4, 1, 4, 1)]);
            MoveRight(vm);
            AssertCursor(vm, new Position(4, 3));
            MoveLeft(vm);
            AssertCursor(vm, new Position(4, 1));

            vm.SetSelections("test", [new Selection(1, 3, 1, 3)]);
            MoveDown(vm);
            AssertCursor(vm, new Position(2, 5));
            MoveDown(vm);
            AssertCursor(vm, new Position(3, 4));
            MoveUp(vm);
            AssertCursor(vm, new Position(2, 5));
            MoveUp(vm);
            AssertCursor(vm, new Position(1, 3));
        });
    }

    // Monaco: 'setSelection / setPosition with source'
    [Fact]
    public void SetSelectionAndSetPositionWithSource() =>
        TestCodeEditor.WithTestCodeEditor("Just text", editor =>
        {
            // Monaco goes through editor.setSelection / setPosition and onDidChangeCursorPosition; the view model
            // raises CursorStateChangedEvent with the same source. The tokenization registration is not needed.
            ViewModel vm = editor.ViewModel;
            string? source = null;
            void Handler(object? sender, OutgoingViewModelEvent e)
            {
                if (e is CursorStateChangedEvent cursorEvent)
                {
                    source = cursorEvent.Source;
                }
            }

            vm.EventDispatcher.OnEvent += Handler;

            vm.SetSelections("navigation", [new Selection(1, 2, 1, 3)]);
            Assert.Equal("navigation", source);

            source = null;
            vm.SetSelections("navigation", [new Selection(1, 2, 1, 2)]);
            Assert.Equal("navigation", source);
            vm.EventDispatcher.OnEvent -= Handler;
        });

    // Monaco: 'issue #23983: Calling model.setEOL does not reset cursor position'
    [Fact]
    public void Issue23983_SetEolDoesNotResetCursorPosition() =>
        TestCodeEditor.WithTestCodeEditor(["first line", "second line"], editor =>
        {
            editor.Model.SetEol(EndOfLineSequence.CRLF);

            editor.ViewModel.SetSelections("test", [new Selection(2, 2, 2, 2)]);
            editor.Model.SetEol(EndOfLineSequence.LF);

            AssertCursor(editor.ViewModel, new Selection(2, 2, 2, 2));
        });

    // Monaco: 'issue #23983: Calling model.setValue() resets cursor position'
    [Fact]
    public void Issue23983_SetValueResetsCursorPosition() =>
        TestCodeEditor.WithTestCodeEditor(["first line", "second line"], editor =>
        {
            editor.Model.SetEol(EndOfLineSequence.CRLF);

            editor.ViewModel.SetSelections("test", [new Selection(2, 2, 2, 2)]);
            editor.Model.SetValue(string.Join('\n', "different first line", "different second line", "new third line"));

            AssertCursor(editor.ViewModel, new Selection(1, 1, 1, 1));
        });

    // Monaco: 'issue #22717: Moving text cursor cause an incorrect position in Chinese'
    [Fact]
    public void Issue22717_MovingTextCursorInChinese() =>
        // Monaco comment: a single model line => 4 view lines (no word wrap here: two model lines, same expectations).
        TestCodeEditor.WithTestCodeEditor(string.Join('\n', "一二三四五六七八九十", "12345678901234567890"), editor =>
        {
            ViewModel vm = editor.ViewModel;
            vm.SetSelections("test", [new Selection(1, 5, 1, 5)]);

            MoveDown(vm);
            AssertCursor(vm, new Selection(2, 9, 2, 9));

            MoveRight(vm);
            AssertCursor(vm, new Selection(2, 10, 2, 10));

            MoveRight(vm);
            AssertCursor(vm, new Selection(2, 11, 2, 11));

            MoveUp(vm);
            AssertCursor(vm, new Selection(1, 6, 1, 6));
        });
}
