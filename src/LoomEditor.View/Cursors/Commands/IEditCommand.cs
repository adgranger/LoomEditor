// Ported from microsoft/vscode: src/vs/editor/common/editorCommon.ts (ICommand)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors.Commands;

/// <summary>A command that edits the text and tells where the cursor goes (Monaco: <c>ICommand</c>).</summary>
/// <remarks>Completed in P3-14. Only declared here because <see cref="EditOperationResult"/> refers to it.</remarks>
internal interface IEditCommand
{
}
