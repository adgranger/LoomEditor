using Avalonia.Controls;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Themes;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class LoomEditorRenderTests
{
    private static uint Background => ToArgb(EditorTheme.Dark.Background);

    private static uint ToArgb(ThemeColor color) =>
        ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;

    [Fact]
    public async Task LoomEditor_InWindow_DrawsText()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = "hello" };
            Window window = EditorWindow.Open(editor);

            uint[] pixels = EditorWindow.Capture(window);

            int lineHeight = (int)editor.LineHeight;
            Assert.True(lineHeight > 0);
            bool inkInFirstLine = false;
            for (int y = 0; y < lineHeight && !inkInFirstLine; y++)
            {
                for (int x = 0; x < 200; x++)
                {
                    inkInFirstLine |= pixels[(y * 400) + x] != Background;
                }
            }

            Assert.True(inkInFirstLine, "no text pixel in the first line box");
            window.Close();
        });
    }

    [Fact]
    public async Task Text_Changed_Redraws()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = "hello" };
            Window window = EditorWindow.Open(editor);
            uint[] before = EditorWindow.Capture(window);

            editor.Text = "hello\nworld\nWWWWWWWWWW";
            uint[] after = EditorWindow.Capture(window);

            Assert.NotEqual(before, after);
            window.Close();
        });
    }

    [Fact]
    public async Task FontSize_Changed_RemeasuresLineHeight()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = "hello" };
            Window window = EditorWindow.Open(editor);
            double before = editor.LineHeight;

            editor.FontSize = 28;
            EditorWindow.Capture(window);

            Assert.True(editor.LineHeight > before, $"{editor.LineHeight} <= {before}");
            window.Close();
        });
    }

    [Fact]
    public async Task ZoomIn_GrowsLineHeight()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = "hello" };
            Window window = EditorWindow.Open(editor);
            double before = editor.LineHeight;

            editor.ZoomLevel = 5;
            EditorWindow.Capture(window);

            Assert.True(editor.LineHeight > before, $"{editor.LineHeight} <= {before}");
            window.Close();
        });
    }

    [Fact]
    public async Task Theme_Dark_BackgroundIsThemeColor()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = "hello" };
            Window window = EditorWindow.Open(editor);

            uint[] pixels = EditorWindow.Capture(window);

            Assert.Equal(Background, pixels[(190 * 400) + 390]);
            window.Close();
        });
    }
}
