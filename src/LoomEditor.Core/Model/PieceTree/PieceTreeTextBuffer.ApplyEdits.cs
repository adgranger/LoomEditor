// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.Core.Undo;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.PieceTree;

internal sealed partial class PieceTreeTextBuffer
{
    // Monaco: a thousand edits work fine regardless of their shape (_reduceOperations).
    private const int ReduceOperationsThreshold = 1000;

    /// <inheritdoc />
    public ApplyEditsResult ApplyEdits(IReadOnlyList<ValidAnnotatedEditOperation> operations, bool recordTrimAutoWhitespace, bool computeUndoEdits)
    {
        ArgumentNullException.ThrowIfNull(operations);

        bool mightContainRtl = _mightContainRtl;
        bool mightContainUnusualLineTerminators = _mightContainUnusualLineTerminators;
        bool mightContainNonBasicAscii = _mightContainNonBasicAscii;
        bool canReduceOperations = true;

        string bufferEol = GetEol();
        var validated = new List<ValidatedEditOperation>(operations.Count);
        for (int i = 0; i < operations.Count; i++)
        {
            ValidAnnotatedEditOperation op = operations[i];
            if (canReduceOperations && op.IsTracked)
            {
                canReduceOperations = false;
            }

            string text = op.Text ?? string.Empty;
            if (text.Length > 0)
            {
                bool textMightContainNonBasicAscii = true;
                if (!mightContainNonBasicAscii)
                {
                    textMightContainNonBasicAscii = !StringClassification.IsBasicAscii(text);
                    mightContainNonBasicAscii = textMightContainNonBasicAscii;
                }

                if (!mightContainRtl && textMightContainNonBasicAscii)
                {
                    mightContainRtl = StringClassification.ContainsRtl(text);
                }

                if (!mightContainUnusualLineTerminators && textMightContainNonBasicAscii)
                {
                    mightContainUnusualLineTerminators = StringClassification.ContainsUnusualLineTerminators(text);
                }
            }

            string validText = string.Empty;
            EolCount eolCount = default;
            if (text.Length > 0)
            {
                eolCount = EolCounter.Count(text);
                validText = NormalizeLineBreaks(text, bufferEol);
            }

            validated.Add(new ValidatedEditOperation
            {
                SortIndex = i,
                Identifier = op.Identifier,
                Range = op.Range,
                RangeOffset = GetOffsetAt(op.Range.StartLineNumber, op.Range.StartColumn),
                RangeLength = GetValueLengthInRange(op.Range),
                Text = validText,
                EolCount = eolCount.Count,
                FirstLineLength = eolCount.FirstLineLength,
                LastLineLength = eolCount.LastLineLength,
                ForceMoveMarkers = op.ForceMoveMarkers,
                IsAutoWhitespaceEdit = op.IsAutoWhitespaceEdit,
            });
        }

        // Sort operations ascending
        validated.Sort(SortOpsAscending);

        bool hasTouchingRanges = false;
        for (int i = 0, count = validated.Count - 1; i < count; i++)
        {
            Position rangeEnd = validated[i].Range.End;
            Position nextRangeStart = validated[i + 1].Range.Start;

            if (nextRangeStart.IsBeforeOrEqual(rangeEnd))
            {
                if (nextRangeStart.IsBefore(rangeEnd))
                {
                    throw new ArgumentException("Overlapping ranges are not allowed!", nameof(operations));
                }

                hasTouchingRanges = true;
            }
        }

        List<ValidatedEditOperation> sorted = validated;
        if (canReduceOperations)
        {
            sorted = ReduceOperations(validated);
        }

        // Delta encode operations
        Range[] reverseRanges = computeUndoEdits || recordTrimAutoWhitespace ? GetInverseEditRanges(sorted) : [];
        List<(int LineNumber, string OldContent)>? trimCandidates = recordTrimAutoWhitespace ? [] : null;
        if (recordTrimAutoWhitespace)
        {
            for (int i = 0; i < sorted.Count; i++)
            {
                ValidatedEditOperation op = sorted[i];
                Range reverseRange = reverseRanges[i];

                if (op.IsAutoWhitespaceEdit && op.Range.IsEmpty)
                {
                    // Record already the future line numbers that might be auto whitespace removal candidates on next edit
                    for (int lineNumber = reverseRange.StartLineNumber; lineNumber <= reverseRange.EndLineNumber; lineNumber++)
                    {
                        string currentLineContent = string.Empty;
                        if (lineNumber == reverseRange.StartLineNumber)
                        {
                            currentLineContent = GetLineContent(op.Range.StartLineNumber);
                            if (StringClassification.FirstNonWhitespaceIndex(currentLineContent) != -1)
                            {
                                continue;
                            }
                        }

                        trimCandidates!.Add((lineNumber, currentLineContent));
                    }
                }
            }
        }

        List<ValidEditOperation>? reverseOperations = null;
        if (computeUndoEdits)
        {
            var reverse = new List<(int SortIndex, ValidEditOperation Edit)>(sorted.Count);
            int reverseRangeDeltaOffset = 0;
            for (int i = 0; i < sorted.Count; i++)
            {
                ValidatedEditOperation op = sorted[i];
                string bufferText = GetValueInRange(op.Range);
                int reverseRangeOffset = op.RangeOffset + reverseRangeDeltaOffset;
                reverseRangeDeltaOffset += op.Text.Length - bufferText.Length;

                reverse.Add((
                    op.SortIndex,
                    new ValidEditOperation(
                        op.Identifier,
                        reverseRanges[i],
                        bufferText,
                        new TextChange(op.RangeOffset, bufferText, reverseRangeOffset, op.Text))));
            }

            // Can only sort reverse operations when the order is not significant
            if (!hasTouchingRanges)
            {
                reverse.Sort(static (a, b) => a.SortIndex - b.SortIndex);
            }

            reverseOperations = new List<ValidEditOperation>(reverse.Count);
            foreach ((int _, ValidEditOperation edit) in reverse)
            {
                reverseOperations.Add(edit);
            }
        }

        _mightContainRtl = mightContainRtl;
        _mightContainUnusualLineTerminators = mightContainUnusualLineTerminators;
        _mightContainNonBasicAscii = mightContainNonBasicAscii;

        List<InternalModelContentChange> contentChanges = DoApplyEdits(sorted);

        List<int>? trimAutoWhitespaceLineNumbers = null;
        if (trimCandidates is { Count: > 0 })
        {
            // sort line numbers auto whitespace removal candidates for next edit descending
            trimCandidates.Sort(static (a, b) => b.LineNumber - a.LineNumber);

            trimAutoWhitespaceLineNumbers = [];
            for (int i = 0; i < trimCandidates.Count; i++)
            {
                int lineNumber = trimCandidates[i].LineNumber;
                if (i > 0 && trimCandidates[i - 1].LineNumber == lineNumber)
                {
                    // Do not have the same line number twice
                    continue;
                }

                string prevContent = trimCandidates[i].OldContent;
                string lineContent = GetLineContent(lineNumber);

                if (lineContent.Length == 0 || lineContent == prevContent || StringClassification.FirstNonWhitespaceIndex(lineContent) != -1)
                {
                    continue;
                }

                trimAutoWhitespaceLineNumbers.Add(lineNumber);
            }
        }

        return new ApplyEditsResult(reverseOperations, contentChanges, trimAutoWhitespaceLineNumbers);
    }

