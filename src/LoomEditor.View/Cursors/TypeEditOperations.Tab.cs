// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorTypeEditOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>The Tab key at every cursor (Monaco: <c>TabOperation</c>).</summary>
/// <remarks>
/// Without a language configuration (phases 4–5) there is no "good indentation" for an empty line: Tab on it inserts
/// one indentation step.
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

                // isCheapToTokenize is always true in phase 3 (phase 4 cut point: there is no tokenizer).
                if (Strings.IsJavaScriptWhitespaceOnly(lineText))
                {
                    string goodIndent = GoodIndentForLine() ?? "\t";
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
                    new ShiftCommandOptions(false, config.TabSize, config.IndentSize, config.InsertSpaces, config.UseTabStops, config.AutoIndent)));
            }
        }

        return commands;
    }

    // Phase 4–5 cut point: cursorTypeEditOperations.ts 877–916 (_goodIndentForLine). It calls getInheritIndentForLine and getEnterAction,
    // which both return null without a language configuration; the method then returns null.
    private static string? GoodIndentForLine() => null;

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
