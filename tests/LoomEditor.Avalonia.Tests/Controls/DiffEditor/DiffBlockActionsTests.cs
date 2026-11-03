using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LoomEditor.Avalonia.Controls;
using LoomEditor.Avalonia.Tests.TestHelpers;

namespace LoomEditor.Avalonia.Tests.Controls.DiffEditor;

// The twin of the WinUI control: no upstream test, all tests are new.
public class DiffBlockActionsTests
{
    private static string Lines(int count) => string.Join('\n', Enumerable.Range(1, count).Select(n => "line " + n));

    private static async Task<(LoomDiffEditor Diff, Window Window)> Open(string original, string modified)
    {
        var diff = new LoomDiffEditor
        {
            OriginalText = original,
            ModifiedText = modified,
            BlockActionsVisibility = DiffBlockActionsVisibility.Always,
            PrimaryBlockActionGlyph = "X",
            PrimaryBlockActionToolTip = "Revert",
        };
        var window = new Window { Width = 1000, Height = 600, Content = diff };
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

    private static string FiveChanges() => Lines(20).Replace("line 2\n", "a\n").Replace("line 6\n", "b\n").Replace("line 10\n", "c\n").Replace("line 14\n", "d\n").Replace("line 18\n", "e\n");

    [Fact]
    public async Task Always_FiveBlocks_FiveButtons()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Lines(20), FiveChanges());

            Assert.Equal(5, diff.Changes.Count);
            Assert.Equal(5, diff.Modified.DiffGutter!.BlockActions.VisibleButtons.Count);
            Assert.Empty(diff.Original.DiffGutter!.BlockActions.VisibleButtons);
            window.Close();
        });
    }

    [Fact]
    public async Task Click_Third_RaisesBlockActionInvoked_WithChanges2()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Lines(20), FiveChanges());
            DiffBlockActionInvokedEventArgs? received = null;
            diff.BlockActionInvoked += (_, e) => received = e;

            diff.Modified.DiffGutter!.BlockActions.VisibleButtons[2].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.NotNull(received);
            Assert.Same(diff.Changes[2], received.Block);
            Assert.Equal(DiffBlockAction.Primary, received.Action);
            window.Close();
        });
    }

    [Fact]
    public async Task PureDeletion_ButtonOnOriginalSide()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open("a\nb\nc\nd\ne", "a\ne");

            Assert.Single(diff.Changes);
            Assert.Single(diff.Original.DiffGutter!.BlockActions.VisibleButtons);
            Assert.Empty(diff.Modified.DiffGutter!.BlockActions.VisibleButtons);
            window.Close();
        });
    }

    [Fact]
    public async Task OnHover_OnlyHoveredBlock()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Lines(20), FiveChanges());
            diff.BlockActionsVisibility = DiffBlockActionsVisibility.OnHover;
            await Settle(window);
            Assert.Empty(diff.Modified.DiffGutter!.BlockActions.VisibleButtons);

            // Line 6 is the second block.
            var inContent = new Point(diff.Modified.ViewModel.LayoutInfo.ContentLeft + 20, (5.5 * diff.Modified.LineHeight));
            window.MouseMove(diff.Modified.TranslatePoint(inContent, window)!.Value);
            await Settle(window);

            Assert.Single(diff.Modified.DiffGutter!.BlockActions.VisibleButtons);
            window.Close();
        });
    }

    [Fact]
    public async Task NotSideBySide_NoButtons()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Lines(20), FiveChanges());

            diff.RenderSideBySide = false;
            await Settle(window);

            Assert.Empty(diff.Modified.DiffGutter!.BlockActions.VisibleButtons);
            window.Close();
        });
    }
}
