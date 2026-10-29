// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorTypeEditOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>Paste at every cursor (Monaco: <c>PasteOperation</c>).</summary>
/// <remarks>
/// Overtype branches are taken when <see cref="EditorInputMode.Overtype"/> is set.
/// Threading: stateless; UI thread only.
/// </remarks>
internal static class PasteOperation
{
    private static readonly Comparer<Selection> StartsComparer = Comparer<Selection>.Create((a, b) => Range.CompareUsingStarts(a.Range, b.Range));

    public static EditOperationResult GetEdits(
        CursorConfiguration config,
        IReadOnlyList<Selection> selections,
        string text,
        bool pasteOnNewLine,
        IReadOnlyList<string> multicursorText,
        bool isBlock = false)
    {
        bool distributeBlockToCursor = isBlock && config.MultiCursorPaste == MultiCursorPasteMode.Spread && selections.Count == 1;
        if (distributeBlockToCursor)
        {
            return BlockPaste(config, selections[0], text);
        }

        IReadOnlyList<string>? distributedPaste = DistributePasteToCursors(config, selections, text, pasteOnNewLine, multicursorText);
        if (distributedPaste is not null)
        {
            // Monaco sorts the array it received in place; the sorted copy is what the commands are built from (stable sort: JavaScript's sort is stable).
            List<Selection> sorted = selections.OrderBy(s => s, StartsComparer).ToList();
            return DistributedPaste(config, sorted, distributedPaste);
        }

        return SimplePaste(config, selections, text, pasteOnNewLine);
    }

    private static EditOperationResult BlockPaste(CursorConfiguration config, Selection selection, string text)
    {
        string[] lines = Strings.SplitLines(text);
        bool shouldOvertypeOnPaste = config.OvertypeOnPaste && config.InputMode == EditorInputMode.Overtype;
        var command = new ColumnSelectionPasteCommand(selection, lines, config.TabSize, shouldOvertypeOnPaste);
        return new EditOperationResult(EditOperationType.Other, [command], true, true);
    }

    private static IReadOnlyList<string>? DistributePasteToCursors(
        CursorConfiguration config,
        IReadOnlyList<Selection> selections,
        string text,
        bool pasteOnNewLine,
        IReadOnlyList<string> multicursorText)
    {
        if (selections.Count == 1)
        {
            return null;
        }

        if (multicursorText.Count == selections.Count)
        {
            return multicursorText;
        }

        if (pasteOnNewLine)
        {
            return null;
        }

        if (config.MultiCursorPaste == MultiCursorPasteMode.Spread)
        {
            // Try to spread the pasted text in case the line count matches the cursor count
            // Remove trailing \n if present
            if (text.Length > 0 && text[^1] == '\n')
            {
                text = text.Substring(0, text.Length - 1);
            }

            // Remove trailing \r if present
            if (text.Length > 0 && text[^1] == '\r')
            {
                text = text.Substring(0, text.Length - 1);
            }

            string[] lines = Strings.SplitLines(text);
            if (lines.Length == selections.Count)
            {
                return lines;
            }
        }

        return null;
    }

    private static EditOperationResult DistributedPaste(CursorConfiguration config, List<Selection> selections, IReadOnlyList<string> text)
    {
        var commands = new List<IEditCommand?>(selections.Count);
        for (int i = 0; i < selections.Count; i++)
        {
            bool shouldOvertypeOnPaste = config.OvertypeOnPaste && config.InputMode == EditorInputMode.Overtype;
            commands.Add(shouldOvertypeOnPaste
                ? new ReplaceOvertypeCommand(selections[i].Range, text[i])
                : new ReplaceCommand(selections[i].Range, text[i]));
        }

        return new EditOperationResult(EditOperationType.Other, commands, true, true);
    }

    private static EditOperationResult SimplePaste(CursorConfiguration config, IReadOnlyList<Selection> selections, string text, bool pasteOnNewLine)
    {
        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection selection in selections)
        {
            Position position = selection.Position;

            // pasteOnNewLine is reassigned inside the loop, like in Monaco: once false, it stays false for the next selections.
            if (pasteOnNewLine && !selection.IsEmpty)
            {
                pasteOnNewLine = false;
            }

            if (pasteOnNewLine && text.IndexOf('\n') != text.Length - 1)
            {
                pasteOnNewLine = false;
            }

            if (pasteOnNewLine)
            {
                // Paste entire line at the beginning of line
                var typeSelection = new Range(position.LineNumber, 1, position.LineNumber, 1);
                commands.Add(new ReplaceCommandThatPreservesSelection(typeSelection, text, selection, true));
            }
            else
            {
                bool shouldOvertypeOnPaste = config.OvertypeOnPaste && config.InputMode == EditorInputMode.Overtype;
                commands.Add(shouldOvertypeOnPaste
                    ? new ReplaceOvertypeCommand(selection.Range, text)
                    : new ReplaceCommand(selection.Range, text));
            }
        }

        return new EditOperationResult(EditOperationType.Other, commands, true, true);
    }
}
