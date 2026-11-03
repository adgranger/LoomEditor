using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using LoomEditor.Avalonia.Controls;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Themes;

namespace LoomEditor.Avalonia.Tests.Controls.DiffEditor;

// The twin of the WinUI control: no upstream test, all tests are new.
public class LoomDiffEditorTests
{
    private static (LoomDiffEditor Diff, Window Window) Open(double width)
    {
        var diff = new LoomDiffEditor { OriginalText = "one\ntwo\nthree", ModifiedText = "one\ntwo\nnew line\nthree" };
        var window = new Window { Width = width, Height = 300, Content = diff };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        diff.Original.FontFamily = new(TestFont.Family);
        diff.Modified.FontFamily = new(TestFont.Family);
        return (diff, window);
    }

    private static async Task WaitForDiff(LoomDiffEditor diff)
    {
        await diff.WaitForDiffAsync();
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public async Task DiffEditor_TwoTexts_ShowsInsertedLineDecoration()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = Open(1000);
            await WaitForDiff(diff);

            uint[] pixels = EditorWindow.Capture(window);
            double lineHeight = diff.Modified.LineHeight;
            int width = (int)window.Width;
            EditorTheme theme = EditorTheme.Dark;
            uint background = ((uint)theme.Background.A << 24) | ((uint)theme.Background.R << 16) | ((uint)theme.Background.G << 8) | theme.Background.B;
            // The right end of a line of the modified side, far from its text and from the overview ruler room.
            int x = width - 60;
            uint unchanged = pixels[(int)(lineHeight * 0.5) * width + x];
            uint inserted = pixels[(int)(lineHeight * 2.5) * width + x];

            Assert.Equal(background, unchanged);
            Assert.NotEqual(background, inserted);
            window.Close();
        });
    }

    [Fact]
    public async Task Sash_Drag_ChangesSplit()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (LoomDiffEditor diff, Window window) = Open(1000);
            double before = diff.Original.Width;
            var press = new Point(before, 100);

            window.MouseDown(press, MouseButton.Left);
            window.MouseMove(new Point(before + 80, 100));
            window.MouseUp(new Point(before + 80, 100), MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(before + 80, diff.Original.Width, 1);
            window.Close();
        });
    }

    [Fact]
    public async Task NarrowWidth_FallsBackToInline()
    {
        await HeadlessSession.RunAsync(() =>
        {
            (LoomDiffEditor wide, Window wideWindow) = Open(1000);
            (LoomDiffEditor narrow, Window narrowWindow) = Open(600);

            Assert.True(wide.Original.IsVisible);
            Assert.False(narrow.Original.IsVisible);
            wideWindow.Close();
            narrowWindow.Close();
        });
    }

    [Fact]
    public async Task DiffChanged_RaisedAfterCompute()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = Open(1000);
            int raised = 0;
            diff.DiffChanged += (_, _) => raised++;

            await WaitForDiff(diff);

            Assert.True(diff.IsDiffUpToDate);
            Assert.True(raised > 0);
            Assert.Equal(1, diff.ChangeCount);
            window.Close();
        });
    }
}
