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
/// Monaco: <c>DecorationsTrees</c>. Monaco has three trees (normal, overview ruler, injected text); phase 3 only
/// needs the first one because tracked ranges are neither in the overview ruler nor injected text. The other two
/// come with decorations.
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

    public void AcceptReplace(long offset, long length, long textLength, bool forceMoveMarkers) =>
        _tree.AcceptReplace(offset, length, textLength, forceMoveMarkers);
}
