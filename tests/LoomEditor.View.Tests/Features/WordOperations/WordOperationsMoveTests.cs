// Ported from microsoft/vscode: src/vs/editor/contrib/wordOperations/test/browser/wordOperations.test.ts (the move tests)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features;
using LoomEditor.View.Features.WordOperations;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.WordTestUtils;

namespace LoomEditor.View.Tests.Features.WordOperations;

// Not ported: 'cursorWordLeft - Recognize words' and 'cursorWordRight - Recognize words' (they need Intl.Segmenter, not available),
// and 'issue #51275 - cursorWordStartLeft does not push undo/redo stack element' (it types text: ported with typing in P3-33).
// The delete tests are ported in P3-30.
public class WordOperationsMoveTests
{
    private static readonly WordLeftCommand CursorWordStartLeft = new(WordOperationsCommands.CursorWordStartLeft, false, WordNavigationType.WordStart);
    private static readonly WordLeftCommand CursorWordEndLeft = new(WordOperationsCommands.CursorWordEndLeft, false, WordNavigationType.WordEnd);
    private static readonly WordLeftCommand CursorWordLeft = new(WordOperationsCommands.CursorWordLeft, false, WordNavigationType.WordStartFast);
    private static readonly WordLeftCommand CursorWordStartLeftSelect = new(WordOperationsCommands.CursorWordStartLeftSelect, true, WordNavigationType.WordStart);
    private static readonly WordLeftCommand CursorWordEndLeftSelect = new(WordOperationsCommands.CursorWordEndLeftSelect, true, WordNavigationType.WordEnd);
    private static readonly WordLeftCommand CursorWordLeftSelect = new(WordOperationsCommands.CursorWordLeftSelect, true, WordNavigationType.WordStartFast);
    private static readonly WordRightCommand CursorWordStartRight = new(WordOperationsCommands.CursorWordStartRight, false, WordNavigationType.WordStart);
    private static readonly WordRightCommand CursorWordEndRight = new(WordOperationsCommands.CursorWordEndRight, false, WordNavigationType.WordEnd);
    private static readonly WordRightCommand CursorWordRight = new(WordOperationsCommands.CursorWordRight, false, WordNavigationType.WordEnd);
    private static readonly WordRightCommand CursorWordStartRightSelect = new(WordOperationsCommands.CursorWordStartRightSelect, true, WordNavigationType.WordStart);
    private static readonly WordRightCommand CursorWordEndRightSelect = new(WordOperationsCommands.CursorWordEndRightSelect, true, WordNavigationType.WordEnd);
    private static readonly WordRightCommand CursorWordRightSelect = new(WordOperationsCommands.CursorWordRightSelect, true, WordNavigationType.WordEnd);
    private static readonly WordAccessibilityLeftCommand CursorWordAccessibilityLeft = new(WordOperationsCommands.CursorWordAccessibilityLeft, false);
    private static readonly WordAccessibilityLeftCommand CursorWordAccessibilityLeftSelect = new(WordOperationsCommands.CursorWordAccessibilityLeftSelect, true);
    private static readonly WordAccessibilityRightCommand CursorWordAccessibilityRight = new(WordOperationsCommands.CursorWordAccessibilityRight, false);
    private static readonly WordAccessibilityRightCommand CursorWordAccessibilityRightSelect = new(WordOperationsCommands.CursorWordAccessibilityRightSelect, true);

    private static void RunEditorCommand(TestCodeEditor editor, MoveWordCommand command) => command.RunEditorCommand(editor.ViewModel);

    private static void CursorWordLeftAction(TestCodeEditor editor, bool inSelectionMode = false) =>
        RunEditorCommand(editor, inSelectionMode ? CursorWordLeftSelect : CursorWordLeft);

    // Monaco: the two accessibility helpers pick the select variant when inSelectionMode is false (kept as is).
    private static void CursorWordAccessibilityLeftAction(TestCodeEditor editor, bool inSelectionMode = false) =>
        RunEditorCommand(editor, inSelectionMode ? CursorWordAccessibilityLeft : CursorWordAccessibilityLeftSelect);

    private static void CursorWordAccessibilityRightAction(TestCodeEditor editor, bool inSelectionMode = false) =>
        RunEditorCommand(editor, inSelectionMode ? CursorWordAccessibilityRightSelect : CursorWordAccessibilityRight);

