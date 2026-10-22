// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Decorations;

/// <summary>
/// The interval trees of a <see cref="TextModel"/> that hold its decorations and tracked ranges.
/// </summary>
/// <remarks>
/// <para>
/// Monaco: <c>DecorationsTrees</c>. Monaco has three trees (normal, overview ruler, injected text); LoomEditor only
/// needs the first one: the overview ruler and injected text are not ported (ADR 0009, decision 3). The other two
/// trees do not exist.
/// </para>
/// <para>Threading: not thread-safe; owned by one text model.</para>
/// </remarks>
internal sealed class DecorationsTrees
{
    private readonly IntervalTree _tree = new();

    public void Insert(IntervalNode node) => _tree.Insert(node);

    public void Delete(IntervalNode node) => _tree.Delete(node);

    /// <summary>The range of a node at the current version of <paramref name="host"/>, cached on the node.</summary>
    public Range GetNodeRange(TextModel host, IntervalNode node)
    {
        int versionId = host.VersionId;
        if (node.CachedVersionId != versionId)
        {
            _tree.ResolveNode(node, versionId);
        }

        node.Range ??= host.GetRangeAt((int)node.CachedAbsoluteStart, (int)node.CachedAbsoluteEnd);
        return node.Range.Value;
    }

    /// <summary>The decorations that intersect [<paramref name="start"/>, <paramref name="end"/>] (Monaco: <c>getAllInInterval</c>).</summary>
    public List<ModelDecoration> GetAllInInterval(
        TextModel host,
        long start,
        long end,
        int filterOwnerId,
        bool filterOutValidation,
        bool filterFontDecorations,
        bool onlyMarginDecorations)
    {
        List<IntervalNode> nodes = _tree.IntervalSearch(start, end, filterOwnerId, filterOutValidation, filterFontDecorations, host.VersionId, onlyMarginDecorations);
        return ToDecorations(host, nodes);
    }

    /// <summary>All the decorations that pass the filters (Monaco: <c>getAll</c>, without <c>overviewRulerOnly</c>).</summary>
    public List<ModelDecoration> GetAll(TextModel host, int filterOwnerId, bool filterOutValidation, bool filterFontDecorations, bool onlyMarginDecorations)
    {
        List<IntervalNode> nodes = _tree.Search(filterOwnerId, filterOutValidation, filterFontDecorations, host.VersionId, onlyMarginDecorations);
        return ToDecorations(host, nodes);
    }

    /// <summary>
    /// The injected-text decorations that intersect [<paramref name="start"/>, <paramref name="end"/>] (Monaco: <c>getInjectedTextInInterval</c>).
    /// Monaco searches its injected-text tree; LoomEditor filters the single tree.
    /// </summary>
    public List<ModelDecoration> GetInjectedTextInInterval(TextModel host, long start, long end, int filterOwnerId) =>
        KeepVisibleInjectedText(GetAllInInterval(host, start, end, filterOwnerId, false, false, false));

    /// <summary>All the injected-text decorations (Monaco: <c>getAllInjectedText</c>); filters the single tree.</summary>
    public List<ModelDecoration> GetAllInjectedText(TextModel host, int filterOwnerId) =>
        KeepVisibleInjectedText(GetAll(host, filterOwnerId, false, false, false));

    private static List<ModelDecoration> KeepVisibleInjectedText(List<ModelDecoration> decorations) =>
        decorations.FindAll(static d => d.Options.HasInjectedText && (d.Options.ShowIfCollapsed || !d.Range.IsEmpty));

    /// <summary>The nodes of an owner (all nodes for owner 0); the cached offsets are not set.</summary>
    public List<IntervalNode> CollectNodesFromOwner(int ownerId) => _tree.CollectNodesFromOwner(ownerId);

    // Monaco: _ensureNodesHaveRanges; the result holds snapshots, not the nodes themselves.
    private static List<ModelDecoration> ToDecorations(TextModel host, List<IntervalNode> nodes)
    {
        var result = new List<ModelDecoration>(nodes.Count);
        foreach (IntervalNode node in nodes)
        {
            node.Range ??= host.GetRangeAt((int)node.CachedAbsoluteStart, (int)node.CachedAbsoluteEnd);
            result.Add(new ModelDecoration(node.Id, node.OwnerId, node.Range.Value, node.Options));
        }

        return result;
    }

    public void AcceptReplace(long offset, long length, long textLength, bool forceMoveMarkers) =>
        _tree.AcceptReplace(offset, length, textLength, forceMoveMarkers);

    /// <summary>Gives every node its <c>Range</c> at the current version (Monaco: <c>ensureAllNodesHaveRanges</c>).</summary>
    public void EnsureAllNodesHaveRanges(TextModel host)
    {
        foreach (IntervalNode node in _tree.Search(0, false, false, host.VersionId, false))
        {
            node.Range ??= host.GetRangeAt((int)node.CachedAbsoluteStart, (int)node.CachedAbsoluteEnd);
        }
    }

    /// <summary>
    /// Transforms back the ranges of the nodes to offsets, after an EOL change (Monaco: the body of
    /// <c>_onAfterEOLChange</c>, which reaches into the tree).
    /// </summary>
    /// <param name="versionId">The version id of the model after the change.</param>
    /// <param name="getOffsetAt">The offset of a line and column, with the new EOL.</param>
    public void RecomputeOffsetsFromRanges(int versionId, Func<int, int, int> getOffsetAt)
    {
        foreach (IntervalNode node in _tree.CollectNodesPostOrder())
        {
            Range range = node.Range!.Value; // the range is defined due to EnsureAllNodesHaveRanges

            long delta = node.CachedAbsoluteStart - node.Start;

            int startOffset = getOffsetAt(range.StartLineNumber, range.StartColumn);
            int endOffset = getOffsetAt(range.EndLineNumber, range.EndColumn);

            node.CachedAbsoluteStart = startOffset;
            node.CachedAbsoluteEnd = endOffset;
            node.CachedVersionId = versionId;

            node.Start = startOffset - delta;
            node.End = endOffset - delta;

            _tree.RecomputeMaxEnd(node);
        }
    }
}
