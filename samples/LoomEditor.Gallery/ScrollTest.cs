using System.Diagnostics;
using Microsoft.UI.Xaml.Media;
using Editor = LoomEditor.WinUI.Controls.LoomEditor;

namespace LoomEditor.Gallery;

/// <summary>Scrolls an editor on every rendered frame for a while and collects the frame intervals.</summary>
/// <remarks>
/// Threading: UI thread only. It uses the public API of the editor (<c>ScrollTop</c>, <c>FontSize</c>).
/// Behavior difference from the card: the intervals come from a <see cref="Stopwatch"/> read in each
/// <c>CompositionTarget.Rendering</c> callback, not from <c>RenderingTime</c>.
/// </remarks>
internal static class ScrollTest
{
    private const int LinesPerFrame = 3;

    /// <summary>Scrolls <paramref name="editor"/> for <paramref name="duration"/>, wrapping to the top at the end.</summary>
    public static Task<FrameStatistics> RunAsync(Editor editor, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var completion = new TaskCompletionSource<FrameStatistics>();
        var intervals = new List<double>();
        // Monaco's default line height: round(1.35 × font size), 19 DIPs at 14.
        double step = LinesPerFrame * Math.Round(1.35 * editor.FontSize);
        var stopwatch = Stopwatch.StartNew();
        TimeSpan? previous = null;
        TimeSpan? start = null;

        void OnRendering(object? sender, object e)
        {
            // Not `RenderingEventArgs.RenderingTime`: casting the event arguments fails in a Native AOT build (E_NOINTERFACE).
            TimeSpan now = stopwatch.Elapsed;
            start ??= now;
            if (previous is { } last)
            {
                intervals.Add((now - last).TotalMilliseconds);
            }

            previous = now;
            if (now - start >= duration)
            {
                CompositionTarget.Rendering -= OnRendering;
                completion.SetResult(FrameStatistics.Compute(intervals));
                return;
            }

            double before = editor.ScrollTop;
            editor.ScrollTop = before + step;
            if (editor.ScrollTop <= before)
            {
                // The end of the document: start again.
                editor.ScrollTop = 0;
            }
        }

        CompositionTarget.Rendering += OnRendering;
        return completion.Task;
    }
}
