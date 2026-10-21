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

/// <summary>Types over a closing character that the editor inserted (Monaco: <c>AutoClosingOvertypeOperation</c>).</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static class AutoClosingOvertypeOperation
{
    public static EditOperationResult? GetEdits(
        EditOperationType prevEditOperationType,
        CursorConfiguration config,
        TextModel model,
        IReadOnlyList<Selection> selections,
        IReadOnlyList<Range> autoClosedCharacters,
        string ch)
    {
        if (AutoClosingOvertype.IsAutoClosingOvertype(config, model, selections, autoClosedCharacters, ch))
        {
            return RunAutoClosingOvertype(prevEditOperationType, selections, ch);
        }

        return null;
    }

    private static EditOperationResult RunAutoClosingOvertype(EditOperationType prevEditOperationType, IReadOnlyList<Selection> selections, string ch)
    {
        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection selection in selections)
        {
            Position position = selection.Position;
            var typeSelection = new Range(position.LineNumber, position.Column, position.LineNumber, position.Column + 1);
            commands.Add(new ReplaceCommand(typeSelection, ch));
        }

        return new EditOperationResult(
            EditOperationType.TypingOther,
            commands,
            TypeEditHelpers.ShouldPushStackElementBetween(prevEditOperationType, EditOperationType.TypingOther),
            false);
    }
}

/// <summary>The overtype step that runs after the close character was typed by an interceptor (Monaco: <c>AutoClosingOvertypeWithInterceptorsOperation</c>).</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static class AutoClosingOvertypeWithInterceptorsOperation
{
    public static EditOperationResult? GetEdits(
        CursorConfiguration config,
        TextModel model,
        IReadOnlyList<Selection> selections,
        IReadOnlyList<Range> autoClosedCharacters,
        string ch)
    {
        if (AutoClosingOvertype.IsAutoClosingOvertype(config, model, selections, autoClosedCharacters, ch))
        {
            // Unfortunately, the close character is at this point "doubled", so we need to delete it...
            var commands = new List<IEditCommand?>(selections.Count);
            foreach (Selection s in selections)
            {
                commands.Add(new ReplaceCommand(new Range(s.PositionLineNumber, s.PositionColumn, s.PositionLineNumber, s.PositionColumn + 1), string.Empty, false));
            }

            return new EditOperationResult(EditOperationType.TypingOther, commands, true, false);
        }

        return null;
    }
}

/// <summary>Whether a typed character types over the auto-closed character after the cursors (Monaco: <c>isAutoClosingOvertype</c>).</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static class AutoClosingOvertype
{
    public static bool IsAutoClosingOvertype(
        CursorConfiguration config,
        TextModel model,
        IReadOnlyList<Selection> selections,
        IReadOnlyList<Range> autoClosedCharacters,
        string ch)
    {
        if (config.AutoClosingOvertype == EditorAutoClosingEditStrategy.Never)
        {
            return false;
        }

        if (!config.AutoClosingPairs.AutoClosingPairsCloseSingleChar.ContainsKey(ch))
        {
            return false;
        }

        foreach (Selection selection in selections)
        {
            if (!selection.IsEmpty)
            {
                return false;
            }

            Position position = selection.Position;
            string lineText = model.GetLineContent(position.LineNumber);
            // JavaScript charAt past the end gives "": it never equals ch.
            string afterCharacter = position.Column - 1 < lineText.Length ? lineText[position.Column - 1].ToString() : string.Empty;
            if (afterCharacter != ch)
            {
                return false;
            }

            // Do not over-type quotes after a backslash
            bool chIsQuote = CursorCommon.IsQuote(ch);
            char beforeCharacter = position.Column > 2 ? lineText[position.Column - 2] : '\0';
            if (beforeCharacter == '\\' && chIsQuote)
            {
                return false;
            }

            // Must over-type a closing character typed by the editor
            if (config.AutoClosingOvertype == EditorAutoClosingEditStrategy.Auto)
            {
                bool found = false;
                foreach (Range autoClosedCharacter in autoClosedCharacters)
                {
                    if (position.LineNumber == autoClosedCharacter.StartLineNumber && position.Column == autoClosedCharacter.StartColumn)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
