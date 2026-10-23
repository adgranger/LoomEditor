// Ported from microsoft/vscode: src/vs/editor/contrib/hover/browser/hoverOperation.ts (IHoverComputer)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Hover;

/// <summary>Computes the parts of a hover: asynchronously (providers) and synchronously (markers).</summary>
/// <remarks>Threading: both methods are called on the UI thread; the task may complete anywhere.</remarks>
internal interface IHoverComputer
{
    /// <summary>Called after half the hover delay; null when nothing is asynchronous.</summary>
    Task<IReadOnlyList<IHoverPart>>? ComputeAsync(HoverComputerOptions options, CancellationToken token);

    /// <summary>Called after the whole hover delay.</summary>
    IReadOnlyList<IHoverPart> ComputeSync(HoverComputerOptions options);
}
