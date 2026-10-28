// Ported from microsoft/vscode: src/vs/editor/contrib/smartSelect/browser/smartSelect.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Features.SmartSelect;

/// <summary>
/// Gives, for each position, the ranges around it (Monaco: <c>SelectionRangeProvider</c>, with <c>SelectionRange</c> reduced to
/// <see cref="Range"/>). Synchronous: the only providers are the local word and bracket ones.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface ISelectionRangeProvider
{
    /// <summary>One list of ranges per position, in the order of <paramref name="positions"/>; each list goes from the innermost range to the outermost.</summary>
    IReadOnlyList<IReadOnlyList<Range>> ProvideSelectionRanges(TextModel model, IReadOnlyList<Position> positions);
}
