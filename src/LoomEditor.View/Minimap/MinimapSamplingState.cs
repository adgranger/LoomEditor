// Ported from microsoft/vscode: src/vs/editor/browser/viewParts/minimap/minimap.ts (MinimapSamplingState)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Layout;

namespace LoomEditor.View.Minimap;

/// <summary>A change of the sampled lines. Monaco: <c>SamplingStateEvent</c> (tagged union).</summary>
internal abstract record SamplingStateEvent;

/// <summary>Monaco: <c>SamplingStateLinesInsertedEvent</c>. <c>I</c> is Monaco's private <c>_i</c>.</summary>
internal sealed record SamplingStateLinesInserted(int InsertFromLineNumber, int InsertToLineNumber) : SamplingStateEvent
{
    internal int I { get; init; }
}

/// <summary>Monaco: <c>SamplingStateLinesDeletedEvent</c>. <c>OldIndex</c> is Monaco's private <c>_oldIndex</c>.</summary>
internal sealed record SamplingStateLinesDeleted(int DeleteFromLineNumber, int DeleteToLineNumber) : SamplingStateEvent
{
    internal int OldIndex { get; init; }
}

/// <summary>Monaco: <c>SamplingStateFlushEvent</c>.</summary>
internal sealed record SamplingStateFlush : SamplingStateEvent;

/// <summary>Which view lines a sampled minimap shows. Monaco: <c>MinimapSamplingState</c>.</summary>
/// <remarks>
/// Threading: mutable (<see cref="OnLinesDeleted"/>, <see cref="OnLinesInserted"/>); UI thread only.
/// Behavior differences: <c>Math.round</c> is <see cref="MidpointRounding.AwayFromZero"/>; the line-range methods return
/// nullable tuples; the change methods take line numbers instead of view events.
/// </remarks>
internal sealed class MinimapSamplingState
{
    private const int MaxEventCount = 10; // more changes than this are reported as a single flush

    private readonly int[] _minimapLines;

    /// <summary>Builds a state; use <see cref="Compute"/>.</summary>
    /// <param name="samplingRatio">View lines per minimap line.</param>
    /// <param name="minimapLines">A map of 0-based minimap line indexes to 1-based view line numbers.</param>
    public MinimapSamplingState(double samplingRatio, int[] minimapLines)
    {
        SamplingRatio = samplingRatio;
        _minimapLines = minimapLines;
    }

    /// <summary>View lines per minimap line. Monaco: <c>samplingRatio</c>.</summary>
    public double SamplingRatio { get; }

    /// <summary>A map of 0-based minimap line indexes to 1-based view line numbers. Monaco: <c>minimapLines</c>.</summary>
    public IReadOnlyList<int> MinimapLines => _minimapLines;

    private static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