    private static void CursorWordStartLeftAction(TestCodeEditor editor, bool inSelectionMode = false) =>
        RunEditorCommand(editor, inSelectionMode ? CursorWordStartLeftSelect : CursorWordStartLeft);

    private static void CursorWordEndLeftAction(TestCodeEditor editor, bool inSelectionMode = false) =>
        RunEditorCommand(editor, inSelectionMode ? CursorWordEndLeftSelect : CursorWordEndLeft);

    private static void CursorWordRightAction(TestCodeEditor editor, bool inSelectionMode = false) =>
        RunEditorCommand(editor, inSelectionMode ? CursorWordRightSelect : CursorWordRight);

    private static void MoveWordEndRight(TestCodeEditor editor, bool inSelectionMode = false) =>
        RunEditorCommand(editor, inSelectionMode ? CursorWordEndRightSelect : CursorWordEndRight);

    private static void MoveWordStartRight(TestCodeEditor editor, bool inSelectionMode = false) =>
        RunEditorCommand(editor, inSelectionMode ? CursorWordStartRightSelect : CursorWordStartRight);

    private static Position GetPosition(TestCodeEditor editor) => editor.ViewModel.GetPosition();

    // The body shared by the 'EXPECTED' tests: run the action until the stop position and compare the pipe text.
    private static void AssertStops(
        string expected,
        Position initialPosition,
        Action<TestCodeEditor> action,
        Position stopAt,
        TestCodeEditorOptions? options = null)
    {
        (string text, _) = DeserializePipePositions(expected);
        List<Position> actualStops = TestRepeatedActionAndExtractPositions(
            text,
            initialPosition,
            action,
            GetPosition,
            ed => GetPosition(ed) == stopAt,
            options);
        string actual = SerializePipePositions(text, actualStops);
        Assert.Equal(expected, actual);
    }

    // Monaco: 'cursorWordLeft - simple'
    [Fact]
    public void CursorWordLeft_Simple() => AssertStops(
        string.Join('\n', "|    \t|My |First |Line\t ", "|\t|My |Second |Line", "|    |Third |Line🐶", "|", "|1"),
        new Position(1000, 1000),
        ed => CursorWordLeftAction(ed),
        new Position(1, 1));

