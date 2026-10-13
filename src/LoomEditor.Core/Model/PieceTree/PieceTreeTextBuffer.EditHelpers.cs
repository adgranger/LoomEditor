// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.PieceTree;

internal sealed partial class PieceTreeTextBuffer
{
    /// <summary>Merges several sorted, validated edits into one edit covering all of them (Monaco: <c>_toSingleEditOperation</c>).</summary>
    internal ValidatedEditOperation ToSingleEditOperation(IReadOnlyList<ValidatedEditOperation> operations)
    {
        bool forceMoveMarkers = false;
        Range firstEditRange = operations[0].Range;
        Range lastEditRange = operations[^1].Range;
        var entireEditRange = new Range(firstEditRange.StartLineNumber, firstEditRange.StartColumn, lastEditRange.EndLineNumber, lastEditRange.EndColumn);
        int lastEndLineNumber = firstEditRange.StartLineNumber;
        int lastEndColumn = firstEditRange.StartColumn;
        var result = new List<string>();

        foreach (ValidatedEditOperation operation in operations)
        {
            Range range = operation.Range;
            forceMoveMarkers = forceMoveMarkers || operation.ForceMoveMarkers;

            // (1) -- Push old text
            result.Add(GetValueInRange(new Range(lastEndLineNumber, lastEndColumn, range.StartLineNumber, range.StartColumn)));

            // (2) -- Push new text
            if (operation.Text.Length > 0)
            {
                result.Add(operation.Text);
            }

            lastEndLineNumber = range.EndLineNumber;
            lastEndColumn = range.EndColumn;
        }

        string text = string.Concat(result);
        EolCount count = EolCounter.Count(text);

        return new ValidatedEditOperation
        {
            SortIndex = 0,
            Identifier = operations[0].Identifier,
            Range = entireEditRange,
            RangeOffset = GetOffsetAt(entireEditRange.StartLineNumber, entireEditRange.StartColumn),
            RangeLength = GetValueLengthInRange(entireEditRange, EndOfLinePreference.TextDefined),
            Text = text,
            EolCount = count.Count,
            FirstLineLength = count.FirstLineLength,
            LastLineLength = count.LastLineLength,
            ForceMoveMarkers = forceMoveMarkers,
            IsAutoWhitespaceEdit = false,
        };
    }

    /// <summary>The range that <paramref name="text"/> occupies once inserted at the start of <paramref name="range"/> (Monaco: <c>_getInverseEditRange</c>).</summary>
    internal static Range GetInverseEditRange(Range range, string text)
    {
        EolCount count = EolCounter.Count(text);
        return InverseRange(range.StartLineNumber, range.StartColumn, text.Length > 0, count.Count, count.FirstLineLength, count.LastLineLength);
    }

    /// <summary>Where the text of each edit ends up once all edits are applied; assumes validated edits sorted ascending (Monaco: <c>_getInverseEditRanges</c>).</summary>
    internal static Range[] GetInverseEditRanges(IReadOnlyList<ValidatedEditOperation> operations)
    {
        var result = new Range[operations.Count];
        int prevOpEndLineNumber = 0;
        int prevOpEndColumn = 0;
        ValidatedEditOperation? prevOp = null;
        for (int i = 0; i < operations.Count; i++)
        {
            ValidatedEditOperation op = operations[i];
            int startLineNumber;
            int startColumn;
            if (prevOp is not null)
            {
                if (prevOp.Range.EndLineNumber == op.Range.StartLineNumber)
                {
                    startLineNumber = prevOpEndLineNumber;
                    startColumn = prevOpEndColumn + (op.Range.StartColumn - prevOp.Range.EndColumn);
                }
                else
                {
                    startLineNumber = prevOpEndLineNumber + (op.Range.StartLineNumber - prevOp.Range.EndLineNumber);
                    startColumn = op.Range.StartColumn;
                }
            }
            else
            {
                startLineNumber = op.Range.StartLineNumber;
                startColumn = op.Range.StartColumn;
            }

            Range resultRange = InverseRange(startLineNumber, startColumn, op.Text.Length > 0, op.EolCount, op.FirstLineLength, op.LastLineLength);
            prevOpEndLineNumber = resultRange.EndLineNumber;
            prevOpEndColumn = resultRange.EndColumn;
            result[i] = resultRange;
            prevOp = op;
        }

        return result;
    }

    /// <summary>Orders edits by range end, then by <see cref="ValidatedEditOperation.SortIndex"/> (Monaco: <c>_sortOpsAscending</c>).</summary>
    internal static int SortOpsAscending(ValidatedEditOperation a, ValidatedEditOperation b)
    {
        int r = Range.CompareUsingEnds(a.Range, b.Range);
        return r == 0 ? a.SortIndex - b.SortIndex : r;
    }

    /// <summary>The reverse of <see cref="SortOpsAscending"/> (Monaco: <c>_sortOpsDescending</c>).</summary>
    internal static int SortOpsDescending(ValidatedEditOperation a, ValidatedEditOperation b)
    {
        int r = Range.CompareUsingEnds(a.Range, b.Range);
        return r == 0 ? b.SortIndex - a.SortIndex : -r;
    }

    private static Range InverseRange(int startLineNumber, int startColumn, bool inserts, int eolCount, int firstLineLength, int lastLineLength)
    {
        if (!inserts)
        {
            // There is nothing to insert
            return new Range(startLineNumber, startColumn, startLineNumber, startColumn);
        }

        int lineCount = eolCount + 1;
        return lineCount == 1
            ? new Range(startLineNumber, startColumn, startLineNumber, startColumn + firstLineLength)
            : new Range(startLineNumber, startColumn, startLineNumber + lineCount - 1, lastLineLength + 1);
    }
}
