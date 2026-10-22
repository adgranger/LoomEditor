// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using LoomEditor.Core.Decorations;
using LoomEditor.Core.Diagnostics;
using LoomEditor.Core.Model.Events;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model;

// Decorations: ranges with options that move with the text. Tracked ranges (TextModel.TrackedRanges.cs) are
// decorations of owner 0. One interval tree holds them all: Monaco's overview ruler and injected text trees,
// line heights and font decorations are not ported (ADR 0009, decision 3). Behavior differences: an exception
// thrown by the ChangeDecorations callback propagates (Monaco reports it and returns null).
public sealed partial class TextModel
{
    private static int _nextInstanceId;

    private readonly int _instanceId = Interlocked.Increment(ref _nextInstanceId);
    private readonly Dictionary<string, IntervalNode> _decorations = [];
    private DecorationsTrees _decorationsTree = new();
    private int _lastDecorationId;
    private int _deltaDecorationCallCnt;

    private sealed class ChangeAccessor(TextModel model, int ownerId) : IModelDecorationsChangeAccessor
    {
        private bool _isValid = true;

        public void Invalidate() => _isValid = false;

        public string AddDecoration(Range range, ModelDecorationOptions options)
        {
            ThrowIfInvalid();
            return model.DeltaDecorationsImpl(ownerId, [], [new ModelDeltaDecoration(range, options)])[0];
        }

        public void ChangeDecoration(string id, Range newRange)
        {
            ThrowIfInvalid();
            model.ChangeDecorationImpl(id, newRange);
        }

        public void ChangeDecorationOptions(string id, ModelDecorationOptions options)
        {
            ThrowIfInvalid();
            model.ChangeDecorationOptionsImpl(id, options);
        }

        public void RemoveDecoration(string id)
        {
            ThrowIfInvalid();
            model.DeltaDecorationsImpl(ownerId, [id], []);
        }

        public string[] DeltaDecorations(IReadOnlyList<string> oldDecorationIds, IReadOnlyList<ModelDeltaDecoration> newDecorations)
        {
            ThrowIfInvalid();
            if (oldDecorationIds.Count == 0 && newDecorations.Count == 0)
            {
                // nothing to do
                return [];
            }

            return model.DeltaDecorationsImpl(ownerId, oldDecorationIds, newDecorations);
        }

        // Monaco: invalidFunc
        private void ThrowIfInvalid()
        {
            if (!_isValid)
            {
                throw new InvalidOperationException("The decorations change accessor can only be used inside the ChangeDecorations callback.");
            }
        }
    }

    /// <summary>Runs <paramref name="callback"/> with an accessor that can add, change and remove decorations of <paramref name="ownerId"/>.</summary>
    /// <remarks>The accessor is invalid once the callback returns. An exception of the callback propagates.</remarks>
    internal T ChangeDecorations<T>(int ownerId, Func<IModelDecorationsChangeAccessor, T> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        var accessor = new ChangeAccessor(this, ownerId);
        try
        {
            BeginDecorationsDeferredEmit();
            return callback(accessor);
        }
        finally
        {
            accessor.Invalidate();
            EndDecorationsDeferredEmit();
        }
    }

    /// <inheritdoc cref="ChangeDecorations{T}(int, Func{IModelDecorationsChangeAccessor, T})"/>
    internal void ChangeDecorations(int ownerId, Action<IModelDecorationsChangeAccessor> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ChangeDecorations(ownerId, accessor =>
        {
            callback(accessor);
            return 0;
        });
    }

    /// <summary>
    /// Replaces the decorations <paramref name="oldDecorationIds"/> of <paramref name="ownerId"/> with
    /// <paramref name="newDecorations"/> and returns the ids of the new ones.
    /// </summary>
    internal string[] DeltaDecorations(int ownerId, IReadOnlyList<string> oldDecorationIds, IReadOnlyList<ModelDeltaDecoration> newDecorations)
    {
        ArgumentNullException.ThrowIfNull(oldDecorationIds);
        ArgumentNullException.ThrowIfNull(newDecorations);
        if (oldDecorationIds.Count == 0 && newDecorations.Count == 0)
        {
            // nothing to do
            return [];
        }

        try
        {
            _deltaDecorationCallCnt++;
            Guard.Assert(_deltaDecorationCallCnt <= 1, "Invoking DeltaDecorations recursively could lead to leaking decorations.");
            BeginDecorationsDeferredEmit();
            return DeltaDecorationsImpl(ownerId, oldDecorationIds, newDecorations);
        }
        finally
        {
            EndDecorationsDeferredEmit();
            _deltaDecorationCallCnt--;
        }
    }

