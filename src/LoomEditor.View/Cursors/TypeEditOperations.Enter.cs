// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorTypeEditOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>Enter, and the commands that insert a line (Monaco: <c>EnterOperation</c>).</summary>
/// <remarks>
/// Threading: stateless; UI thread only.
/// </remarks>
internal static class EnterOperation
{
    /// <summary>The edits for a typed <c>"\n"</c>, or <see langword="null"/> when <paramref name="ch"/> is something else (Monaco: <c>undefined</c>).</summary>
    public static EditOperationResult? GetEdits(
        CursorConfiguration config,
        TextModel model,
        IReadOnlyList<Selection> selections,
        string ch,
        bool isDoingComposition)
    {
        if (!isDoingComposition && ch == "\n")
        {
            var commands = new List<IEditCommand?>(selections.Count);
            foreach (Selection selection in selections)
            {
                commands.Add(Enter(config, model, false, selection.Range));
            }

            return new EditOperationResult(EditOperationType.TypingOther, commands, true, false);
        }

        return null;
    }

    /// <summary>The commands that insert a line before each cursor's line (Monaco: <c>lineInsertBefore</c>).</summary>
    public static List<IEditCommand?> LineInsertBefore(CursorConfiguration config, TextModel model, IReadOnlyList<Selection> selections)
    {
        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection selection in selections)
        {
            int lineNumber = selection.PositionLineNumber;
            if (lineNumber == 1)
            {
                commands.Add(new ReplaceCommandWithoutChangingPosition(new Range(1, 1, 1, 1), "\n"));
            }
            else
            {
                lineNumber--;
                int column = model.GetLineMaxColumn(lineNumber);
                commands.Add(Enter(config, model, false, new Range(lineNumber, column, lineNumber, column)));
            }
        }

        return commands;
    }

    /// <summary>The commands that insert a line after each cursor's line (Monaco: <c>lineInsertAfter</c>).</summary>
    public static List<IEditCommand?> LineInsertAfter(CursorConfiguration config, TextModel model, IReadOnlyList<Selection> selections)
    {
        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection selection in selections)
        {
            int lineNumber = selection.PositionLineNumber;
            int column = model.GetLineMaxColumn(lineNumber);
            commands.Add(Enter(config, model, false, new Range(lineNumber, column, lineNumber, column)));
        }

        return commands;
    }

    /// <summary>The commands that break the line at each cursor and leave the cursor in place (Monaco: <c>lineBreakInsert</c>).</summary>
    public static List<IEditCommand?> LineBreakInsert(CursorConfiguration config, TextModel model, IReadOnlyList<Selection> selections)
    {
        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection selection in selections)
        {
            commands.Add(Enter(config, model, true, selection.Range));
        }

        return commands;
    }

    // Monaco: _enter
    private static IEditCommand Enter(CursorConfiguration config, TextModel model, bool keepPosition, Range range)
    {
        if (config.AutoIndent == EditorAutoIndentStrategy.None)
        {
            return TypeEditHelpers.TypeCommand(range, "\n", keepPosition);
        }

        if (!model.Tokenization.IsCheapToTokenize(range.StartLineNumber) || config.AutoIndent == EditorAutoIndentStrategy.Keep)
        {
            string keptLineText = model.GetLineContent(range.StartLineNumber);
            string keptIndentation = LeadingIndentationBeforeColumn(keptLineText, range.StartColumn);
            return TypeEditHelpers.TypeCommand(range, "\n" + config.NormalizeIndentation(keptIndentation), keepPosition);
        }

        CompleteEnterAction? r = EnterActions.GetEnterAction(config.AutoIndent, model, range, config.LanguageConfigurationService);
        if (r is { } action)
        {
            switch (action.IndentAction)
            {
                case IndentAction.None:
                    // Nothing special
                    return TypeEditHelpers.TypeCommand(range, "\n" + config.NormalizeIndentation(action.Indentation + action.AppendText), keepPosition);

                case IndentAction.Indent:
                    // Indent once
                    return TypeEditHelpers.TypeCommand(range, "\n" + config.NormalizeIndentation(action.Indentation + action.AppendText), keepPosition);

                case IndentAction.IndentOutdent:
                {
                    // Ultra special
                    string normalIndent = config.NormalizeIndentation(action.Indentation);
                    string increasedIndent = config.NormalizeIndentation(action.Indentation + action.AppendText);
                    string typeText = "\n" + increasedIndent + "\n" + normalIndent;
                    if (keepPosition)
                    {
                        return new ReplaceCommandWithoutChangingPosition(range, typeText, true);
                    }

                    return new ReplaceCommandWithOffsetCursorState(range, typeText, -1, increasedIndent.Length - normalIndent.Length, true);
                }

                case IndentAction.Outdent:
                {
                    string actualIndentation = TypeEditHelpers.UnshiftIndent(config, action.Indentation);
                    return TypeEditHelpers.TypeCommand(range, "\n" + config.NormalizeIndentation(actualIndentation + action.AppendText), keepPosition);
                }
            }
        }

        string lineText = model.GetLineContent(range.StartLineNumber);
        string indentation = LeadingIndentationBeforeColumn(lineText, range.StartColumn);

        if (config.AutoIndent >= EditorAutoIndentStrategy.Full
            && AutoIndent.GetIndentForEnter(config.AutoIndent, model, range, new CursorIndentConverter(config), config.LanguageConfigurationService) is { } ir)
        {
            int oldEndViewColumn = config.VisibleColumnFromColumn(new TextModelCursorSimpleModel(model), range.End);
            int oldEndColumn = range.EndColumn;
            string newLineContent = model.GetLineContent(range.EndLineNumber);
            int firstNonWhitespace = StringClassification.FirstNonWhitespaceIndex(newLineContent);
            range = firstNonWhitespace >= 0
                ? new Range(range.StartLineNumber, range.StartColumn, range.EndLineNumber, Math.Max(range.EndColumn, firstNonWhitespace + 1))
                : new Range(range.StartLineNumber, range.StartColumn, range.EndLineNumber, model.GetLineMaxColumn(range.EndLineNumber));
            if (keepPosition)
            {
                return new ReplaceCommandWithoutChangingPosition(range, "\n" + config.NormalizeIndentation(ir.AfterEnter), true);
            }

            int offset = 0;
            if (oldEndColumn <= firstNonWhitespace + 1)
            {
                if (!config.InsertSpaces)
                {
                    oldEndViewColumn = (oldEndViewColumn + config.IndentSize - 1) / config.IndentSize;
                }

                offset = Math.Min(oldEndViewColumn + 1 - config.NormalizeIndentation(ir.AfterEnter).Length - 1, 0);
            }

            return new ReplaceCommandWithOffsetCursorState(range, "\n" + config.NormalizeIndentation(ir.AfterEnter), 0, offset, true);
        }

        return TypeEditHelpers.TypeCommand(range, "\n" + config.NormalizeIndentation(indentation), keepPosition);
    }

    // Monaco: strings.getLeadingWhitespace(lineText).substring(0, column - 1); substring clamps to the length, Substring does not.
    private static string LeadingIndentationBeforeColumn(string lineText, int column)
    {
        string whitespace = Strings.GetLeadingWhitespace(lineText);
        return whitespace.Substring(0, Math.Min(column - 1, whitespace.Length));
    }
}