    /// <summary>Computes the sample, reusing <paramref name="oldSamplingState"/> where possible. Monaco: <c>compute</c>.</summary>
    public static (MinimapSamplingState? State, IReadOnlyList<SamplingStateEvent> Events) Compute(
        MinimapOptions options, int viewLineCount, MinimapSamplingState? oldSamplingState)
    {
        if (options.RenderMinimap == RenderMinimap.None || !options.IsSampling)
        {
            return (null, []);
        }

        // ratio is intentionally not part of the layout to avoid the layout changing all the time
        // so we need to recompute it again...
        int minimapLineCount = (int)EditorLayoutInfoComputer.ComputeContainedMinimapLineCount(
            viewLineCount, options.ScrollBeyondLastLine, options.PaddingTop, options.PaddingBottom,
            options.EditorHeight, options.LineHeight, options.PixelRatio).MinimapLineCount;
        double ratio = (double)viewLineCount / minimapLineCount;
        double halfRatio = ratio / 2;

        if (oldSamplingState is null || oldSamplingState._minimapLines.Length == 0)
        {
            var first = new int[Math.Max(minimapLineCount, 1)];
            first[0] = 1;
            if (minimapLineCount > 1)
            {
                for (int i = 0, lastIndex = minimapLineCount - 1; i < lastIndex; i++)
                {
                    first[i] = Round(i * ratio + halfRatio);
                }

                first[minimapLineCount - 1] = viewLineCount;
            }

            return (new MinimapSamplingState(ratio, first), []);
        }

        int[] oldMinimapLines = oldSamplingState._minimapLines;
        int oldLength = oldMinimapLines.Length;
        var result = new int[minimapLineCount];
        int oldIndex = 0;
        int oldDeltaLineCount = 0;
        int minViewLineNumber = 1;
        var events = new List<SamplingStateEvent>();
        SamplingStateEvent? lastEvent = null;
        for (int i = 0; i < minimapLineCount; i++)
        {
            int fromViewLineNumber = Math.Max(minViewLineNumber, Round(i * ratio));
            int toViewLineNumber = Math.Max(fromViewLineNumber, Round((i + 1) * ratio));

            while (oldIndex < oldLength && oldMinimapLines[oldIndex] < fromViewLineNumber)
            {
                if (events.Count < MaxEventCount)
                {
                    AddDeleted(events, ref lastEvent, oldIndex, oldIndex + 1 + oldDeltaLineCount);
                    oldDeltaLineCount--;
                }

                oldIndex++;
            }

            int selectedViewLineNumber;
            if (oldIndex < oldLength && oldMinimapLines[oldIndex] <= toViewLineNumber)
            {
                // reuse the old sampled line
                selectedViewLineNumber = oldMinimapLines[oldIndex];
                oldIndex++;
            }
            else
            {
                if (i == 0)
                {
                    selectedViewLineNumber = 1;
                }
                else if (i + 1 == minimapLineCount)
                {
                    selectedViewLineNumber = viewLineCount;
                }
                else
                {
                    selectedViewLineNumber = Round(i * ratio + halfRatio);
                }

                if (events.Count < MaxEventCount)
                {
                    int oldMinimapLineNumber = oldIndex + 1 + oldDeltaLineCount;
                    if (lastEvent is SamplingStateLinesInserted inserted && inserted.I == i - 1)
                    {
                        // Monaco keeps `_i` unchanged when extending, so a third consecutive insert starts a new event.
                        lastEvent = inserted with { InsertToLineNumber = inserted.InsertToLineNumber + 1 };
                        events[^1] = lastEvent;
                    }
                    else
                    {
                        lastEvent = new SamplingStateLinesInserted(oldMinimapLineNumber, oldMinimapLineNumber) { I = i };
                        events.Add(lastEvent);
                    }

                    oldDeltaLineCount++;
                }
            }

            result[i] = selectedViewLineNumber;
            minViewLineNumber = selectedViewLineNumber;
        }

        if (events.Count < MaxEventCount)
        {
            while (oldIndex < oldLength)
            {
                AddDeleted(events, ref lastEvent, oldIndex, oldIndex + 1 + oldDeltaLineCount);
                oldDeltaLineCount--;
                oldIndex++;
            }
        }
        else
        {
            // too many events, just give up
            events = [new SamplingStateFlush()];
        }

        return (new MinimapSamplingState(ratio, result), events);
    }

    // Extends the last 'deleted' event when it starts at the previous old index, else starts a new one (Monaco keeps `_oldIndex`).
    private static void AddDeleted(List<SamplingStateEvent> events, ref SamplingStateEvent? lastEvent, int oldIndex, int oldMinimapLineNumber)
    {
        if (lastEvent is SamplingStateLinesDeleted deleted && deleted.OldIndex == oldIndex - 1)
        {
            lastEvent = deleted with { DeleteToLineNumber = deleted.DeleteToLineNumber + 1 };
            events[^1] = lastEvent;
        }
        else
        {
            lastEvent = new SamplingStateLinesDeleted(oldMinimapLineNumber, oldMinimapLineNumber) { OldIndex = oldIndex };
            events.Add(lastEvent);
        }
    }

