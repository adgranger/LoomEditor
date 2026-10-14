// Ported from microsoft/vscode: src/vs/editor/common/viewModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// An accessor that allows custom line heights to be inserted, changed or removed in bulk.
/// </summary>
/// <remarks>Threading: only valid inside the callback it is passed to; same thread as the owning <c>LinesLayout</c>.</remarks>
internal interface ILineHeightChangeAccessor
{
    /// <summary>Inserts or changes the custom line height identified by <paramref name="decorationId"/>.</summary>
    void InsertOrChangeCustomLineHeight(string decorationId, int startLineNumber, int endLineNumber, int lineHeight);

    /// <summary>Removes the custom line height identified by <paramref name="decorationId"/>.</summary>
    void RemoveCustomLineHeight(string decorationId);
}
