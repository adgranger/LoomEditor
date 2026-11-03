using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using LoomEditor.Avalonia.Controls;
using LoomEditor.Avalonia.Tests.TestHelpers;

namespace LoomEditor.Avalonia.Tests.Controls.DiffEditor;

// The twin of the WinUI control: no upstream test, all tests are new.
public class DiffAlignmentTests
{
    private static async Task<(LoomDiffEditor Diff, Window Window)> Open(string original, string modified)
    {
        var diff = new LoomDiffEditor { OriginalText = original, ModifiedText = modified };
        var window = new Window { Width = 1000, Height = 300, Content = diff };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        diff.Original.FontFamily = new(TestFont.Family);
        diff.Modified.FontFamily = new(TestFont.Family);
        await diff.WaitForDiffAsync();
        // The zones are computed on the dispatcher, a few passes settle them.
        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(30);
            EditorWindow.Capture(window);
            Dispatcher.UIThread.RunJobs();
        }

        return (diff, window);
    }

    private static string Lines(int count) => string.Join('\n', Enumerable.Range(1, count).Select(n => "line " + n));

    [Fact]
    public async Task InsertedBlock_OriginalSideGetsPaddingZone()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open("a\nb\nc", "a\nx\ny\nb\nc");

            // "b" is line 2 of the original and line 4 of the modified side: the zone pads the original side so both tops are equal.
            Assert.Equal(diff.Modified.GetTopForLineNumber(4, true), diff.Original.GetTopForLineNumber(2, true));
            window.Close();
        });
    }

    [Fact]
    public async Task Scroll_OneSide_ScrollsTheOther()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Lines(200), Lines(200));

            diff.Modified.ScrollTop = 120;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(120, diff.Modified.ScrollTop, 1);
            Assert.Equal(diff.Modified.ScrollTop, diff.Original.ScrollTop, 1);
            window.Close();
        });
    }

    [Fact]
    public async Task OverviewRuler_DrawsChangeMarks()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Lines(200), Lines(200).Replace("line 100\n", "changed\n"));

            uint[] pixels = EditorWindow.Capture(window);
            int width = (int)window.Width;
            var colors = new HashSet<uint>();
            for (int y = 0; y < 300; y++)
            {
                for (int x = width - 29; x < width; x += 4)
                {
                    colors.Add(pixels[(y * width) + x]);
                }
            }

            // The background, the slider and at least one change mark.
            Assert.True(colors.Count >= 3, $"colors: {colors.Count}");
            window.Close();
        });
    }

    [Fact]
    public async Task OverviewRuler_Click_ScrollsToChange()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Lines(200), Lines(200).Replace("line 150\n", "changed\n"));
            Assert.Equal(0, diff.Modified.ScrollTop);

            // A press on the track below the slider centers the slider on the pointer.
            var point = new Point(window.Width - 10, 220);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            Assert.True(diff.Modified.ScrollTop > 0);
            window.Close();
        });
    }
}