    /// <summary>Rewrites every line break of <paramref name="text"/> (<c>\r\n</c>, <c>\r</c>, <c>\n</c>) as <paramref name="eol"/>.</summary>
    private static string NormalizeLineBreaks(string text, string eol)
    {
        if (text.AsSpan().IndexOfAny('\r', '\n') < 0)
        {
            return text;
        }

        var sb = new System.Text.StringBuilder(text.Length + 8);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                sb.Append(eol);
            }
            else if (c == '\n')
            {
                sb.Append(eol);
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>Collapses a huge batch into one edit so that it cannot cause OOM crashes (Monaco: <c>_reduceOperations</c>).</summary>
    private List<ValidatedEditOperation> ReduceOperations(List<ValidatedEditOperation> operations)
    {
        if (operations.Count < ReduceOperationsThreshold)
        {
            return operations;
        }

        return [ToSingleEditOperation(operations)];
    }

    private List<InternalModelContentChange> DoApplyEdits(List<ValidatedEditOperation> operations)
    {
        // Sorted in place: the caller does not use the list afterwards (Monaco sorts its own array too).
        operations.Sort(SortOpsDescending);

        var contentChanges = new List<InternalModelContentChange>(operations.Count);

        // operations are from bottom to top
        foreach (ValidatedEditOperation op in operations)
        {
            if (op.Range.IsEmpty && op.Text.Length == 0)
            {
                // no-op
                continue;
            }

            if (op.Text.Length > 0)
            {
                // replacement
                _pieceTree.Delete(op.RangeOffset, op.RangeLength);
                _pieceTree.Insert(op.RangeOffset, op.Text, true);
            }
            else
            {
                // deletion
                _pieceTree.Delete(op.RangeOffset, op.RangeLength);
            }

            var change = new ModelContentChange(op.Range, op.RangeOffset, op.RangeLength, op.Text);
            contentChanges.Add(new InternalModelContentChange(change, op.ForceMoveMarkers));
        }

        return contentChanges;
    }
}
