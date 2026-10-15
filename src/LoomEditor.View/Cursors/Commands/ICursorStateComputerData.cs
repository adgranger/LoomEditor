// Ported from microsoft/vscode: src/vs/editor/common/editorCommon.ts (ICursorStateComputerData)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors.Commands;

/// <summary>What an <see cref="IEditCommand"/> reads to compute the cursor state after its edits (Monaco: <c>ICursorStateComputerData</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface ICursorStateComputerData
{
    /// <summary>The inverse of the edits of this command, ordered like the edits that were added.</summary>
    IReadOnlyList<ValidEditOperation> GetInverseEditOperations();

    /// <summary>The selection tracked with <see cref="IEditOperationBuilder.TrackSelection"/>, moved by the edits.</summary>
    Selection GetTrackedSelection(string id);
}
