// Ported from microsoft/vscode: src/vs/editor/common/languages/autoIndent.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages;

/// <summary>
/// Shifts an indentation by one level (Monaco: <c>IIndentConverter</c>; its optional <c>normalizeIndentation</c> is
/// not read by <c>autoIndent.ts</c> and is not ported).
/// </summary>
/// <remarks>Threading: an implementation is used from the thread of the editor.</remarks>
internal interface IIndentConverter
{
    /// <summary>The indentation one level deeper.</summary>
    string ShiftIndent(string indentation);

    /// <summary>The indentation one level shallower.</summary>
    string UnshiftIndent(string indentation);
}
