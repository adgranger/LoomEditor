// Ported from microsoft/vscode: src/vs/editor/contrib/wordPartOperations/test/browser/wordPartOperations.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features;
using LoomEditor.View.Features.WordPartOperations;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.WordTestUtils;

namespace LoomEditor.View.Tests.Features.WordPartOperations;

public class WordPartOperationsTests
{
    private static readonly DeleteWordPartLeft DeleteWordPartLeftCommand = new();
    private static readonly DeleteWordPartRight DeleteWordPartRightCommand = new();
    private static readonly WordPartLeftCommand CursorWordPartLeft = new(WordPartOperationsCommands.CursorWordPartLeft, false);
    private static readonly WordPartLeftCommand CursorWordPartLeftSelect = new(WordPartOperationsCommands.CursorWordPartLeftSelect, true);
    private static readonly WordPartRightCommand CursorWordPartRight = new(WordPartOperationsCommands.CursorWordPartRight, false);
    private static readonly WordPartRightCommand CursorWordPartRightSelect = new(WordPartOperationsCommands.CursorWordPartRightSelect, true);

    // Monaco: wordSeparators of the 'issue #158667' tests: the default characters sans '$-%~' plus '·'
    private const string NoDashSeparators = "!\"#&'()*+,./:;<=>?@[\\]^`{|}·";

    private static readonly TestCodeEditorOptions NoDashOptions = new(CursorOptions: new EditorCursorOptions { WordSeparators = NoDashSeparators });

    private static void CursorWordPartLeftAction(TestCodeEditor editor, bool inSelectionMode = false) =>
        (inSelectionMode ? CursorWordPartLeftSelect : CursorWordPartLeft).RunEditorCommand(editor.ViewModel);

    private static void CursorWordPartRightAction(TestCodeEditor editor, bool inSelectionMode = false) =>
        (inSelectionMode ? CursorWordPartRightSelect : CursorWordPartRight).RunEditorCommand(editor.ViewModel);

    private static void DeleteWordPartLeftAction(TestCodeEditor editor) => DeleteWordPartLeftCommand.RunEditorCommand(editor.ViewModel);

    private static void DeleteWordPartRightAction(TestCodeEditor editor) => DeleteWordPartRightCommand.RunEditorCommand(editor.ViewModel);

    private static Position GetPosition(TestCodeEditor editor) => editor.ViewModel.GetPosition();

    private static void AssertStops(
        string expected,
        Position initialPosition,
        Action<TestCodeEditor> action,
        Func<TestCodeEditor, bool> stopCondition,
        Func<TestCodeEditor, Position>? record = null,
        TestCodeEditorOptions? options = null)
    {
        (string text, _) = DeserializePipePositions(expected);
        List<Position> actualStops = TestRepeatedActionAndExtractPositions(
            text, initialPosition, action, record ?? GetPosition, stopCondition, options);
        Assert.Equal(expected, SerializePipePositions(text, actualStops));
    }

    private static Func<TestCodeEditor, bool> At(int lineNumber, int column) =>
        ed => GetPosition(ed) == new Position(lineNumber, column);

    // Monaco: 'cursorWordPartLeft - basic'
    [Fact]
    public void CursorWordPartLeft_Basic() => AssertStops(
        string.Join(
            '\n',
            "|start| |line|",
            "|this|Is|A|Camel|Case|Var|  |this_|is_|a_|snake_|case_|var| |THIS_|IS_|CAPS_|SNAKE| |this_|IS|Mixed|Use|",
            "|end| |line"),
        new Position(1000, 1000),
        ed => CursorWordPartLeftAction(ed),
        At(1, 1));

    // Monaco: 'cursorWordPartLeft - issue #53899: whitespace'
    [Fact]
    public void CursorWordPartLeft_Issue53899_Whitespace() => AssertStops(
        "|myvar| |=| |'|demonstration|     |of| |selection| |with| |space|'",
        new Position(1000, 1000),
        ed => CursorWordPartLeftAction(ed),
        At(1, 1));

    // Monaco: 'cursorWordPartLeft - issue #53899: underscores'
    [Fact]
    public void CursorWordPartLeft_Issue53899_Underscores() => AssertStops(
        "|myvar| |=| |'|demonstration_____|of| |selection| |with| |space|'",
        new Position(1000, 1000),
        ed => CursorWordPartLeftAction(ed),
        At(1, 1));

    // Monaco: 'cursorWordPartRight - basic'
    [Fact]
    public void CursorWordPartRight_Basic() => AssertStops(
        string.Join(
            '\n',
            "start| |line|",
            "|this|Is|A|Camel|Case|Var|  |this|_is|_a|_snake|_case|_var| |THIS|_IS|_CAPS|_SNAKE| |this|_IS|Mixed|Use|",
            "|end| |line|"),
        new Position(1, 1),
        ed => CursorWordPartRightAction(ed),
        At(3, 9));

    // Monaco: 'cursorWordPartRight - issue #53899: whitespace'
    [Fact]
    public void CursorWordPartRight_Issue53899_Whitespace() => AssertStops(
        "myvar| |=| |'|demonstration|     |of| |selection| |with| |space|'|",
        new Position(1, 1),
        ed => CursorWordPartRightAction(ed),
        At(1, 52));

    // Monaco: 'cursorWordPartRight - issue #53899: underscores'
    [Fact]
    public void CursorWordPartRight_Issue53899_Underscores() => AssertStops(
        "myvar| |=| |'|demonstration|_____of| |selection| |with| |space|'|",
        new Position(1, 1),
        ed => CursorWordPartRightAction(ed),
        At(1, 52));

