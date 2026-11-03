using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using LoomEditor.Avalonia.Controls;
using LoomEditor.Avalonia.Tests.TestHelpers;

namespace LoomEditor.Avalonia.Tests.Controls.DiffEditor;

// The twin of the WinUI control: no upstream test, all tests are new.
public class DiffLineSelectionTests
{
    private static async Task<(LoomDiffEditor Diff, Window Window)> Open(string original, string modified)
    {
        var diff = new LoomDiffEditor { OriginalText = original, ModifiedText = modified, LineSelectionEnabled = true };
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

    private static async Task Click(Window window, Control box, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        Point center = box.TranslatePoint(new Point(box.Bounds.Width / 2, box.Bounds.Height / 2), window)!.Value;
        window.MouseDown(center, MouseButton.Left, modifiers);
        window.MouseUp(center, MouseButton.Left, modifiers);
        await Settle(window);
    }

    // Original lines 2 to 4 become "x", "y", "z": a block of 3 deleted and 3 added lines.
    private const string Original = "a\nb\nc\nd\ne\nf";

    private const string Modified = "a\nx\ny\nz\ne\nf";

    [Fact]
    public async Task Click_TogglesOneLine()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Original, Modified);

            await Click(window, diff.Modified.DiffGutter!.LineCheckBoxes.VisibleCheckBoxes[0]);

            Assert.Equal([new DiffLine(DiffSide.Modified, 2)], diff.SelectedLines);
            window.Close();
        });
    }

    [Fact]
    public async Task CtrlClick_TogglesPair()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Original, Modified);

            await Click(window, diff.Modified.DiffGutter!.LineCheckBoxes.VisibleCheckBoxes[1], RawInputModifiers.Control);

            Assert.Equal([new DiffLine(DiffSide.Original, 3), new DiffLine(DiffSide.Modified, 3)], diff.SelectedLines);
            window.Close();
        });
    }

    [Fact]
    public async Task ShiftClick_ExtendsFromAnchor()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Original, Modified);
            var boxes = diff.Modified.DiffGutter!.LineCheckBoxes.VisibleCheckBoxes;

            await Click(window, boxes[0]);
            await Click(window, boxes[2], RawInputModifiers.Shift);

            Assert.Equal(3, diff.SelectedLines.Count);
            Assert.All(diff.SelectedLines, line => Assert.Equal(DiffSide.Modified, line.Side));
            window.Close();
        });
    }

    [Fact]
    public async Task NewDiff_SameVersions_KeepsSelection()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Original, Modified);
            await Click(window, diff.Modified.DiffGutter!.LineCheckBoxes.VisibleCheckBoxes[0]);
            Assert.Single(diff.SelectedLines);

            // A new diff of the same texts (an option changed): the selection stays.
            diff.IgnoreTrimWhitespace = false;
            await Task.Delay(400);
            await diff.WaitForDiffAsync();
            await Settle(window);

            Assert.Single(diff.SelectedLines);
            window.Close();
        });
    }

    [Fact]
    public async Task LineActionKey_RaisesLineActionInvoked()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open(Original, Modified);
            diff.IsReadOnly = true;
            diff.SetSelection([new DiffLine(DiffSide.Modified, 2)]);
            DiffLineActionInvokedEventArgs? received = null;
            diff.LineActionInvoked += (_, e) => received = e;
            diff.Modified.Focus();
            Dispatcher.UIThread.RunJobs();

            window.KeyPress(Key.S, RawInputModifiers.None, PhysicalKey.S, "s");
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(received);
            Assert.Equal([new DiffLine(DiffSide.Modified, 2)], received.Lines);
            window.Close();
        });
    }
}
