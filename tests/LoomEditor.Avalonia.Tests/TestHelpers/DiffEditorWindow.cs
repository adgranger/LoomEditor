using Avalonia.Controls;
using Avalonia.Threading;
using LoomEditor.Avalonia.Controls;

namespace LoomEditor.Avalonia.Tests.TestHelpers;

/// <summary>Opens a <see cref="LoomDiffEditor"/> in a headless window with the test font and waits until its diff and layout have settled.</summary>
internal static class DiffEditorWindow
{
    /// <summary>Creates the diff editor, shows it, sets the test font, waits for the diff and for <paramref name="frames"/> frames.</summary>
    public static async Task<(LoomDiffEditor Diff, Window Window)> OpenAsync(
        string original, string modified, double width = 1000, double height = 300, Action<LoomDiffEditor>? configure = null, int frames = 5)
    {
        var diff = new LoomDiffEditor { OriginalText = original, ModifiedText = modified };
        configure?.Invoke(diff);
        var window = new Window { Width = width, Height = height, Content = diff };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        diff.Original.FontFamily = new(TestFont.Family);
        diff.Modified.FontFamily = new(TestFont.Family);
        await diff.WaitForDiffAsync();
        await SettleAsync(window, frames);
        return (diff, window);
    }

    /// <summary>Lets <paramref name="frames"/> frames pass: the zones and the gutter controls are computed on the dispatcher, a few passes settle them.</summary>
    public static async Task SettleAsync(Window window, int frames = 5)
    {
        for (int i = 0; i < frames; i++)
        {
            await Task.Delay(30);
            EditorWindow.Capture(window);
            Dispatcher.UIThread.RunJobs();
        }
    }
}
