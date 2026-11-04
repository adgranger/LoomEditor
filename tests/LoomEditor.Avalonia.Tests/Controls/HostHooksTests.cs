using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using LoomEditor.Avalonia.Controls;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Core.Text;
using LoomEditor.Themes;
using LoomEditor.View.Decorations;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class HostHooksTests
{
    // The captured frame is RGBA: opaque red is 0xFF0000FF.
    private const uint RedPixel = 0xFF0000FF;

    private static string Lines(int count) => string.Join('\n', Enumerable.Range(1, count).Select(i => "line " + i));

    private static (Editor Editor, Window Window) Open(string text)
    {
        var editor = new Editor { Text = text, MinimapEnabled = false };
        Window window = EditorWindow.Open(editor);
        EditorWindow.Capture(window);
        return (editor, window);
    }

    // The host table of custom classes is global: the test uses its own class name and unregisters it.
    [Fact]
    public async Task CreateDecorationsCollection_DrawsInlineClass()
    {
        await HeadlessSession.RunAsync(() =>
        {
            const string className = "test-avalonia-host-hooks-background";
            CustomDecorationStyles.Register(className, new CustomDecorationStyle
            {
                Background = ThemeColor.FromRgb(0xFF, 0, 0),
                DarkBackground = ThemeColor.FromRgb(0xFF, 0, 0),
            });
            try
            {
                (Editor editor, Window window) = Open("hello world");
                editor.CreateDecorationsCollection([new EditorDecoration(new Range(1, 1, 1, 6), new EditorDecorationOptions { ClassName = className })]);

                uint[] pixels = EditorWindow.Capture(window);

                int left = (int)editor.ViewModel.LayoutInfo.ContentLeft;
                int red = 0;
                for (int y = 0; y < (int)editor.LineHeight; y++)
                {
                    for (int x = left; x < left + 40; x++)
                    {
                        red += pixels[(y * EditorWindow.Width) + x] == RedPixel ? 1 : 0;
                    }
                }

                Assert.True(red > 100, $"{red} red pixels");
                window.Close();
            }
            finally
            {
                CustomDecorationStyles.Unregister(className);
            }
        });
    }

    [Fact]
    public async Task ScrollChanged_RaisedOnWheel()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open(Lines(100));
            EditorScrollChangedEventArgs? received = null;
            editor.ScrollChanged += (_, e) => received = e;

            window.MouseWheel(new Point(100, 100), new Vector(0, -1));
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(received);
            Assert.True(received.ScrollTopChanged);
            Assert.Equal(editor.ScrollTop, received.ScrollTop);
            window.Close();
        });
    }

    [Fact]
    public async Task LayoutChanged_RaisedOnZoom()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open("hello");
            int raised = 0;
            editor.LayoutChanged += (_, _) => raised++;

            editor.ZoomLevel = 3;
            EditorWindow.Capture(window);

            Assert.True(raised > 0);
            window.Close();
        });
    }

    [Fact]
    public async Task GetTopForLineNumber_MatchesRenderer()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open(Lines(10));

            Assert.Equal(4 * editor.LineHeight, editor.GetTopForLineNumber(5));
            Assert.Equal(0, editor.GetTopForLineNumber(-3));
            Assert.Equal(9 * editor.LineHeight, editor.GetTopForLineNumber(99));
            window.Close();
        });
    }

    [Fact]
    public async Task SetHiddenAreas_HidesLines()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open(Lines(10));

            editor.SetHiddenAreas([new Range(2, 1, 3, 1)]);

            Assert.Equal(editor.LineHeight, editor.GetTopForLineNumber(4));
            editor.SetHiddenAreas([]);
            Assert.Equal(3 * editor.LineHeight, editor.GetTopForLineNumber(4));
            window.Close();
        });
    }

    [Fact]
    public async Task ResetText_KeepsScrollAtTop()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open(Lines(200));
            editor.ScrollTop = 500;
            Dispatcher.UIThread.RunJobs();

            editor.ResetText("short");
            EditorWindow.Capture(window);

            Assert.Equal(0, editor.ScrollTop);
            Assert.Equal(new Position(1, 1), editor.Selections[0].Position);
            window.Close();
        });
    }

    [Fact]
    public async Task GutterClicked_LineNumberArea()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open("abcdef\nghijkl");
            GutterClickedEventArgs? received = null;
            editor.GutterClicked += (_, e) =>
            {
                received = e;
                e.Handled = true;
            };
            var point = new Point(editor.ViewModel.LayoutInfo.LineNumbersLeft + 2, 1.5 * editor.LineHeight);

            window.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(received);
            Assert.Equal(2, received.LineNumber);
            Assert.Equal(GutterArea.LineNumbers, received.Area);
            window.Close();
        });
    }
}
