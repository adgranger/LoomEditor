// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/foldingModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;

namespace LoomEditor.View.Features.Folding;

/// <summary>Creates and changes the model decorations of the folding regions (Monaco: <c>IDecorationProvider</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface IDecorationProvider
{
    ModelDecorationOptions GetDecorationOption(bool isCollapsed, bool isHidden, bool isManual);

    T ChangeDecorations<T>(Func<IModelDecorationsChangeAccessor, T> callback);

    void RemoveDecorations(IReadOnlyList<string> decorationIds);
}

/// <summary>Raised by <see cref="FoldingModel.Changed"/> (Monaco: <c>FoldingModelChangeEvent</c>).</summary>
/// <param name="Model">The folding model.</param>
/// <param name="CollapseStateChanged">The toggled regions, or <see langword="null"/> when the regions were replaced.</param>
internal sealed record FoldingModelChangeEvent(FoldingModel Model, IReadOnlyList<FoldingRegion>? CollapseStateChanged = null);
