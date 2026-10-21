// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts (IEditorCommentsOptions, EditorComments)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Comment;

/// <summary>The <c>editor.comments</c> options.</summary>
/// <param name="InsertSpace">Insert a space after the line comment token and inside the block comment tokens.</param>
/// <param name="IgnoreEmptyLines">Ignore empty lines when toggling, adding or removing line comments.</param>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed record EditorCommentsOptions(bool InsertSpace = true, bool IgnoreEmptyLines = true)
{
    /// <summary>VS Code's defaults.</summary>
    public static EditorCommentsOptions Default { get; } = new();
}