    // Monaco: 'cursorWordPartRight - issue #53899: second case'
    [Fact]
    public void CursorWordPartRight_Issue53899_SecondCase() => AssertStops(
        string.Join('\n', ";| |--| |1|", "|;|        |--| |2|", "|;|    |#|3|", "|;|   |#|4|"),
        new Position(1, 1),
        ed => CursorWordPartRightAction(ed),
        At(4, 7));

    // Monaco: 'issue #93239 - cursorWordPartRight'
    [Fact]
    public void Issue93239_CursorWordPartRight() => AssertStops(
        "foo|_bar|",
        new Position(1, 1),
        ed => CursorWordPartRightAction(ed),
        At(1, 8));

    // Monaco: 'issue #93239 - cursorWordPartLeft'
    [Fact]
    public void Issue93239_CursorWordPartLeft() => AssertStops(
        "|foo_|bar",
        new Position(1, 8),
        ed => CursorWordPartLeftAction(ed),
        At(1, 1));

    // Monaco: 'deleteWordPartLeft - basic'
    [Fact]
    public void DeleteWordPartLeft_Basic() => AssertStops(
        "|   |/*| |Just| |some| |text| |a|+=| |3| |+|5|-|3| |*/|  |this|Is|A|Camel|Case|Var|  |this_|is_|a_|snake_|case_|var| |THIS_|IS_|CAPS_|SNAKE| |this_|IS|Mixed|Use",
        new Position(1, 1000),
        DeleteWordPartLeftAction,
        ed => ed.Model.GetValue().Length == 0);

    // Monaco: 'deleteWordPartRight - basic'
    [Fact]
    public void DeleteWordPartRight_Basic()
    {
        const string expected = "   |/*| |Just| |some| |text| |a|+=| |3| |+|5|-|3| |*/|  |this|Is|A|Camel|Case|Var|  |this|_is|_a|_snake|_case|_var| |THIS|_IS|_CAPS|_SNAKE| |this|_IS|Mixed|Use|";
        (string text, _) = DeserializePipePositions(expected);
        AssertStops(
            expected,
            new Position(1, 1),
            DeleteWordPartRightAction,
            ed => ed.Model.GetValue().Length == 0,
            ed => new Position(1, text.Length - ed.Model.GetValue().Length + 1));
    }

    // Monaco: 'issue #158667: cursorWordPartLeft stops at "-" even when "-" is not in word separators'
    [Fact]
    public void Issue158667_CursorWordPartLeftStopsAtDash() => AssertStops(
        "|this-|is-|a-|kebab-|case-|var| |THIS-|IS-|CAPS-|KEBAB| |this-|IS|Mixed|Use",
        new Position(1000, 1000),
        ed => CursorWordPartLeftAction(ed),
        At(1, 1),
        options: NoDashOptions);

    // Monaco: 'issue #158667: cursorWordPartRight stops at "-" even when "-" is not in word separators'
    [Fact]
    public void Issue158667_CursorWordPartRightStopsAtDash() => AssertStops(
        "this|-is|-a|-kebab|-case|-var| |THIS|-IS|-CAPS|-KEBAB| |this|-IS|Mixed|Use|",
        new Position(1, 1),
        ed => CursorWordPartRightAction(ed),
        At(1, 60),
        options: NoDashOptions);

    // Monaco: 'issue #158667: deleteWordPartLeft stops at "-" even when "-" is not in word separators'
    [Fact]
    public void Issue158667_DeleteWordPartLeftStopsAtDash() => AssertStops(
        string.Join(' ', "|this-|is-|a-|kebab-|case-|var| |THIS-|IS-|CAPS-|KEBAB| |this-|IS|Mixed|Use"),
        new Position(1000, 1000),
        DeleteWordPartLeftAction,
        ed => ed.Model.GetValue().Length == 0,
        options: NoDashOptions);

    // Monaco: 'issue #158667: deleteWordPartRight stops at "-" even when "-" is not in word separators'
    [Fact]
    public void Issue158667_DeleteWordPartRightStopsAtDash()
    {
        const string expected = "this|-is|-a|-kebab|-case|-var| |THIS|-IS|-CAPS|-KEBAB| |this|-IS|Mixed|Use|";
        (string text, _) = DeserializePipePositions(expected);
        AssertStops(
            expected,
            new Position(1, 1),
            DeleteWordPartRightAction,
            ed => ed.Model.GetValue().Length == 0,
            ed => new Position(1, text.Length - ed.Model.GetValue().Length + 1),
            NoDashOptions);
    }

    [Fact]
    public void Feature_Attach_RegistersTheWordPartIdsAndTheLegacyAliases()
    {
        var context = new EditorContext();
        using var host = new EditorFeatureHost(context, [() => new WordPartOperationsFeature()]);

        string[] ids =
        [
            WordPartOperationsCommands.DeleteWordPartLeft, WordPartOperationsCommands.DeleteWordPartRight,
            WordPartOperationsCommands.CursorWordPartLeft, WordPartOperationsCommands.CursorWordPartLeftSelect,
            WordPartOperationsCommands.CursorWordPartRight, WordPartOperationsCommands.CursorWordPartRightSelect,
            "cursorWordPartStartLeft", "cursorWordPartStartLeftSelect",
        ];

        Assert.All(ids, id => Assert.True(context.Commands.Contains(id), id));
    }
}