    /// <summary>Removes every decoration of <paramref name="ownerId"/> (owner 0: all of them, tracked ranges included).</summary>
    internal void RemoveAllDecorationsWithOwnerId(int ownerId)
    {
        // Monaco: `if (this._isDisposed) return;` is dropped, a TextModel has no Dispose.
        foreach (IntervalNode node in _decorationsTree.CollectNodesFromOwner(ownerId))
        {
            _decorationsTree.Delete(node);
            _decorations.Remove(node.Id);
        }
    }

    /// <summary>The options of a decoration, or <see langword="null"/> when the id is unknown.</summary>
    internal ModelDecorationOptions? GetDecorationOptions(string id) =>
        _decorations.TryGetValue(id, out IntervalNode? node) ? node.Options : null;

    /// <summary>The current range of a decoration, or <see langword="null"/> when the id is unknown.</summary>
    internal Range? GetDecorationRange(string id) =>
        _decorations.TryGetValue(id, out IntervalNode? node) ? _decorationsTree.GetNodeRange(this, node) : null;

    /// <summary>Every decoration that injects text (Monaco: <c>getInjectedTextDecorations</c>).</summary>
    /// <remarks>Monaco searches its injected-text tree; LoomEditor filters the single tree.</remarks>
    internal IReadOnlyList<ModelDecoration> GetInjectedTextDecorations(int ownerId = 0) =>
        _decorationsTree.GetAllInjectedText(this, ownerId);

    /// <summary>The text injected on a line, sorted by column (Monaco: <c>getLineInjectedText</c>).</summary>
    /// <remarks>Monaco searches its injected-text tree; LoomEditor filters the single tree.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lineNumber"/> is not a line of the model.</exception>
    internal IReadOnlyList<LineInjectedText> GetLineInjectedText(int lineNumber, int ownerId = 0)
    {
        AssertLineNumber(lineNumber);
        int startOffset = _buffer.GetOffsetAt(lineNumber, 1);
        int endOffset = startOffset + _buffer.GetLineLength(lineNumber);

        List<ModelDecoration> decorations = _decorationsTree.GetInjectedTextInInterval(this, startOffset, endOffset, ownerId);
        return LineInjectedText.FromDecorations(decorations).FindAll(t => t.LineNumber == lineNumber);
    }

    /// <summary>The decorations that touch a line; an out-of-range line gives an empty list.</summary>
    internal IReadOnlyList<ModelDecoration> GetLineDecorations(int lineNumber, int ownerId = 0, bool filterOutValidation = false, bool filterFontDecorations = false)
    {
        if (lineNumber < 1 || lineNumber > LineCount)
        {
            return [];
        }

        return GetLinesDecorations(lineNumber, lineNumber, ownerId, filterOutValidation, filterFontDecorations);
    }

    /// <summary>The decorations that touch the lines from <paramref name="startLineNumber"/> to <paramref name="endLineNumber"/> (clamped to the text).</summary>
    internal IReadOnlyList<ModelDecoration> GetLinesDecorations(int startLineNumber, int endLineNumber, int ownerId = 0, bool filterOutValidation = false, bool filterFontDecorations = false)
    {
        int lineCount = LineCount;
        int start = Math.Min(lineCount, Math.Max(1, startLineNumber));
        int end = Math.Min(lineCount, Math.Max(1, endLineNumber));
        var range = new Range(start, 1, end, GetLineMaxColumn(end));

        List<ModelDecoration> decorations = GetDecorationsInRangeCore(range, ownerId, filterOutValidation, filterFontDecorations);
        decorations.AddRange(_decorationProvider.GetDecorationsInRange(range, ownerId, filterOutValidation));
        return decorations;
    }

    /// <summary>The decorations that intersect <paramref name="range"/> (validated first).</summary>
    internal IReadOnlyList<ModelDecoration> GetDecorationsInRange(Range range, int ownerId = 0, bool filterOutValidation = false, bool filterFontDecorations = false)
    {
        Range validated = ValidateRange(range);

        List<ModelDecoration> decorations = GetDecorationsInRangeCore(validated, ownerId, filterOutValidation, filterFontDecorations);
        decorations.AddRange(_decorationProvider.GetDecorationsInRange(validated, ownerId, filterOutValidation));
        return decorations;
    }

    /// <summary>All the decorations of the model (tracked ranges included).</summary>
    internal IReadOnlyList<ModelDecoration> GetAllDecorations(int ownerId = 0, bool filterOutValidation = false, bool filterFontDecorations = false)
    {
        List<ModelDecoration> result = [.. _decorationsTree.GetAll(this, ownerId, filterOutValidation, filterFontDecorations, onlyMarginDecorations: false)];
        result.AddRange(_decorationProvider.GetAllDecorations(ownerId, filterOutValidation));
        return result;
    }

