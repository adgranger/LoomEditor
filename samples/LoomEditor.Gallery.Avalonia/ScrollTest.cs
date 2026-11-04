using System.Diagnostics;
using Avalonia.Controls;
using Editor = LoomEditor.Avalonia.Controls.LoomEditor;

namespace LoomEditor.Gallery;

/// <summary>Scrolls an editor on every rendered frame for a while and collects the frame intervals.</summary>
/// <remarks>Threading: UI thread only. The intervals come from a <see cref="Stopwatch"/> read in each animation frame callback.</remarks>
internal static class ScrollTest
{
    private const int LinesPerFrame = 3;

    /// <summary>Scrolls <paramref name="editor"/> for <paramref name="duration"/>, wrapping to the top at the end.</summary>
    public static Task<FrameStatistics> RunAsync(Editor editor, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(editor);

        var completion = new TaskCompletionSource<FrameStatistics>();
        TopLevel? topLevel = TopLevel.GetTopLevel(editor);
        if (topLevel is null)
        {
            completion.SetException(new InvalidOperationException("The editor is not in a window."));
            return completion.Task;
        }

        var intervals = new List<double>();
        // Monaco's default line height: round(1.35 × font size), 19 DIPs at 14.
        double step = LinesPerFrame * Math.Round(1.35 * editor.FontSize);
        var stopwatch = Stopwatch.StartNew();
        TimeSpan? previous = null;
        TimeSpan? start = null;

        void OnFrame(TimeSpan unused)
        {
            TimeSpan now = stopwatch.Elapsed;
            start ??= now;
            if (previous is { } last)
            {
                intervals.Add((now - last).TotalMilliseconds);
            }

            previous = now;
            if (now - start >= duration)
            {
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

            topLevel.RequestAnimationFrame(OnFrame);
        }

        topLevel.RequestAnimationFrame(OnFrame);
        return completion.Task;
    }
}