    // Monaco: 'cursorWordLeft - with selection'
    [Fact]
    public void CursorWordLeft_WithSelection() =>
        TestCodeEditor.WithTestCodeEditor(
            ["    \tMy First Line\t ", "\tMy Second Line", "    Third Line🐶", string.Empty, "1"],
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(5, 2, 5, 2)]);
                CursorWordLeftAction(editor, true);
                Assert.Equal(new Selection(5, 2, 5, 1), editor.ViewModel.GetSelection());
            });

    // Monaco: 'cursorWordLeft - issue #832'
    [Fact]
    public void CursorWordLeft_Issue832() => AssertStops(
        "|   |/* |Just |some   |more   |text |a|+= |3 |+|5-|3 |+ |7 |*/  ",
        new Position(1000, 1000),
        ed => CursorWordLeftAction(ed),
        new Position(1, 1));

    // Monaco: 'cursorWordLeft - issue #48046: Word selection doesn't work as usual'
    [Fact]
    public void CursorWordLeft_Issue48046_WordSelectionDoesNotWorkAsUsual() => AssertStops(
        "|deep.|object.|property",
        new Position(1, 21),
        ed => CursorWordLeftAction(ed),
        new Position(1, 1));

    // Monaco: 'cursorWordLeft - Does not recognize words'
    [Fact]
    public void CursorWordLeft_DoesNotRecognizeWords() => AssertStops(
        "|/* |これはテストです |/*",
        new Position(1000, 1000),
        ed => CursorWordLeftAction(ed, true),
        new Position(1, 1),
        new TestCodeEditorOptions(CursorOptions: new EditorCursorOptions { WordSegmenterLocales = [] }));

    // Monaco: 'cursorWordLeft - issue #169904: cursors out of sync'
    [Fact]
    public void CursorWordLeft_Issue169904_CursorsOutOfSync()
    {
        string[] text =
        [
            ".grid1 {",
            "  display: grid;",
            "  grid-template-columns:",
            "    [full-start] minmax(1em, 1fr)",
            "    [main-start] minmax(0, 40em) [main-end]",
            "    minmax(1em, 1fr) [full-end];",
            "}",
            ".grid2 {",
            "  display: grid;",
            "  grid-template-columns:",
            "    [full-start] minmax(1em, 1fr)",
            "    [main-start] minmax(0, 40em) [main-end] minmax(1em, 1fr) [full-end];",
            "}",
        ];
        TestCodeEditor.WithTestCodeEditor(text, editor =>
        {
            editor.ViewModel.SetSelections(
                "test",
                [new Selection(5, 44, 5, 44), new Selection(6, 32, 6, 32), new Selection(12, 44, 12, 44), new Selection(12, 72, 12, 72)]);
            CursorWordLeftAction(editor, false);
            Assert.Equal(
                [new Selection(5, 43, 5, 43), new Selection(6, 31, 6, 31), new Selection(12, 43, 12, 43), new Selection(12, 71, 12, 71)],
                editor.ViewModel.GetSelections());
        });
    }

    // Monaco: 'cursorWordLeftSelect - issue #74369: cursorWordLeft and cursorWordLeftSelect do not behave consistently'
    [Fact]
    public void CursorWordLeftSelect_Issue74369() => AssertStops(
        "|this.|is.|a.|test",
        new Position(1, 15),
        ed => CursorWordLeftAction(ed, true),
        new Position(1, 1));

    // Monaco: 'cursorWordStartLeft'
    [Fact]
    public void CursorWordStartLeft_MatchesVisualStudio() => AssertStops(
        "|   |/* |Just |some   |more   |text |a|+= |3 |+|5|-|3 |+ |7 |*/  ",
        new Position(1000, 1000),
        ed => CursorWordStartLeftAction(ed),
        new Position(1, 1));

    // Monaco: 'cursorWordStartLeft - issue #51119: regression makes VS compatibility impossible'
    [Fact]
    public void CursorWordStartLeft_Issue51119() => AssertStops(
        "|this|.|is|.|a|.|test",
        new Position(1000, 1000),
        ed => CursorWordStartLeftAction(ed),
        new Position(1, 1));

    // Monaco: 'cursorWordEndLeft'
    [Fact]
    public void CursorWordEndLeft_Test() => AssertStops(
        "|   /*| Just| some|   more|   text| a|+=| 3| +|5|-|3| +| 7| */|  ",
        new Position(1000, 1000),
        ed => CursorWordEndLeftAction(ed),
        new Position(1, 1));

    // Monaco: 'cursorWordRight - simple'
    [Fact]
    public void CursorWordRight_Simple() => AssertStops(
        string.Join('\n', "    \tMy| First| Line|\t |", "\tMy| Second| Line|", "    Third| Line🐶|", "|", "1|"),
        new Position(1, 1),
        ed => CursorWordRightAction(ed),
        new Position(5, 2));

    // Monaco: 'cursorWordRight - selection'
    [Fact]
    public void CursorWordRight_Selection() =>
        TestCodeEditor.WithTestCodeEditor(
            ["    \tMy First Line\t ", "\tMy Second Line", "    Third Line🐶", string.Empty, "1"],
            editor =>
            {
                editor.ViewModel.SetSelections("test", [new Selection(1, 1, 1, 1)]);
                CursorWordRightAction(editor, true);
                Assert.Equal(new Selection(1, 1, 1, 8), editor.ViewModel.GetSelection());
            });

    // Monaco: 'cursorWordRight - issue #832'
    [Fact]
    public void CursorWordRight_Issue832() => AssertStops(
        "   /*| Just| some|   more|   text| a|+=| 3| +5|-3| +| 7| */|  |",
        new Position(1, 1),
        ed => CursorWordRightAction(ed),
        new Position(1, 50));

    // Monaco: 'cursorWordRight - issue #41199'
    [Fact]
    public void CursorWordRight_Issue41199() => AssertStops(
        "console|.log|(err|)|",
        new Position(1, 1),
        ed => CursorWordRightAction(ed),
        new Position(1, 17));

    // Monaco: 'cursorWordRight - Does not recognize words'
    [Fact]
    public void CursorWordRight_DoesNotRecognizeWords() => AssertStops(
        "/*| これはテストです|/*|",
        new Position(1, 1),
        ed => CursorWordRightAction(ed),
        new Position(1, 14),
        new TestCodeEditorOptions(CursorOptions: new EditorCursorOptions { WordSegmenterLocales = [] }));

    // Monaco: 'moveWordEndRight'
    [Fact]
    public void MoveWordEndRight_Test() => AssertStops(
        "   /*| Just| some|   more|   text| a|+=| 3| +5|-3| +| 7| */|  |",
        new Position(1, 1),
        ed => MoveWordEndRight(ed),
        new Position(1, 50));

    // Monaco: 'moveWordStartRight'
    [Fact]
    public void MoveWordStartRight_MatchesVisualStudio() => AssertStops(
        "   |/* |Just |some   |more   |text |a|+= |3 |+|5|-|3 |+ |7 |*/  |",
        new Position(1, 1),
        ed => MoveWordStartRight(ed),
        new Position(1, 50));

    // Monaco: 'issue #51119: cursorWordStartRight regression makes VS compatibility impossible'
    [Fact]
    public void Issue51119_CursorWordStartRight() => AssertStops(
        "this|.|is|.|a|.|test|",
        new Position(1, 1),
        ed => MoveWordStartRight(ed),
        new Position(1, 15));

    // Monaco: 'issue #64810: cursorWordStartRight skips first word after newline'
    [Fact]
    public void Issue64810_CursorWordStartRightSkipsFirstWordAfterNewline() => AssertStops(
        string.Join('\n', "Hello |World|", "|Hei |mailman|"),
        new Position(1, 1),
        ed => MoveWordStartRight(ed),
        new Position(2, 12));

    // Monaco: 'cursorWordAccessibilityLeft'
    [Fact]
    public void CursorWordAccessibilityLeft_Test() => AssertStops(
        "|   /* |Just |some   |more   |text |a+= |3 +|5-|3 + |7 */  ",
        new Position(1000, 1000),
        ed => CursorWordAccessibilityLeftAction(ed),
        new Position(1, 1));

    // Monaco: 'cursorWordAccessibilityRight'
    [Fact]
    public void CursorWordAccessibilityRight_Test() => AssertStops(
        "   /* |Just |some   |more   |text |a+= |3 +|5-|3 + |7 */  |",
        new Position(1, 1),
        ed => CursorWordAccessibilityRightAction(ed),
        new Position(1, 50));

    [Fact]
    public void Feature_Attach_RegistersTheSixteenMoveIds()
    {
        var context = new EditorContext();
        using var host = new EditorFeatureHost(context, [() => new WordOperationsFeature()]);

        string[] ids =
        [
            WordOperationsCommands.CursorWordStartLeft, WordOperationsCommands.CursorWordEndLeft, WordOperationsCommands.CursorWordLeft,
            WordOperationsCommands.CursorWordStartLeftSelect, WordOperationsCommands.CursorWordEndLeftSelect, WordOperationsCommands.CursorWordLeftSelect,
            WordOperationsCommands.CursorWordAccessibilityLeft, WordOperationsCommands.CursorWordAccessibilityLeftSelect,
            WordOperationsCommands.CursorWordStartRight, WordOperationsCommands.CursorWordEndRight, WordOperationsCommands.CursorWordRight,
            WordOperationsCommands.CursorWordStartRightSelect, WordOperationsCommands.CursorWordEndRightSelect, WordOperationsCommands.CursorWordRightSelect,
            WordOperationsCommands.CursorWordAccessibilityRight, WordOperationsCommands.CursorWordAccessibilityRightSelect,
        ];

        Assert.All(ids, id => Assert.True(context.Commands.Contains(id), id));
    }

    [Fact]
    public void Execute_CursorWordLeft_MovesTheCursor()
    {
        var context = new EditorContext();
        using var host = new EditorFeatureHost(context, [() => new WordOperationsFeature()]);
        context.SetText("hello world");
        context.Selection = new Selection(1, 12, 1, 12);

        bool ran = context.Commands.Execute(WordOperationsCommands.CursorWordLeft);

        Assert.True(ran);
        Assert.Equal(new Position(1, 7), context.Selection.Position);
    }
}
