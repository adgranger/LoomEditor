// Ported from microsoft/vscode: src/vs/editor/common/viewModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// An accessor that allows for whitespace to be added, removed or changed in bulk.
/// </summary>
/// <remarks>Threading: only valid inside the callback it is passed to; same thread as the owning <c>LinesLayout</c>.</remarks>
internal interface IWhitespaceChangeAccessor
{
    /// <summary>Inserts a whitespace and returns its id.</summary>
    string InsertWhitespace(int afterLineNumber, int ordinal, int heightInPx, int minWidth);

    /// <summary>Changes the position and height of an existing whitespace.</summary>
    void ChangeOneWhitespace(string id, int newAfterLineNumber, int newHeight);

    /// <summary>Removes a whitespace.</summary>
    void RemoveWhitespace(string id);
}
