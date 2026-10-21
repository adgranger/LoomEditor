// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts (autoIndentOnPaste, autoIndentOnPasteWithinString)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Indentation;

/// <summary>The indentation options of the editor.</summary>
/// <param name="AutoIndentOnPaste">Re-indent the pasted lines (<c>editor.autoIndentOnPaste</c>).</param>
/// <param name="AutoIndentOnPasteWithinString">Re-indent when the paste starts or ends in a string (<c>editor.autoIndentOnPasteWithinString</c>).</param>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed record EditorIndentationOptions(bool AutoIndentOnPaste = false, bool AutoIndentOnPasteWithinString = true)
{
    /// <summary>VS Code's defaults.</summary>
    public static EditorIndentationOptions Default { get; } = new();
}
