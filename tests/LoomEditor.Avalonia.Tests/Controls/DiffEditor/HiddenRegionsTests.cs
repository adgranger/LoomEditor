using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using LoomEditor.Avalonia.Controls;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.Avalonia.Tests.Controls.DiffEditor;

// The twin of the WinUI control: no upstream test, all tests are new.
public class HiddenRegionsTests
{
    private static string Lines(int count, int changed) =>
        string.Join('\n', Enumerable.Range(1, count).Select(n => n == changed ? "changed" : "line " + n));

    [Fact]
    public async Task HideUnchanged_CollapsesLongUnchangedRun()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await DiffEditorWindow.OpenAsync(Lines(100, 0), Lines(100, 50));
            double before = diff.Modified.GetScrollHeight();

            diff.HideUnchangedRegions = true;
            await DiffEditorWindow.SettleAsync(window);

            Assert.True(diff.Modified.GetScrollHeight() < before / 2);
            window.Close();
        });
    }

    [Fact]
    public async Task ExpandControl_Click_ShowsLines()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await DiffEditorWindow.OpenAsync(Lines(100, 0), Lines(100, 50));
            diff.HideUnchangedRegions = true;
            await DiffEditorWindow.SettleAsync(window);
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
            await DiffEditorWindow.SettleAsync(window);

            Assert.True(diff.Modified.GetScrollHeight() > collapsed);
            window.Close();
        });
    }

    private readonly record struct ViewZoneId(int Top, int Height);
}
