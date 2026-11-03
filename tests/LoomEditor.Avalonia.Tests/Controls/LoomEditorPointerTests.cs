using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using LoomEditor.Avalonia.Controls;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Core.Text;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class LoomEditorPointerTests
{
    private static readonly RawInputModifiers CommandKey = OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control;

    private static (Editor Editor, Window Window) Open(string text)
    {
        var editor = new Editor { Text = text, MinimapEnabled = false };
        Window window = EditorWindow.Open(editor);
        EditorWindow.Capture(window);
        return (editor, window);
    }

    // The point of a character boundary: `line` and `column` are 1-based.
    private static Point At(Editor editor, int line, int column)
    {
        double x = editor.ViewModel.LayoutInfo.ContentLeft + ((column - 1) * editor.ViewModel.FontInfo.SpaceWidth);
        double y = ((line - 0.5) * editor.ViewModel.FontInfo.LineHeight);
        return new Point(x, y);
    }

    private static void Click(Window window, Point point, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.MouseDown(point, MouseButton.Left, modifiers);
        window.MouseUp(point, MouseButton.Left, modifiers);
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public async Task Click_PlacesCaret()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open("abcdef\nghijkl");

            Click(window, At(editor, 2, 3));

            Selection selection = Assert.Single(editor.Selections);
            Assert.True(selection.IsEmpty);
            Assert.Equal(new Position(2, 3), selection.Position);
            window.Close();
        });
    }

    [Fact]
    public async Task DoubleClick_SelectsWord()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open("hello world");
            Point point = At(editor, 1, 3);

            Click(window, point);
            Click(window, point);

            Selection selection = Assert.Single(editor.Selections);
            Assert.Equal(new Position(1, 1), selection.SelectionStart);
            Assert.Equal(new Position(1, 6), selection.Position);
            window.Close();
        });
    }

    [Fact]
    public async Task Drag_SelectsRange()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open("abcdef\nghijkl");

            window.MouseDown(At(editor, 1, 2), MouseButton.Left);
            window.MouseMove(At(editor, 2, 4));
            window.MouseUp(At(editor, 2, 4), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            Selection selection = Assert.Single(editor.Selections);
            Assert.Equal(new Position(1, 2), selection.SelectionStart);
            Assert.Equal(new Position(2, 4), selection.Position);
            window.Close();
        });
    }

    [Fact]
    public async Task ShiftClick_ExtendsSelection()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open("abcdef");

            Click(window, At(editor, 1, 2));
            Click(window, At(editor, 1, 5), RawInputModifiers.Shift);

            Selection selection = Assert.Single(editor.Selections);
            Assert.Equal(new Position(1, 2), selection.SelectionStart);
            Assert.Equal(new Position(1, 5), selection.Position);
            window.Close();
        });
    }

    [Fact]
    public async Task AltClick_AddsCursor()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open("abcdef\nghijkl");

            Click(window, At(editor, 1, 2));
            Click(window, At(editor, 2, 4), RawInputModifiers.Alt);

            Assert.Equal(2, editor.Selections.Count);
            window.Close();
        });
    }

    [Fact]
    public async Task CtrlWheel_Zooms()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (Editor editor, Window window) = Open("abcdef\nghijkl");
            editor.MouseWheelZoom = true;

            for (int i = 0; i < 3; i++)
            {
                window.MouseWheel(new Point(100, 50), new Vector(0, 1), CommandKey);
                Dispatcher.UIThread.RunJobs();
            }

            Assert.True(editor.ZoomLevel > 0, $"zoom level {editor.ZoomLevel}");
            window.Close();
        });
    }

    [Fact]
    public async Task GutterClick_RaisesGutterClicked()
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

            // The twin of this test in HostHooksTests (P15-16).
            Click(window, new Point(editor.ViewModel.LayoutInfo.LineNumbersLeft + 2, (1.5 * editor.ViewModel.FontInfo.LineHeight)));

            Assert.NotNull(received);
            Assert.Equal(2, received.LineNumber);
            Assert.Equal(GutterArea.LineNumbers, received.Area);
            window.Close();
        });
    }
}
