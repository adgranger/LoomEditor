using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LoomEditor.Avalonia.Tests.TestHelpers;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class HostViewZoneTests
{
    private static string Lines(int count) => string.Join('\n', Enumerable.Range(1, count).Select(i => "line " + i));

    private static double TopOfLine(Editor editor, int line) => editor.ViewModel.ViewLayout.GetVerticalOffsetForLineNumber(line, true);

    [Fact]
    public async Task AddViewZone_PushesNextLineDown()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = Lines(10) };
            var window = EditorWindow.Open(editor);
            double before = TopOfLine(editor, 5);

            editor.AddViewZone(2, 40, new Border());

            Assert.Equal(before + 40, TopOfLine(editor, 5));
            window.Close();
        });
    }

    [Fact]
    public async Task AddViewZone_ContentIsArrangedInGap()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = Lines(10) };
            var window = EditorWindow.Open(editor);
            var content = new Border { Background = global::Avalonia.Media.Brushes.Red };
            double gapTop = TopOfLine(editor, 3);

            editor.AddViewZone(2, 40, content);
            EditorWindow.Capture(window);
            Dispatcher.UIThread.RunJobs();
            EditorWindow.Capture(window);

            Assert.True(content.IsVisible);
            Assert.Equal(40, content.Bounds.Height);
            Assert.Equal(gapTop, content.Bounds.Top);
            window.Close();
        });
    }

    [Fact]
    public async Task RemoveViewZone_RestoresLayout()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = Lines(10) };
            var window = EditorWindow.Open(editor);
            double before = TopOfLine(editor, 5);
            var content = new Border();
            string id = editor.AddViewZone(2, 40, content);
            EditorWindow.Capture(window);
            Dispatcher.UIThread.RunJobs();

            Assert.True(editor.RemoveViewZone(id));
            Assert.False(editor.RemoveViewZone(id));

            Assert.Equal(before, TopOfLine(editor, 5));
            Assert.DoesNotContain(content, editor.GetVisualDescendants());
            window.Close();
        });
    }

    [Fact]
    public async Task SetViewZoneHeight_ResizesGap()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = Lines(10) };
            var window = EditorWindow.Open(editor);
            double before = TopOfLine(editor, 5);
            string id = editor.AddViewZone(2, 40);

            Assert.True(editor.SetViewZoneHeight(id, 60.2));

            Assert.Equal(before + 61, TopOfLine(editor, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.SetViewZoneHeight(id, 0));
            window.Close();
        });
    }
}
