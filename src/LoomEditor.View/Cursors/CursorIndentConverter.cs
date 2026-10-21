// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorTypeEditOperations.ts (the object literal given to getIndentForEnter)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;

namespace LoomEditor.View.Cursors;

/// <summary>Shifts and unshifts indentations with the settings of a <see cref="CursorConfiguration"/>.</summary>
/// <remarks>Threading: stateless apart from its configuration; UI thread only.</remarks>
internal sealed class CursorIndentConverter(CursorConfiguration config) : IIndentConverter
{
    public string ShiftIndent(string indentation) => TypeEditHelpers.ShiftIndent(config, indentation);

    public string UnshiftIndent(string indentation) => TypeEditHelpers.UnshiftIndent(config, indentation);
}
