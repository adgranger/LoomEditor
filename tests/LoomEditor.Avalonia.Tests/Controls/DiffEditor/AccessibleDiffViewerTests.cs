using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using LoomEditor.Avalonia.Controls;
using LoomEditor.Avalonia.Tests.TestHelpers;

namespace LoomEditor.Avalonia.Tests.Controls.DiffEditor;

// The twin of the WinUI control: no upstream test, all tests are new.
public class AccessibleDiffViewerTests
{
    private static async Task<(LoomDiffEditor Diff, Window Window)> Open()
    {
        string original = string.Join('\n', Enumerable.Range(1, 30).Select(n => "line " + n));
        string modified = original.Replace("line 5\n", "changed 5\n").Replace("line 25\n", "changed 25\n");
        var diff = new LoomDiffEditor { OriginalText = original, ModifiedText = modified };
        var window = new Window { Width = 1000, Height = 300, Content = diff };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        diff.Original.FontFamily = new(TestFont.Family);
        diff.Modified.FontFamily = new(TestFont.Family);
        await diff.WaitForDiffAsync();
        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(30);
            EditorWindow.Capture(window);
            Dispatcher.UIThread.RunJobs();
        }

        diff.Modified.Focus();
        return (diff, window);
    }

    private static void F7(Window window)
    {
        window.KeyPress(Key.F7, RawInputModifiers.None, PhysicalKey.F7, null);
        Dispatcher.UIThread.RunJobs();
    }

    [Fact]
    public async Task F7_OpensViewer_FocusesFirstChange()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open();

            F7(window);

            Assert.True(diff.IsAccessibleDiffViewerOpen);
            Assert.True(diff.AccessibleViewer!.Rows.Count > 0);
            Assert.Contains(diff.AccessibleViewer.Rows, row => row.IsFocused || row.IsSelected);
            window.Close();
        });
    }

    [Fact]
    public async Task Down_MovesToNextRow()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open();
            F7(window);
            ListBoxItem[] rows = [.. diff.AccessibleViewer!.Rows];
            int before = Array.FindIndex(rows, row => row.IsSelected);

            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(before + 1, Array.FindIndex([.. diff.AccessibleViewer!.Rows], row => row.IsSelected));
            window.Close();
        });
    }

    [Fact]
    public async Task Row_AutomationName_IsRowItemName()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open();
            F7(window);

            foreach (ListBoxItem row in diff.AccessibleViewer!.Rows)
            {
                Assert.False(string.IsNullOrEmpty(AutomationProperties.GetName(row)));
            }

            window.Close();
        });
    }

    [Fact]
    public async Task Escape_ClosesAndFocusesModifiedEditor()
    {
        await HeadlessSession.RunAsync(async () =>
        {
            (LoomDiffEditor diff, Window window) = await Open();
            F7(window);

            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Dispatcher.UIThread.RunJobs();

            Assert.False(diff.IsAccessibleDiffViewerOpen);
            Assert.True(diff.Modified.IsFocused);
            window.Close();
        });
    }
}
