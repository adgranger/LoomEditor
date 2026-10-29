using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for the drawing of the caret (it renders HTML): all tests are new.
public class TextViewRendererCaretStyleTests
{
    private const float CharacterWidth = (float)FakeTextMeasurer.CharacterWidth;

    private static RecordingRenderSurface Render(
        ViewHarness harness, TextEditorCursorStyle style, int column, EditorInputMode mode = EditorInputMode.Insert)
    {
        harness.Renderer.CursorStyle = style;
        harness.Renderer.InputMode = mode;
        return harness.Render(Selection.Collapsed(new Position(1, column)));
    }

    private static List<FillRectangleCall> Fills(ViewHarness harness, RecordingRenderSurface surface) =>
        [.. surface.Calls.OfType<FillRectangleCall>().Where(call => call.Color == harness.Renderer.Theme.CursorForeground)];

    [Fact]
    public void Line_Draws2PxWideRect()
    {
        using var harness = new ViewHarness("abcdef");

        FillRectangleCall caret = Assert.Single(Fills(harness, Render(harness, TextEditorCursorStyle.Line, 1)));

        Assert.Equal(2, caret.Width);
        Assert.Equal(FakeTextMeasurer.LineHeight, caret.Height);
    }

    [Fact]
    public void Block_FillsCell_AndRedrawsCharacterInBackgroundColor()
    {
        using var harness = new ViewHarness("abcdef");

        RecordingRenderSurface surface = Render(harness, TextEditorCursorStyle.Block, 3);

        FillRectangleCall caret = Assert.Single(Fills(harness, surface));
        Assert.Equal(harness.LayoutInfo.ContentLeft + (2 * CharacterWidth), caret.X);
        Assert.Equal(CharacterWidth, caret.Width);
        Assert.Equal(FakeTextMeasurer.LineHeight, caret.Height);
        DrawLineLayoutCall glyph = Assert.Single(
            surface.Calls.OfType<DrawLineLayoutCall>(), call => call.Color == harness.Renderer.Theme.Background);
        Assert.Equal("c", glyph.Text);
        Assert.Equal(caret.X, glyph.X);
    }

    [Fact]
    public void Block_AtEndOfLine_FillsTypicalWidthWithoutCharacter()
    {
        using var harness = new ViewHarness("abc");

        RecordingRenderSurface surface = Render(harness, TextEditorCursorStyle.Block, 4);

        FillRectangleCall caret = Assert.Single(Fills(harness, surface));
        Assert.Equal(harness.Measurer.FontInfo.TypicalHalfwidthCharacterWidth, caret.Width);
        Assert.DoesNotContain(surface.Calls.OfType<DrawLineLayoutCall>(), call => call.Color == harness.Renderer.Theme.Background);
    }

    [Fact]
    public void BlockOutline_DrawsRectangleOutline()
    {
        using var harness = new ViewHarness("abcdef");

        RecordingRenderSurface surface = Render(harness, TextEditorCursorStyle.BlockOutline, 2);

        DrawRectangleCall outline = Assert.Single(
            surface.Calls.OfType<DrawRectangleCall>(), call => call.Color == harness.Renderer.Theme.CursorForeground);
        Assert.Equal(1, outline.StrokeWidth);
        Assert.Equal(CharacterWidth - 1, outline.Width);
        Assert.Equal(FakeTextMeasurer.LineHeight - 1, outline.Height);
        Assert.Empty(Fills(harness, surface));
    }

    [Fact]
    public void Underline_Is2PxAtBottom()
    {
        using var harness = new ViewHarness("abcdef");

        FillRectangleCall caret = Assert.Single(Fills(harness, Render(harness, TextEditorCursorStyle.Underline, 2)));

        Assert.Equal(2, caret.Height);
        Assert.Equal(FakeTextMeasurer.LineHeight - 2, caret.Y);
        Assert.Equal(CharacterWidth, caret.Width);
    }

    [Fact]
    public void UnderlineThin_Is1PxAtBottom()
    {
        using var harness = new ViewHarness("abcdef");

        FillRectangleCall caret = Assert.Single(Fills(harness, Render(harness, TextEditorCursorStyle.UnderlineThin, 2)));

        Assert.Equal(1, caret.Height);
        Assert.Equal(FakeTextMeasurer.LineHeight - 1, caret.Y);
    }

    [Fact]
    public void LineThin_Is1PxWide()
    {
        using var harness = new ViewHarness("abcdef");

        FillRectangleCall caret = Assert.Single(Fills(harness, Render(harness, TextEditorCursorStyle.LineThin, 3)));

        Assert.Equal(1, caret.Width);
        Assert.Equal(FakeTextMeasurer.LineHeight, caret.Height);
    }

    [Fact]
    public void OvertypeMode_UsesOvertypeStyle()
    {
        using var harness = new ViewHarness("abcdef");

        // Defaults: Line in insert mode, Block in overtype mode.
        FillRectangleCall insert = Assert.Single(Fills(harness, Render(harness, TextEditorCursorStyle.Line, 2)));
        FillRectangleCall overtype = Assert.Single(
            Fills(harness, Render(harness, TextEditorCursorStyle.Line, 2, EditorInputMode.Overtype)));

        Assert.Equal(2, insert.Width);
        Assert.Equal(CharacterWidth, overtype.Width);
    }

    [Fact]
    public void Tab_BlockCursor_UsesTypicalWidth()
    {
        using var harness = new ViewHarness("\tabc");

        RecordingRenderSurface surface = Render(harness, TextEditorCursorStyle.Block, 1);

        FillRectangleCall caret = Assert.Single(Fills(harness, surface));
        Assert.Equal(harness.Measurer.FontInfo.TypicalHalfwidthCharacterWidth, caret.Width);
        Assert.DoesNotContain(surface.Calls.OfType<DrawLineLayoutCall>(), call => call.Color == harness.Renderer.Theme.Background);
    }
}
