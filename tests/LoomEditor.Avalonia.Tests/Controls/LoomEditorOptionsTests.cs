using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.View.Layout;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;
using EditorWordWrap = global::LoomEditor.Avalonia.Controls.EditorWordWrap;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class LoomEditorOptionsTests
{
    private const int TextArea = 340;

    private static uint[] Render(Editor editor)
    {
        var window = EditorWindow.Open(editor);
        uint[] pixels = EditorWindow.Capture(window);
        window.Close();
        return pixels;
    }

    private static int LastInkColumn(uint[] pixels, int top, int bottom)
    {
        for (int x = TextArea - 1; x >= 0; x--)
        {
            if (EditorWindow.InkCount(pixels, x, top, x + 1, bottom) > 0)
            {
                return x;
            }
        }

        return -1;
    }

    [Fact]
    public async Task LineNumbersEnabled_False_TextStartsAtLeftMargin()
    {
        await HeadlessSession.RunAsync(() =>
        {
            uint[] with = Render(new Editor { Text = "x\nMMMM", MinimapEnabled = false });
            uint[] without = Render(new Editor { Text = "x\nMMMM", MinimapEnabled = false, LineNumbersEnabled = false });

            // The text is the rightmost ink of line 2 (line 1 has the current line highlight); without the line numbers it starts further left.
            Assert.True(LastInkColumn(without, 20, 35) < LastInkColumn(with, 20, 35));
        });
    }

    [Fact]
    public async Task MinimapEnabled_DrawsMinimapPixels()
    {
        await HeadlessSession.RunAsync(() =>
        {
            string text = string.Join('\n', Enumerable.Repeat("MMMMMMMMMMMMMMMM MMMMMMMMMM", 40));

            uint[] on = Render(new Editor { Text = text });
            uint[] off = Render(new Editor { Text = text, MinimapEnabled = false });

            Assert.True(EditorWindow.InkCount(on, 280, 0, 380, 200) > EditorWindow.InkCount(off, 280, 0, 380, 200));
        });
    }

    [Fact]
    public async Task WordWrap_On_LongLineWraps()
    {
        await HeadlessSession.RunAsync(() =>
        {
            string text = string.Concat(Enumerable.Repeat("word ", 100));

            uint[] off = Render(new Editor { Text = text, MinimapEnabled = false });
            uint[] on = Render(new Editor { Text = text, MinimapEnabled = false, WordWrap = EditorWordWrap.On });

            // One line only: the second row has ink once the line wraps.
            Assert.Equal(0, EditorWindow.InkCount(off, 0, 25, TextArea, 60));
            Assert.True(EditorWindow.InkCount(on, 0, 25, TextArea, 60) > 0);
        });
    }

    [Fact]
    public async Task RenderWhitespace_All_DrawsDots()
    {
        await HeadlessSession.RunAsync(() =>
        {
            uint[] none = Render(new Editor { Text = "a     b", MinimapEnabled = false });
            uint[] all = Render(new Editor { Text = "a     b", MinimapEnabled = false, RenderWhitespace = RenderWhitespace.All });

            Assert.True(EditorWindow.InkCount(all, 0, 0, TextArea, 20) > EditorWindow.InkCount(none, 0, 0, TextArea, 20));
        });
    }

    [Fact]
    public async Task StickyScroll_Scrolled_ShowsHeader()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            string text = "class A\n{\n" + string.Join('\n', Enumerable.Range(1, 60).Select(i => "    int field" + i + ";")) + "\n}";
            var editor = new Editor { Text = text, LanguageId = "csharp", MinimapEnabled = false, StickyScrollEnabled = false };
            var window = EditorWindow.Open(editor);
            editor.ScrollTop = 10 * editor.LineHeight;
            uint[] without = EditorWindow.Capture(window);

            editor.StickyScrollEnabled = true;
            uint[] with = without;
            for (int i = 0; i < 30 && with.AsSpan().SequenceEqual(without); i++)
            {
                await Task.Delay(100);
                with = EditorWindow.Capture(window);
            }

            window.Close();
            Assert.False(with.AsSpan().SequenceEqual(without), "the sticky header was never drawn");
        });
    }
}
