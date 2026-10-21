// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorTypeEditOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>Re-indents the line while typing a character that matches an indentation rule (Monaco: <c>AutoIndentOperation</c>).</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static class AutoIndentOperation
{
    private readonly record struct IndentationForSelection(Selection Selection, string Indentation);

    public static EditOperationResult? GetEdits(CursorConfiguration config, TextModel model, IReadOnlyList<Selection> selections, string ch, bool isDoingComposition)
    {
        if (!isDoingComposition && IsAutoIndentType(config, model, selections))
        {
            var indentationForSelections = new List<IndentationForSelection>(selections.Count);
            foreach (Selection selection in selections)
            {
                string? indentation = FindActualIndentationForSelection(config, model, selection, ch);
                if (indentation is null)
                {
                    // Auto indentation failed
                    return null;
                }

                indentationForSelections.Add(new IndentationForSelection(selection, indentation));
            }

            AutoClosingPairResult? autoClosingPair = AutoClosingOpenCharTypeOperation.GetAutoClosingPair(config, model, selections, ch, false);
            return GetIndentationAndAutoClosingPairEdits(config, model, indentationForSelections, ch, autoClosingPair);
        }

        return null;
    }

    private static bool IsAutoIndentType(CursorConfiguration config, TextModel model, IReadOnlyList<Selection> selections)
    {
        if (config.AutoIndent < EditorAutoIndentStrategy.Full)
        {
            return false;
        }

        foreach (Selection selection in selections)
        {
            if (!model.Tokenization.IsCheapToTokenize(selection.Range.EndLineNumber))
            {
                return false;
            }
        }

        return true;
    }

    private static string? FindActualIndentationForSelection(CursorConfiguration config, TextModel model, Selection selection, string ch)
    {
        string? actualIndentation = AutoIndent.GetIndentActionForType(
            config.AutoIndent,
            config.AutoClosingPairs,
            model,
            selection.Range,
            ch,
            new CursorIndentConverter(config),
            config.LanguageConfigurationService);

        if (actualIndentation is null)
        {
            return null;
        }

        string currentIndentation = LanguageConfigurationRegistry.GetIndentationAtPosition(model, selection.Range.StartLineNumber, selection.Range.StartColumn);
        if (actualIndentation == config.NormalizeIndentation(currentIndentation))
        {
            return null;
        }

        return actualIndentation;
    }

    private static EditOperationResult GetIndentationAndAutoClosingPairEdits(
        CursorConfiguration config,
        TextModel model,
        List<IndentationForSelection> indentationForSelections,
        string ch,
        AutoClosingPairResult? autoClosingPair)
    {
        bool autoCloseWithUndoStop = autoClosingPair is { } pair && pair.Open.Length > ch.Length;
        var commands = new List<IEditCommand?>(indentationForSelections.Count);
        foreach ((Selection selection, string indentation) in indentationForSelections)
        {
            if (autoClosingPair is { } autoClosing && !autoCloseWithUndoStop)
            {
                // Apply both auto closing pair edits and auto indentation edits
                (Range range, string text) = GetEditFromIndentationAndSelection(config, model, indentation, selection, ch, false);
                commands.Add(new TypeWithIndentationAndAutoClosingCommand(range, text, selection.Range, ch, autoClosing.Close));
            }
            else
            {
                // Apply only auto indentation edits
                (Range range, string text) = GetEditFromIndentationAndSelection(config, model, indentation, selection, ch, true);
                commands.Add(TypeEditHelpers.TypeCommand(range, text, false));
            }
        }

        if (autoClosingPair is { } withUndoStop && autoCloseWithUndoStop)
        {
            return AutoClosingOpenCharTypeOperation.TypeAndAutoCloseWithUndoStop(model, commands, ch, withUndoStop.Close);
        }

        return new EditOperationResult(EditOperationType.TypingOther, commands, true, false);
    }

    private static (Range Range, string Text) GetEditFromIndentationAndSelection(
        CursorConfiguration config,
        TextModel model,
        string indentation,
        Selection selection,
        string ch,
        bool includeChInEdit)
    {
        int startLineNumber = selection.Range.StartLineNumber;
        int firstNonWhitespaceColumn = model.GetLineFirstNonWhitespaceColumn(startLineNumber);
        string text = config.NormalizeIndentation(indentation);
        if (firstNonWhitespaceColumn != 0)
        {
            string startLine = model.GetLineContent(startLineNumber);
            // JavaScript's substring swaps its arguments when the cursor is inside the indentation.
            int from = Math.Min(firstNonWhitespaceColumn, selection.Range.StartColumn) - 1;
            int to = Math.Max(firstNonWhitespaceColumn, selection.Range.StartColumn) - 1;
            text += startLine.Substring(from, to - from);
        }

        text += includeChInEdit ? ch : string.Empty;
        var range = new Range(startLineNumber, 1, selection.Range.EndLineNumber, selection.Range.EndColumn);
        return (range, text);
    }
}