    /// <summary>Monaco: <c>modelLineToMinimapLine</c>.</summary>
    public int ModelLineToMinimapLine(int lineNumber)
    {
        return Math.Min(_minimapLines.Length, Math.Max(1, Round(lineNumber / SamplingRatio)));
    }

    /// <summary>Null if the model line range does not intersect a sampled model line. Monaco: <c>modelLineRangeToMinimapLineRange</c>.</summary>
    public (int From, int To)? ModelLineRangeToMinimapLineRange(int fromLineNumber, int toLineNumber)
    {
        int fromLineIndex = ModelLineToMinimapLine(fromLineNumber) - 1;
        while (fromLineIndex > 0 && _minimapLines[fromLineIndex - 1] >= fromLineNumber)
        {
            fromLineIndex--;
        }

        int toLineIndex = ModelLineToMinimapLine(toLineNumber) - 1;
        while (toLineIndex + 1 < _minimapLines.Length && _minimapLines[toLineIndex + 1] <= toLineNumber)
        {
            toLineIndex++;
        }

        if (fromLineIndex == toLineIndex)
        {
            int sampledLineNumber = _minimapLines[fromLineIndex];
            if (sampledLineNumber < fromLineNumber || sampledLineNumber > toLineNumber)
            {
                // This line is not part of the sampled lines ==> nothing to do
                return null;
            }
        }

        return (fromLineIndex + 1, toLineIndex + 1);
    }

    /// <summary>Always returns a range, even if it does not intersect a sampled model line. Monaco: <c>decorationLineRangeToMinimapLineRange</c>.</summary>
    public (int Start, int End) DecorationLineRangeToMinimapLineRange(int startLineNumber, int endLineNumber)
    {
        int minimapLineStart = ModelLineToMinimapLine(startLineNumber);
        int minimapLineEnd = ModelLineToMinimapLine(endLineNumber);
        if (startLineNumber != endLineNumber && minimapLineEnd == minimapLineStart)
        {
            if (minimapLineEnd == _minimapLines.Length)
            {
                if (minimapLineStart > 1)
                {
                    minimapLineStart--;
                }
            }
            else
            {
                minimapLineEnd++;
            }
        }

        return (minimapLineStart, minimapLineEnd);
    }

    /// <summary>Returns the changed 0-based minimap line index range. Monaco: <c>onLinesDeleted</c>.</summary>
    public (int ChangeStartIndex, int ChangeEndIndex) OnLinesDeleted(int fromLineNumber, int toLineNumber)
    {
        // have the mapping be sticky
        int deletedLineCount = toLineNumber - fromLineNumber + 1;
        int changeStartIndex = _minimapLines.Length;
        int changeEndIndex = 0;
        for (int i = _minimapLines.Length - 1; i >= 0; i--)
        {
            if (_minimapLines[i] < fromLineNumber)
            {
                break;
            }

            if (_minimapLines[i] <= toLineNumber)
            {
                // this line got deleted => move to previous available
                _minimapLines[i] = Math.Max(1, fromLineNumber - 1);
                changeStartIndex = Math.Min(changeStartIndex, i);
                changeEndIndex = Math.Max(changeEndIndex, i);
            }
            else
            {
                _minimapLines[i] -= deletedLineCount;
            }
        }

        return (changeStartIndex, changeEndIndex);
    }

    /// <summary>Monaco: <c>onLinesInserted</c>.</summary>
    public void OnLinesInserted(int fromLineNumber, int toLineNumber)
    {
        // have the mapping be sticky
        int insertedLineCount = toLineNumber - fromLineNumber + 1;
        for (int i = _minimapLines.Length - 1; i >= 0; i--)
        {
            if (_minimapLines[i] < fromLineNumber)
            {
                break;
            }

            _minimapLines[i] += insertedLineCount;
        }
    }
}
