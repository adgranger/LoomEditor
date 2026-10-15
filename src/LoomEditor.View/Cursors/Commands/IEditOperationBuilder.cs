// Ported from microsoft/vscode: src/vs/editor/common/editorCommon.ts (IEditOperationBuilder)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors.Commands;

/// <summary>What an <see cref="IEditCommand"/> uses to describe its edits (Monaco: <c>IEditOperationBuilder</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface IEditOperationBuilder
{
    /// <summary>Adds an edit operation to the command; a <see langword="null"/> text deletes the range.</summary>
    void AddEditOperation(Range range, string? text, bool forceMoveMarkers = false);

    /// <summary>Like <see cref="AddEditOperation"/>, and tells the model that the edit is tracked (it is never merged with other edits).</summary>
    void AddTrackedEditOperation(Range range, string? text, bool forceMoveMarkers = false);

    /// <summary>
    /// Tracks a selection so that it can be read back after the edits with
    /// <see cref="ICursorStateComputerData.GetTrackedSelection"/>.
    /// </summary>
    /// <param name="selection">The selection to track.</param>
    /// <param name="trackPreviousOnEmpty">
    /// For an empty selection: <see langword="true"/> to stay before text typed at the position, <see langword="false"/>
    /// to stay after it, <see langword="null"/> to decide from the surroundings (Monaco: <c>undefined</c>).
    /// </param>
    /// <returns>An id to give to <see cref="ICursorStateComputerData.GetTrackedSelection"/>.</returns>
    string TrackSelection(Selection selection, bool? trackPreviousOnEmpty = null);
}
