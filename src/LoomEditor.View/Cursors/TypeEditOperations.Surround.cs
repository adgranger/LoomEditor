// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorTypeEditOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>Typing a bracket or a quote over a selection surrounds it (Monaco: <c>SurroundSelectionOperation</c>).</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static class SurroundSelectionOperation
{
    public static EditOperationResult? GetEdits(CursorConfiguration config, TextModel model, IReadOnlyList<Selection> selections, string ch, bool isDoingComposition)
    {
        if (!isDoingComposition && IsSurroundSelectionType(config, model, selections, ch))
        {
            return RunSurroundSelectionType(config, selections, ch);
        }

        return null;
    }

    /// <summary>Whether <c>autoSurround</c> allows surrounding with <paramref name="ch"/> (Monaco: <c>shouldSurroundChar</c>).</summary>
    public static bool ShouldSurroundChar(CursorConfiguration config, string ch) =>
        CursorCommon.IsQuote(ch)
            ? config.AutoSurround is EditorAutoSurroundStrategy.Quotes or EditorAutoSurroundStrategy.LanguageDefined
            // Character is a bracket
            : config.AutoSurround is EditorAutoSurroundStrategy.Brackets or EditorAutoSurroundStrategy.LanguageDefined;

    private static EditOperationResult RunSurroundSelectionType(CursorConfiguration config, IReadOnlyList<Selection> selections, string ch)
    {
        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection selection in selections)
        {
            string closeCharacter = config.SurroundingPairs[ch];
            commands.Add(new SurroundSelectionCommand(selection, ch, closeCharacter));
        }

        return new EditOperationResult(EditOperationType.Other, commands, true, true);
    }

    private static bool IsSurroundSelectionType(CursorConfiguration config, TextModel model, IReadOnlyList<Selection> selections, string ch)
    {
        if (!ShouldSurroundChar(config, ch) || !config.SurroundingPairs.ContainsKey(ch))
        {
            return false;
        }

        bool isTypingAQuoteCharacter = CursorCommon.IsQuote(ch);
        foreach (Selection selection in selections)
        {
            if (selection.IsEmpty)
            {
                return false;
            }

            bool selectionContainsOnlyWhitespace = true;
            for (int lineNumber = selection.Range.StartLineNumber; lineNumber <= selection.Range.EndLineNumber; lineNumber++)
            {
                string lineText = model.GetLineContent(lineNumber);
                int startIndex = lineNumber == selection.Range.StartLineNumber ? selection.Range.StartColumn - 1 : 0;
                int endIndex = lineNumber == selection.Range.EndLineNumber ? selection.Range.EndColumn - 1 : lineText.Length;
                if (ContainsNonBlank(lineText.AsSpan(startIndex, endIndex - startIndex)))
                {
                    // this selected text contains something other than whitespace
                    selectionContainsOnlyWhitespace = false;
                    break;
                }
            }

            if (selectionContainsOnlyWhitespace)
            {
                return false;
            }

            if (isTypingAQuoteCharacter && selection.Range.StartLineNumber == selection.Range.EndLineNumber && selection.Range.StartColumn + 1 == selection.Range.EndColumn)
            {
                string selectionText = model.GetValueInRange(selection.Range);
                if (CursorCommon.IsQuote(selectionText))
                {
                    // Typing a quote character on top of another quote character
                    // => disable surround selection type
                    return false;
                }
            }
        }

        return true;
    }

    // Monaco: /[^ \t]/.test(text)
    private static bool ContainsNonBlank(ReadOnlySpan<char> text)
    {
        foreach (char c in text)
        {
            if (c != ' ' && c != (char)9)
            {
                return true;
            }
        }

        return false;
    }
}
