// Ported from microsoft/vscode: src/vs/editor/common/model/decorationProvider.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Decorations;

/// <summary>
/// A source of decorations that the model adds to its own (Monaco: <c>DecorationProvider</c>). The font and minimap
/// filters of Monaco are not ported; the owner id is never <c>undefined</c>, so that check is not ported either.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface IDecorationProvider
{
    /// <summary>The decorations that intersect <paramref name="range"/>.</summary>
    IReadOnlyList<ModelDecoration> GetDecorationsInRange(Range range, int ownerId, bool filterOutValidation);

    /// <summary>All the decorations of the provider.</summary>
    IReadOnlyList<ModelDecoration> GetAllDecorations(int ownerId, bool filterOutValidation);
}
