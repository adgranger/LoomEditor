// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorTypeEditOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>The Tab key at every cursor (Monaco: <c>TabOperation</c>).</summary>
/// <remarks>
/// Threading: stateless; UI thread only.
/// </remarks>
internal static class TabOperation
{
    public static List<IEditCommand?> GetCommands(CursorConfiguration config, TextModel model, IReadOnlyList<Selection> selections)
    {
        var simpleModel = new TextModelCursorSimpleModel(model);
        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection selection in selections)
        {
            Range range = selection.Range;
            if (selection.IsEmpty)
            {
                string lineText = model.GetLineContent(range.StartLineNumber);

                if (Strings.IsJavaScriptWhitespaceOnly(lineText) && model.Tokenization.IsCheapToTokenize(range.StartLineNumber))
                {
                    string goodIndent = GoodIndentForLine(config, model, range.StartLineNumber) ?? "\t";
                    string possibleTypeText = config.NormalizeIndentation(goodIndent);
                    if (!lineText.StartsWith(possibleTypeText, StringComparison.Ordinal))
                    {
                        commands.Add(new ReplaceCommand(new Range(range.StartLineNumber, 1, range.StartLineNumber, lineText.Length + 1), possibleTypeText, true));
                        continue;
                    }
                }

                commands.Add(ReplaceJumpToNextIndent(config, simpleModel, selection, true));
            }
            else
            {
                if (range.StartLineNumber == range.EndLineNumber)
                {
                    int lineMaxColumn = model.GetLineMaxColumn(range.StartLineNumber);
                    if (range.StartColumn != 1 || range.EndColumn != lineMaxColumn)
                    {
                        // This is a single line selection that is not the entire line
                        commands.Add(ReplaceJumpToNextIndent(config, simpleModel, selection, false));
                        continue;
                    }
                }

                commands.Add(new ShiftCommand(
                    selection,
                    new ShiftCommandOptions(false, config.TabSize, config.IndentSize, config.InsertSpaces, config.UseTabStops, config.AutoIndent),
                    config.LanguageConfigurationService));
            }
        }

        return commands;
    }

    // Monaco: _goodIndentForLine
    private static string? GoodIndentForLine(CursorConfiguration config, TextModel model, int lineNumber)
    {
        IndentAction? action = null;
        string indentation = string.Empty;
        InheritIndentResult? expectedIndentAction = AutoIndent.GetInheritIndentForLine(config.AutoIndent, model, lineNumber, false, config.LanguageConfigurationService);
        if (expectedIndentAction is { } expected)
        {
            action = expected.Action;
            indentation = expected.Indentation;
        }
        else if (lineNumber > 1)
        {
            int lastLineNumber;
            for (lastLineNumber = lineNumber - 1; lastLineNumber >= 1; lastLineNumber--)
            {
                string lineText = model.GetLineContent(lastLineNumber);
                int nonWhitespaceIdx = Strings.LastNonWhitespaceIndex(lineText);
                if (nonWhitespaceIdx >= 0)
                {
                    break;
                }
            }

            if (lastLineNumber < 1)
            {
                // No previous line with content found
                return null;
            }

            int maxColumn = model.GetLineMaxColumn(lastLineNumber);
            CompleteEnterAction? expectedEnterAction = EnterActions.GetEnterAction(
                config.AutoIndent, model, new Range(lastLineNumber, maxColumn, lastLineNumber, maxColumn), config.LanguageConfigurationService);
            if (expectedEnterAction is { } enterAction)
            {
                indentation = enterAction.Indentation + enterAction.AppendText;
            }
        }

        if (action is not null)
        {
            if (action == IndentAction.Indent)
            {
                indentation = TypeEditHelpers.ShiftIndent(config, indentation);
            }

            if (action == IndentAction.Outdent)
            {
                indentation = TypeEditHelpers.UnshiftIndent(config, indentation);
            }

            indentation = config.NormalizeIndentation(indentation);
        }

        return string.IsNullOrEmpty(indentation) ? null : indentation;
    }

    private static ReplaceCommand ReplaceJumpToNextIndent(CursorConfiguration config, ICursorSimpleModel model, Selection selection, bool insertsAutoWhitespace)
    {
        string typeText;
        Position position = selection.Range.Start;
        if (config.InsertSpaces)
        {
            int visibleColumnFromColumn = config.VisibleColumnFromColumn(model, position);
            int indentSize = config.IndentSize;
            int spacesCnt = indentSize - (visibleColumnFromColumn % indentSize);
            typeText = new string(' ', spacesCnt);
        }
        else
        {
            typeText = "\t";
        }

        return new ReplaceCommand(selection.Range, typeText, insertsAutoWhitespace);
    }
}
