using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using LoomEditor.Avalonia.Controls;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.Avalonia.Tests.Controls.DiffEditor;

// The twin of the WinUI control: no upstream test, all tests are new.
public class HiddenRegionsTests
{
    private static string Lines(int count, int changed) =>
        string.Join('\n', Enumerable.Range(1, count).Select(n => n == changed ? "changed" : "line " + n));

    private static async Task<(LoomDiffEditor Diff, Window Window)> Open()
    {
        var diff = new LoomDiffEditor { OriginalText = Lines(100, 0), ModifiedText = Lines(100, 50) };
        var window = new Window { Width = 1000, Height = 300, Content = diff };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        diff.Original.FontFamily = new(TestFont.Family);
        diff.Modified.FontFamily = new(TestFont.Family);
        await diff.WaitForDiffAsync();
        await Settle(window);
        return (diff, window);
    }

    private static async Task Settle(Window window)
    {
        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(30);
            EditorWindow.Capture(window);
            Dispatcher.UIThread.RunJobs();
        }
    }

    [Fact]
    public async Task HideUnchanged_CollapsesLongUnchangedRun()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open();
            double before = diff.Modified.GetScrollHeight();

            diff.HideUnchangedRegions = true;
            await Settle(window);

            Assert.True(diff.Modified.GetScrollHeight() < before / 2);
            window.Close();
        });
    }

    [Fact]
    public async Task ExpandControl_Click_ShowsLines()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open();
            diff.HideUnchangedRegions = true;
            await Settle(window);
            double collapsed = diff.Modified.GetScrollHeight();

            // The first bar: the zone with the collapsed data of the modified side.
            ViewZoneId zone = default;
            foreach (var whitespace in diff.Modified.ViewModel.ViewLayout.GetWhitespaceViewportData())
            {
                if (diff.Modified.ViewZones.GetZone(whitespace.Id)?.Data is CollapsedZoneData)
                {
                    zone = new ViewZoneId(whitespace.VerticalOffset, whitespace.Height);
                    break;
                }
            }

            Assert.True(zone.Height > 0);
            Point inModified = new(diff.Modified.ViewModel.LayoutInfo.ContentLeft + 80, zone.Top - diff.Modified.ScrollTop + (zone.Height / 2));
            Point point = diff.Modified.TranslatePoint(inModified, window)!.Value;
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            await Settle(window);

            Assert.True(diff.Modified.GetScrollHeight() > collapsed);
            window.Close();
        });
    }

    private readonly record struct ViewZoneId(int Top, int Height);
}
