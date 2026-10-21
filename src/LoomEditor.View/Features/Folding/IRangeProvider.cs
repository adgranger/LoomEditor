// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/folding.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Folding;

/// <summary>
/// Computes the folding ranges of a model (Monaco: <c>RangeProvider</c>). Synchronous: Monaco returns a <c>Promise</c> and takes a
/// cancellation token, but the indentation provider resolves at once.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface IRangeProvider : IDisposable
{
    string Id { get; }

    FoldingRegions? Compute();
}

/// <summary>Tells the user that the number of ranges was limited (Monaco: <c>FoldingLimitReporter</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface IFoldingLimitReporter
{
    int Limit { get; }

    /// <param name="computed">The number of ranges found.</param>
    /// <param name="limited">The limit when the ranges were limited, else <see langword="null"/> (Monaco: <c>false</c>).</param>
    void Update(int computed, int? limited);
}