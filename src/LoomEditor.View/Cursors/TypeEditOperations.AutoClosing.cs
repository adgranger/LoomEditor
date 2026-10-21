// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorTypeEditOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>
/// A command that types an opening character and its closing character, and remembers where the closing character is
/// (Monaco: <c>BaseTypeWithAutoClosingCommand</c>). The subclasses compute the cursor state with
/// <see cref="ComputeCursorStateWithRange"/>.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal abstract class BaseTypeWithAutoClosingCommand(
    Range range,
    string text,
    int lineNumberDeltaOffset,
    int columnDeltaOffset,
    string openCharacter,
    string closeCharacter) : ReplaceCommandWithOffsetCursorState(range, text, lineNumberDeltaOffset, columnDeltaOffset)
{
    /// <summary>The range of the closing character, known once the command has run.</summary>
    public Range? CloseCharacterRange { get; private set; }

    /// <summary>The range of the opening and closing characters, known once the command has run.</summary>
    public Range? EnclosingRange { get; private set; }

    /// <summary>Monaco: <c>_computeCursorStateWithRange</c>.</summary>
    protected Selection ComputeCursorStateWithRange(TextModel model, Range range, ICursorStateComputerData helper)
    {
        CloseCharacterRange = new Range(range.StartLineNumber, range.EndColumn - closeCharacter.Length, range.EndLineNumber, range.EndColumn);
        EnclosingRange = new Range(range.StartLineNumber, range.EndColumn - openCharacter.Length - closeCharacter.Length, range.EndLineNumber, range.EndColumn);
        return base.ComputeCursorState(model, helper);
    }
}
