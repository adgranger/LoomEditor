// Ported from microsoft/vscode: src/vs/editor/common/commands/trimTrailingWhitespaceCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.View.Cursors.Commands;

/// <summary>Trims the trailing whitespace of the model, except on the lines where cursors are sitting.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class TrimTrailingWhitespaceCommand(Selection selection, IReadOnlyList<Position> cursors, bool trimInRegexesAndStrings) : IEditCommand
{
    private string? _selectionId;

    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        foreach ((Range range, string? text) in TrimTrailingWhitespace(model, [.. cursors], trimInRegexesAndStrings))
        {
            builder.AddEditOperation(range, text);
        }

        _selectionId = builder.TrackSelection(selection);
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper) => helper.GetTrackedSelection(_selectionId!);

    /// <summary>
    /// The edits that trim the trailing whitespace of <paramref name="model"/> and ignore the lines on which cursors are sitting.
    /// </summary>
    /// <remarks>Unlike Monaco, <paramref name="cursors"/> is not sorted and spliced in place: a copy is.</remarks>
    internal static List<(Range Range, string? Text)> TrimTrailingWhitespace(TextModel model, List<Position> cursors, bool trimInRegexesAndStrings)
    {
        // Sort cursors ascending (stable)
        List<Position> sorted = [.. cursors.OrderBy(p => p, Comparer<Position>.Create((a, b) => a.CompareTo(b)))];

        // Reduce multiple cursors on the same line and only keep the last one on the line
        for (int i = sorted.Count - 2; i >= 0; i--)
        {
            if (sorted[i].LineNumber == sorted[i + 1].LineNumber)
            {
                // Remove cursor at `i`
                sorted.RemoveAt(i);
            }
        }

        var r = new List<(Range Range, string? Text)>();
        int cursorIndex = 0;
        int cursorLen = sorted.Count;

        for (int lineNumber = 1, lineCount = model.LineCount; lineNumber <= lineCount; lineNumber++)
        {
            string lineContent = model.GetLineContent(lineNumber);
            int maxLineColumn = lineContent.Length + 1;
            int minEditColumn = 0;

            if (cursorIndex < cursorLen && sorted[cursorIndex].LineNumber == lineNumber)
            {
                minEditColumn = sorted[cursorIndex].Column;
                cursorIndex++;
                if (minEditColumn == maxLineColumn)
                {
                    // The cursor is at the end of the line => no edits for sure on this line
                    continue;
                }
            }

            if (lineContent.Length == 0)
            {
                continue;
            }

            int lastNonWhitespaceIndex = Strings.LastNonWhitespaceIndex(lineContent);

            int fromColumn;
            if (lastNonWhitespaceIndex == -1)
            {
                // Entire line is whitespace
                fromColumn = 1;
            }
            else if (lastNonWhitespaceIndex != lineContent.Length - 1)
            {
                // There is trailing whitespace
                fromColumn = lastNonWhitespaceIndex + 2;
            }
            else
            {
                // There is no trailing whitespace
                continue;
            }

            if (!trimInRegexesAndStrings)
            {
                if (!model.Tokenization.HasAccurateTokensForLine(lineNumber))
                {
                    // We don't want to force line tokenization, as that can be expensive, but we also don't want to trim
                    // trailing whitespace in lines that are not tokenized yet, as that can be wrong and trim whitespace from
                    // lines that the user requested we don't. So we bail out if the tokens are not accurate for this line.
                    continue;
                }

                LineTokens lineTokens = model.Tokenization.GetLineTokens(lineNumber);
                StandardTokenType fromColumnType = lineTokens.GetStandardTokenType(lineTokens.FindTokenIndexAtOffset(fromColumn));

                if (fromColumnType is StandardTokenType.String or StandardTokenType.RegEx)
                {
                    continue;
                }
            }

            fromColumn = Math.Max(minEditColumn, fromColumn);
            r.Add((new Range(lineNumber, fromColumn, lineNumber, maxLineColumn), null));
        }

        return r;
    }
}
