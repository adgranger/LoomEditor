using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Tests.TestHelpers;
using static LoomEditor.View.Tests.Cursors.TestHelpers.CursorTestHelpers;

namespace LoomEditor.View.Tests.Commands;

// No Monaco test targets the scroll commands: all tests are new.
public class EditorScrollTests
{
    private static readonly string[] Lines = Enumerable.Range(1, 200).Select(i => "line " + i).ToArray();

    private static void WithEditor(Action<TestCodeEditor> callback) => TestCodeEditor.WithTestCodeEditor(Lines, callback);

    private static int FutureTop(TestCodeEditor editor) => editor.ViewModel.ViewLayout.GetFutureViewport().Top;

    [Fact]
    public void Parse_UnknownTo_ReturnsNull() =>
        Assert.Null(EditorScroll.Parse(new EditorScrollArguments("sideways")));

    [Fact]
    public void Parse_Defaults_AreWrappedLineAndOne()
    {
        EditorScrollParsedArguments? parsed = EditorScroll.Parse(new EditorScrollArguments("down", Value: 0));

        Assert.NotNull(parsed);
        Assert.Equal(EditorScrollDirection.Down, parsed.Direction);
        Assert.Equal(EditorScrollUnit.WrappedLine, parsed.Unit);
        Assert.Equal(1, parsed.Value);
        Assert.False(parsed.RevealCursor);
    }

    [Fact]
    public void ScrollLineDown_ScrollsByOneLineHeightAndDoesNotMoveTheCursor() =>
        WithEditor(editor =>
        {
            MoveTo(editor.ViewModel, 1, 1);
            int lineHeight = editor.ViewModel.CursorConfig.LineHeight;

            CoreNavigationCommands.ScrollLineDown.RunCoreEditorCommand(editor.ViewModel, new BaseCommandArgs());

            Assert.Equal(lineHeight, FutureTop(editor));
            AssertCursor(editor.ViewModel, new Position(1, 1));
        });

    [Fact]
    public void ScrollPageDown_ScrollsByAPage() =>
        WithEditor(editor =>
        {
            CursorConfigurationValues(editor, out int pageSize, out int lineHeight);

            CoreNavigationCommands.ScrollPageDown.RunCoreEditorCommand(editor.ViewModel, new BaseCommandArgs());

            Assert.Equal(pageSize * lineHeight, FutureTop(editor));
        });

    [Fact]
    public void ScrollLineUp_AtTheTop_StaysAtTheTop() =>
        WithEditor(editor =>
        {
            CoreNavigationCommands.ScrollLineUp.RunCoreEditorCommand(editor.ViewModel, new BaseCommandArgs());

            Assert.Equal(0, FutureTop(editor));
        });

    [Fact]
    public void EditorScroll_RevealCursor_MovesTheCursorInsideTheNewViewport() =>
        WithEditor(editor =>
        {
            MoveTo(editor.ViewModel, 1, 1);

            CoreNavigationCommands.EditorScroll.RunCoreEditorCommand(editor.ViewModel, new EditorScrollArguments("down", "page", 1, true));

            Assert.True(editor.ViewModel.GetSelection().Position.LineNumber > 1);
        });

    [Fact]
    public void EditorScroll_IncompatibleUnitAndDirection_DoesNothing() =>
        WithEditor(editor =>
        {
            CoreNavigationCommands.EditorScroll.RunCoreEditorCommand(editor.ViewModel, new EditorScrollArguments("left", "page"));

            Assert.Equal(0, FutureTop(editor));
        });

    private static void CursorConfigurationValues(TestCodeEditor editor, out int pageSize, out int lineHeight)
    {
        pageSize = editor.ViewModel.CursorConfig.PageSize;
        lineHeight = editor.ViewModel.CursorConfig.LineHeight;
    }
}
