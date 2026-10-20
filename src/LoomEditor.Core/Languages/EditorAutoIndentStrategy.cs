// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages;

/// <summary>How much the editor adjusts the indentation of new lines (Monaco: <c>EditorAutoIndentStrategy</c>).</summary>
internal enum EditorAutoIndentStrategy
{
    None = 0,
    Keep = 1,
    Brackets = 2,
    Advanced = 3,
    Full = 4,
}
