// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/diffEditorDecorations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;

namespace LoomEditor.View.DiffEditor;

/// <summary>Where the decorations of one side of the diff are applied (implemented by the control over an editor decorations collection).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface IDiffDecorationTarget
{
    /// <summary>Replaces all the decorations previously set.</summary>
    void SetDecorations(IReadOnlyList<ModelDeltaDecoration> decorations);
}

/// <summary>
/// Keeps the diff decorations applied to the two models (Monaco: <c>DiffEditorDecorations</c> with
/// <c>applyObservableDecorations</c>). The owner calls <see cref="Update"/> when the diff or an option changes.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DiffDecorations(IDiffDecorationTarget original, IDiffDecorationTarget modified)
{
    /// <summary>Recomputes and sets both sides; a <c>null</c> diff clears them.</summary>
    public void Update(DiffState? diff, DiffEditorOptions options, int originalLineCount, int modifiedLineCount)
    {
        if (diff is null)
        {
            original.SetDecorations([]);
            modified.SetDecorations([]);
            return;
        }

        var (o, m) = DiffDecorationsComputer.Compute(diff, options.RenderIndicators, options.ShowEmptyDecorations, originalLineCount, modifiedLineCount);
        original.SetDecorations(o);
        modified.SetDecorations(m);
    }
}
