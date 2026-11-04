using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LoomEditor.Avalonia.Controls.Overlay;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.View.Layout;
using Editor = global::LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Avalonia.Tests.Controls;

// The twin of the WinUI control: no upstream test, all tests are new.
public class OverlayHostTests
{
    [Fact]
    public async Task Overlay_Show_PlacesAtCoordinates()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = "a" };
            var window = EditorWindow.Open(editor);
            OverlayHost overlay = editor.Overlay!;
            var element = new Border { Width = 10, Height = 10 };

            overlay.Show(element, new OverlayRect(30, 40, 100, 50));
            Dispatcher.UIThread.RunJobs();

            Canvas canvas = editor.GetVisualDescendants().OfType<Canvas>().Single(c => c.Name == "PART_OverlayCanvas");
            Assert.Contains(element, canvas.Children);
            Assert.Equal(30, Canvas.GetLeft(element));
            Assert.Equal(40, Canvas.GetTop(element));
            Assert.Equal(100, element.MaxWidth);
            Assert.True(element.IsVisible);
            Assert.True(overlay.Contains(element));
            window.Close();
        });
    }

    [Fact]
    public async Task Overlay_Hide_CollapsesElement()
    {
        await HeadlessSession.RunAsync(() =>
        {
            var editor = new Editor { Text = "a" };
            var window = EditorWindow.Open(editor);
            OverlayHost overlay = editor.Overlay!;
            var element = new Border { Width = 10, Height = 10 };
            overlay.Show(element, new OverlayRect(0, 0, 10, 10));

            OverlayHost.Hide(element);

            Assert.False(element.IsVisible);
            overlay.Remove(element);
            Assert.False(overlay.Contains(element));
            window.Close();
        });
    }
}
