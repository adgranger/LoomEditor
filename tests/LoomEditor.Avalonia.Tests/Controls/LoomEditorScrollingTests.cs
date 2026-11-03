using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LoomEditor.Avalonia.Tests.TestHelpers;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class LoomEditorScrollingTests
{
    private static string Lines(int count) => string.Join('\n', Enumerable.Range(1, count).Select(i => "line " + i));

    [Fact]
    public async Task MouseWheel_Down_ScrollsByLines()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = Lines(100) };
            var window = EditorWindow.Open(editor);
            EditorWindow.Capture(window);

            window.MouseWheel(new Point(100, 100), new Vector(0, -1));
            Dispatcher.UIThread.RunJobs();

            // Monaco: one wheel notch scrolls 50 DIPs (`StandardWheelEvent`, `ScrollableElement`).
            Assert.Equal(50, editor.ScrollTop);
            window.Close();
        });
    }

    [Fact]
    public async Task VerticalScrollBar_Value_FollowsScrollTop()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = Lines(100) };
            var window = EditorWindow.Open(editor);
            EditorWindow.Capture(window);

            editor.ScrollTop = 120;
            Dispatcher.UIThread.RunJobs();

            ScrollBar bar = editor.GetVisualDescendants().OfType<ScrollBar>().Single(bar => bar.Name == "PART_VerticalScrollBar");
            Assert.True(bar.IsVisible);
            Assert.Equal(120, bar.Value);
            window.Close();
        });
    }

    [Fact]
    public async Task RevealLine_Far_ScrollsIntoView()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = Lines(200) };
            var window = EditorWindow.Open(editor);
            EditorWindow.Capture(window);

            editor.RevealRange(new Range(150, 1, 150, 1));
            EditorWindow.Capture(window);

            // Line 150 starts at 149 lines of the line height; the viewport shows 200 DIPs.
            double top = 149 * editor.LineHeight;
            Assert.InRange(editor.ScrollTop, top - EditorWindow.Height, top);
            window.Close();
        });
    }
}
