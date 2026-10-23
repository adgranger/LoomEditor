// Ported from microsoft/vscode: src/vs/editor/contrib/stickyScroll/browser/stickyScrollModelProvider.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.View.Features.Folding;

namespace LoomEditor.View.Features.StickyScroll;

/// <summary>
/// Builds the sticky scope tree of a text model (Monaco: <c>StickyModelProvider</c>). Only the indentation model exists until
/// phase 8, so whatever <see cref="EditorStickyScrollOptions.DefaultModel"/> is, the chain ends on it. Synchronous: Monaco's
/// promises and cancellation tokens are dropped.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class StickyModelProvider : IDisposable
{
    private readonly TextModel _model;
    private readonly IndentRangeProvider _provider;

    /// <param name="model">The text model to read.</param>
    /// <param name="limitReporter">Reports the folding regions limit (Monaco: a <c>RangesLimitReporter</c> on the same option).</param>
    public StickyModelProvider(TextModel model, RangesLimitReporter limitReporter)
    {
        _model = model;
        _provider = new IndentRangeProvider(model, model.LanguageConfigurationRegistry, limitReporter);
    }

    /// <summary>Computes the sticky model of the current text, or <see langword="null"/> when no regions could be computed.</summary>
    public StickyModel? Update()
    {
        // Phase 8: outline and folding range providers
        FoldingRegions? regions = _provider.Compute();
        return regions is null ? null : new StickyModel(_model.VersionId, FromFoldingRegions(regions));
    }

    /// <summary>Monaco: <c>_fromFoldingRegions</c>. Each range ends one line after its folding region.</summary>
    internal static StickyElement FromFoldingRegions(FoldingRegions foldingRegions)
    {
        int length = foldingRegions.Length;
        var orderedStickyElements = new List<StickyElement>();
        var root = new StickyElement(null, [], null);

        for (int i = 0; i < length; i++)
        {
            int parentIndex = foldingRegions.GetParentIndex(i);
            StickyElement parentNode = parentIndex != -1 ? orderedStickyElements[parentIndex] : root;
            var child = new StickyElement(
                new StickyRange(foldingRegions.GetStartLineNumber(i), foldingRegions.GetEndLineNumber(i) + 1), [], parentNode);
            parentNode.Children.Add(child);
            orderedStickyElements.Add(child);
        }

        return root;
    }

    public void Dispose() => _provider.Dispose();
}
