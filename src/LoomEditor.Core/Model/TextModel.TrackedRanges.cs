// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using LoomEditor.Core.Decorations;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model;

// Tracked ranges: ranges of the model that move with the text when it is edited. The cursor stores its selections as
// tracked ranges. Phase 3 has no public decorations API: only the tracked-range path of Monaco's _deltaDecorationsImpl.
public sealed partial class TextModel
{
    private static int _nextInstanceId;

    // Monaco: TRACKED_RANGE_OPTIONS (indexed by stickiness)
    private static readonly ModelDecorationOptions[] TrackedRangeOptions =
    [
        new() { Stickiness = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges },
        new() { Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges },
        new() { Stickiness = TrackedRangeStickiness.GrowsOnlyWhenTypingBefore },
        new() { Stickiness = TrackedRangeStickiness.GrowsOnlyWhenTypingAfter },
    ];

    private readonly int _instanceId = Interlocked.Increment(ref _nextInstanceId);
    private readonly Dictionary<string, IntervalNode> _decorations = [];
    private DecorationsTrees _decorationsTree = new();
    private int _lastDecorationId;

    /// <summary>The current range of a tracked range, or <see langword="null"/> when the id is unknown (Monaco: <c>_getTrackedRange</c>).</summary>
    internal Range? GetTrackedRange(string id)
    {
        // Monaco: getDecorationRange(id)
        return _decorations.TryGetValue(id, out IntervalNode? node) ? _decorationsTree.GetNodeRange(this, node) : null;
    }

    /// <summary>
    /// Creates, moves or deletes a tracked range (Monaco: <c>_setTrackedRange</c>).
    /// </summary>
    /// <param name="id">The id of an existing tracked range, or <see langword="null"/> to create one.</param>
    /// <param name="newRange">The range to track, or <see langword="null"/> to delete the tracked range.</param>
    /// <param name="stickiness">How the range grows when text is typed at its edges.</param>
    /// <returns>The id of the tracked range, or <see langword="null"/> when it was deleted (or never existed).</returns>
    internal string? SetTrackedRange(string? id, Range? newRange, TrackedRangeStickiness stickiness)
    {
        IntervalNode? node = null;
        if (id is not null)
        {
            _decorations.TryGetValue(id, out node);
        }

        if (node is null)
        {
            if (newRange is null)
            {
                // node doesn't exist, the request is to delete => nothing to do
                return null;
            }

            // node doesn't exist, the request is to set => add the tracked range
            // Monaco: _deltaDecorationsImpl(0, [], [{ range, options }], true), the part that creates a node
            string decorationId = string.Create(CultureInfo.InvariantCulture, $"{_instanceId};{++_lastDecorationId}");
            node = new IntervalNode(decorationId, 0, 0);
            _decorations[decorationId] = node;

            Range validated = ValidateRangeRelaxed(newRange.Value);
            int start = _buffer.GetOffsetAt(validated.StartLineNumber, validated.StartColumn);
            int end = _buffer.GetOffsetAt(validated.EndLineNumber, validated.EndColumn);

            node.OwnerId = 0;
            node.Reset(_versionId, start, end, validated);
            node.SetOptions(TrackedRangeOptions[(int)stickiness]);
            _decorationsTree.Insert(node);
            return node.Id;
        }

        if (newRange is null)
        {
            // node exists, the request is to delete => delete node
            _decorationsTree.Delete(node);
            _decorations.Remove(node.Id);
            return null;
        }

        // node exists, the request is to set => change the tracked range and its options
        Range range = ValidateRangeRelaxed(newRange.Value);
        int startOffset = _buffer.GetOffsetAt(range.StartLineNumber, range.StartColumn);
        int endOffset = _buffer.GetOffsetAt(range.EndLineNumber, range.EndColumn);
        _decorationsTree.Delete(node);
        node.Reset(_versionId, startOffset, endOffset, range);
        node.SetOptions(TrackedRangeOptions[(int)stickiness]);
        _decorationsTree.Insert(node);
        return node.Id;
    }

    /// <summary>
    /// Clamps a range to the text like <see cref="ValidateRange(Range)"/>, but a position inside a surrogate pair is
    /// allowed (Monaco: <c>_validateRangeRelaxedNoAllocations</c>).
    /// </summary>
    internal Range ValidateRangeRelaxed(Range range)
    {
        // Difference from Monaco: a Range cannot hold NaN or values below 1, so only the clamping part remains.
        Position start = ValidatePositionCore(range.StartLineNumber, range.StartColumn, StringOffsetValidationType.Relaxed);
        Position end = ValidatePositionCore(range.EndLineNumber, range.EndColumn, StringOffsetValidationType.Relaxed);
        return range.Start == start && range.End == end ? range : new Range(start, end);
    }

    /// <summary>The range between two offsets (Monaco: <c>getRangeAt</c>).</summary>
    internal Range GetRangeAt(int start, int end) => _buffer.GetRangeAt(start, end - start);

    // Monaco: _setValueFromTextBuffer, "Destroy all my decorations"
    private void DestroyDecorations()
    {
        _decorations.Clear();
        _decorationsTree = new DecorationsTrees();
    }
}
