using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using LoomEditor.Avalonia.Controls;
using LoomEditor.Avalonia.Tests.TestHelpers;

namespace LoomEditor.Avalonia.Tests.Controls.DiffEditor;

// The twin of the WinUI control: no upstream test, all tests are new.
public class DiffOptionsTests
{
    private static async Task<(LoomDiffEditor Diff, Window Window)> Open(string original, string modified, double width = 1000)
    {
        var diff = new LoomDiffEditor { OriginalText = original, ModifiedText = modified };
        var window = new Window { Width = width, Height = 300, Content = diff };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        diff.Original.FontFamily = new(TestFont.Family);
        diff.Modified.FontFamily = new(TestFont.Family);
        await diff.WaitForDiffAsync();
        Dispatcher.UIThread.RunJobs();
        return (diff, window);
    }

    private static string Lines(params int[] numbers) => string.Join('\n', numbers.Select(n => "line " + n));

    // Ten lines, changes at 2 and 8 (the modified side has them edited).
    private static string Original => Lines(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

    private static string Modified => Original.Replace("line 2\n", "changed 2\n").Replace("line 8\n", "changed 8\n");

    [Fact]
    public async Task RenderSideBySide_False_ShowsInline()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Original, Modified);
            Assert.True(diff.Original.IsVisible);

            diff.RenderSideBySide = false;
            Dispatcher.UIThread.RunJobs();

            Assert.False(diff.Original.IsVisible);
            window.Close();
        });
    }

    [Fact]
    public async Task IgnoreTrimWhitespace_WhitespaceOnly_NoChange()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open("a\nb\nc", "a\n  b  \nc");
            Assert.Equal(0, diff.ChangeCount);

            diff.IgnoreTrimWhitespace = false;
            await diff.WaitForDiffAsync();
            await Task.Delay(300);
            Dispatcher.UIThread.RunJobs();
            await diff.WaitForDiffAsync();

            Assert.Equal(1, diff.ChangeCount);
            window.Close();
        });
    }

    [Fact]
    public async Task AltF5_GoesToNextChange()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Original, Modified);
            diff.Modified.Focus();
            diff.Modified.SetSelections([new Core.Text.Selection(1, 1, 1, 1)]);

            window.KeyPress(Key.F5, RawInputModifiers.Alt, PhysicalKey.F5, null);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(2, diff.Modified.Selections[0].PositionLineNumber);
            window.Close();
        });
    }

    [Fact]
    public async Task ShiftAltF5_GoesToPrevious()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Original, Modified);
            diff.Modified.Focus();
            diff.Modified.SetSelections([new Core.Text.Selection(10, 1, 10, 1)]);

            window.KeyPress(Key.F5, RawInputModifiers.Alt | RawInputModifiers.Shift, PhysicalKey.F5, null);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(8, diff.Modified.Selections[0].PositionLineNumber);
            window.Close();
        });
    }

    [Fact]
    public async Task Changes_MatchesDiff()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Original, Modified);

            Assert.Equal(diff.ChangeCount, diff.Changes.Count);
            Assert.Equal(2, diff.Changes.Count);
            window.Close();
        });
    }
}