    // Monaco: _getDecorationsInRange
    private List<ModelDecoration> GetDecorationsInRangeCore(Range filterRange, int ownerId, bool filterOutValidation, bool filterFontDecorations)
    {
        int startOffset = _buffer.GetOffsetAt(filterRange.StartLineNumber, filterRange.StartColumn);
        int endOffset = _buffer.GetOffsetAt(filterRange.EndLineNumber, filterRange.EndColumn);
        return _decorationsTree.GetAllInInterval(this, startOffset, endOffset, ownerId, filterOutValidation, filterFontDecorations, onlyMarginDecorations: false);
    }

    // Monaco: _changeDecorationImpl, without the line-affected records.
    private void ChangeDecorationImpl(string decorationId, Range newRange)
    {
        if (!_decorations.TryGetValue(decorationId, out IntervalNode? node))
        {
            return;
        }

        Range range = ValidateRangeRelaxed(newRange);
        int startOffset = _buffer.GetOffsetAt(range.StartLineNumber, range.StartColumn);
        int endOffset = _buffer.GetOffsetAt(range.EndLineNumber, range.EndColumn);

        _decorationsTree.Delete(node);
        node.Reset(_versionId, startOffset, endOffset, range);
        _decorationsTree.Insert(node);
        CheckAffectedAndFireDecorations(node.Options);
    }

    // Monaco: _changeDecorationOptionsImpl. Monaco moves the node between its trees when it enters or leaves the
    // overview ruler or injected text, which cannot happen with one tree: the options are always set in place.
    private void ChangeDecorationOptionsImpl(string decorationId, ModelDecorationOptions options)
    {
        if (_decorations.TryGetValue(decorationId, out IntervalNode? node))
        {
            CheckAffectedAndFireDecorations(node.Options);
            CheckAffectedAndFireDecorations(options);
            node.SetOptions(options);
        }
    }

    // Monaco: _deltaDecorationsImpl (the deferral part), without the line-affected records.
    private string[] DeltaDecorationsImpl(int ownerId, IReadOnlyList<string> oldDecorationIds, IReadOnlyList<ModelDeltaDecoration> newDecorations, bool suppressEvents = false)
    {
        BeginDecorationsDeferredEmit();
        try
        {
            return DeltaDecorationsCore(ownerId, oldDecorationIds, newDecorations, suppressEvents);
        }
        finally
        {
            EndDecorationsDeferredEmit();
        }
    }

    private string[] DeltaDecorationsCore(int ownerId, IReadOnlyList<string> oldDecorationIds, IReadOnlyList<ModelDeltaDecoration> newDecorations, bool suppressEvents)
    {
        int versionId = _versionId;

        int oldDecorationsLen = oldDecorationIds.Count;
        int oldDecorationIndex = 0;

        int newDecorationsLen = newDecorations.Count;
        int newDecorationIndex = 0;

        var result = new string[newDecorationsLen];
        while (oldDecorationIndex < oldDecorationsLen || newDecorationIndex < newDecorationsLen)
        {
            IntervalNode? node = null;

            if (oldDecorationIndex < oldDecorationsLen)
            {
                // (1) get ourselves an old node
                do
                {
                    _decorations.TryGetValue(oldDecorationIds[oldDecorationIndex++], out node);
                }
                while (node is null && oldDecorationIndex < oldDecorationsLen);

                // (2) remove the node from the tree (if it exists)
                if (node is not null)
                {
                    _decorationsTree.Delete(node);

                    if (!suppressEvents)
                    {
                        CheckAffectedAndFireDecorations(node.Options);
                    }
                }
            }

            if (newDecorationIndex < newDecorationsLen)
            {
                // (3) create a new node if necessary
                if (node is null)
                {
                    string decorationId = string.Create(CultureInfo.InvariantCulture, $"{_instanceId};{++_lastDecorationId}");
                    node = new IntervalNode(decorationId, 0, 0);
                    _decorations[decorationId] = node;
                }

                // (4) initialize node
                ModelDeltaDecoration newDecoration = newDecorations[newDecorationIndex];
                Range range = ValidateRangeRelaxed(newDecoration.Range);
                int startOffset = _buffer.GetOffsetAt(range.StartLineNumber, range.StartColumn);
                int endOffset = _buffer.GetOffsetAt(range.EndLineNumber, range.EndColumn);

                node.OwnerId = ownerId;
                node.Reset(versionId, startOffset, endOffset, range);
                node.SetOptions(newDecoration.Options);
                if (!suppressEvents)
                {
                    CheckAffectedAndFireDecorations(newDecoration.Options);
                }

                _decorationsTree.Insert(node);

                result[newDecorationIndex] = node.Id;

                newDecorationIndex++;
            }
            else if (node is not null)
            {
                _decorations.Remove(node.Id);
            }
        }

        return result;
    }
}
