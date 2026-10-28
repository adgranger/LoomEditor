// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/computeMovedLines.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Text;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Diff;

internal static partial class ComputeMovedLines
{
    private sealed class PossibleMapping(LineRange modifiedLineRange, LineRange originalLineRange)
    {
        public LineRange ModifiedLineRange { get; set; } = modifiedLineRange;

        public LineRange OriginalLineRange { get; set; } = originalLineRange;
    }

    private static List<LineRangeMapping> ComputeUnchangedMoves(
        List<DetailedLineRangeMapping> changes,
        int[] hashedOriginalLines,
        int[] hashedModifiedLines,
        string[] originalLines,
        string[] modifiedLines,
        ITimeout timeout)
    {
        var moves = new List<LineRangeMapping>();

        // Monaco's SetMap<string, { range }>: the sets are only added to and iterated in insertion order.
        var original3LineHashes = new Dictionary<string, List<LineRange>>();

        foreach (DetailedLineRangeMapping change in changes)
        {
            for (int i = change.Original.StartLineNumber; i < change.Original.EndLineNumberExclusive - 2; i++)
            {
                string key = $"{hashedOriginalLines[i - 1]}:{hashedOriginalLines[i + 1 - 1]}:{hashedOriginalLines[i + 2 - 1]}";
                if (!original3LineHashes.TryGetValue(key, out List<LineRange>? set))
                {
                    set = [];
                    original3LineHashes[key] = set;
                }

                set.Add(new LineRange(i, i + 3));
            }
        }

        var possibleMappings = new List<PossibleMapping>();

        // Monaco sorts the (already filtered, so private) list in place: OrderBy is stable like JS sort.
        changes = changes.OrderBy(c => c.Modified.StartLineNumber).ToList();

        foreach (DetailedLineRangeMapping change in changes)
        {
            var lastMappings = new List<PossibleMapping>();
            for (int i = change.Modified.StartLineNumber; i < change.Modified.EndLineNumberExclusive - 2; i++)
            {
                string key = $"{hashedModifiedLines[i - 1]}:{hashedModifiedLines[i + 1 - 1]}:{hashedModifiedLines[i + 2 - 1]}";
                var currentModifiedRange = new LineRange(i, i + 3);

                var nextMappings = new List<PossibleMapping>();
                if (original3LineHashes.TryGetValue(key, out List<LineRange>? ranges))
                {
                    foreach (LineRange range in ranges)
                    {
                        PossibleMapping? extended = null;
                        foreach (PossibleMapping lastMapping in lastMappings)
                        {
                            // does this match extend some last match?
                            if (lastMapping.OriginalLineRange.EndLineNumberExclusive + 1 == range.EndLineNumberExclusive
                                && lastMapping.ModifiedLineRange.EndLineNumberExclusive + 1 == currentModifiedRange.EndLineNumberExclusive)
                            {
                                lastMapping.OriginalLineRange = new LineRange(lastMapping.OriginalLineRange.StartLineNumber, range.EndLineNumberExclusive);
                                lastMapping.ModifiedLineRange = new LineRange(lastMapping.ModifiedLineRange.StartLineNumber, currentModifiedRange.EndLineNumberExclusive);
                                extended = lastMapping;
                                break;
                            }
                        }

                        if (extended is not null)
                        {
                            nextMappings.Add(extended);
                            continue;
                        }

                        var mapping = new PossibleMapping(currentModifiedRange, range);
                        possibleMappings.Add(mapping);
                        nextMappings.Add(mapping);
                    }
                }

                lastMappings = nextMappings;
            }

            if (!timeout.IsValid())
            {
                return [];
            }
        }

        possibleMappings = possibleMappings.OrderByDescending(m => m.ModifiedLineRange.Length).ToList();

        var modifiedSet = new LineRangeSet();
        var originalSet = new LineRangeSet();

        foreach (PossibleMapping mapping in possibleMappings)
        {
            int diffOrigToMod = mapping.ModifiedLineRange.StartLineNumber - mapping.OriginalLineRange.StartLineNumber;
            LineRangeSet modifiedSections = modifiedSet.SubtractFrom(mapping.ModifiedLineRange);
            LineRangeSet originalTranslatedSections = originalSet.SubtractFrom(mapping.OriginalLineRange).GetWithDelta(diffOrigToMod);

            LineRangeSet modifiedIntersectedSections = modifiedSections.GetIntersection(originalTranslatedSections);

            foreach (LineRange s in modifiedIntersectedSections.Ranges)
            {
                if (s.Length < 3)
                {
                    continue;
                }

                LineRange modifiedLineRange = s;
                LineRange originalLineRange = s.Delta(-diffOrigToMod);

                moves.Add(new LineRangeMapping(originalLineRange, modifiedLineRange));

                modifiedSet.AddRange(modifiedLineRange);
                originalSet.AddRange(originalLineRange);
            }
        }

        moves = moves.OrderBy(m => m.Original.StartLineNumber).ToList();

        for (int i = 0; i < moves.Count; i++)
        {
            LineRangeMapping move = moves[i];
            DetailedLineRangeMapping firstTouchingChangeOrig = FindLastMonotonous(changes, c => c.Original.StartLineNumber <= move.Original.StartLineNumber)!;
            DetailedLineRangeMapping firstTouchingChangeMod = FindLastMonotonous(changes, c => c.Modified.StartLineNumber <= move.Modified.StartLineNumber)!;
            int linesAbove = Math.Max(
                move.Original.StartLineNumber - firstTouchingChangeOrig.Original.StartLineNumber,
                move.Modified.StartLineNumber - firstTouchingChangeMod.Modified.StartLineNumber);

            DetailedLineRangeMapping lastTouchingChangeOrig = FindLastMonotonous(changes, c => c.Original.StartLineNumber < move.Original.EndLineNumberExclusive)!;
            DetailedLineRangeMapping lastTouchingChangeMod = FindLastMonotonous(changes, c => c.Modified.StartLineNumber < move.Modified.EndLineNumberExclusive)!;
            int linesBelow = Math.Max(
                lastTouchingChangeOrig.Original.EndLineNumberExclusive - move.Original.EndLineNumberExclusive,
                lastTouchingChangeMod.Modified.EndLineNumberExclusive - move.Modified.EndLineNumberExclusive);

            int extendToTop;
            for (extendToTop = 0; extendToTop < linesAbove; extendToTop++)
            {
                int origLine = move.Original.StartLineNumber - extendToTop - 1;
                int modLine = move.Modified.StartLineNumber - extendToTop - 1;
                if (origLine > originalLines.Length || modLine > modifiedLines.Length)
                {
                    break;
                }

                if (modifiedSet.Contains(modLine) || originalSet.Contains(origLine))
                {
                    break;
                }

                if (!AreLinesSimilar(originalLines[origLine - 1], modifiedLines[modLine - 1], timeout))
                {
                    break;
                }
            }

            if (extendToTop > 0)
            {
                originalSet.AddRange(new LineRange(move.Original.StartLineNumber - extendToTop, move.Original.StartLineNumber));
                modifiedSet.AddRange(new LineRange(move.Modified.StartLineNumber - extendToTop, move.Modified.StartLineNumber));
            }

            int extendToBottom;
            for (extendToBottom = 0; extendToBottom < linesBelow; extendToBottom++)
            {
                int origLine = move.Original.EndLineNumberExclusive + extendToBottom;
                int modLine = move.Modified.EndLineNumberExclusive + extendToBottom;
                if (origLine > originalLines.Length || modLine > modifiedLines.Length)
                {
                    break;
                }

                if (modifiedSet.Contains(modLine) || originalSet.Contains(origLine))
                {
                    break;
                }

                if (!AreLinesSimilar(originalLines[origLine - 1], modifiedLines[modLine - 1], timeout))
                {
                    break;
                }
            }

            if (extendToBottom > 0)
            {
                originalSet.AddRange(new LineRange(move.Original.EndLineNumberExclusive, move.Original.EndLineNumberExclusive + extendToBottom));
                modifiedSet.AddRange(new LineRange(move.Modified.EndLineNumberExclusive, move.Modified.EndLineNumberExclusive + extendToBottom));
            }

            if (extendToTop > 0 || extendToBottom > 0)
            {
                moves[i] = new LineRangeMapping(
                    new LineRange(move.Original.StartLineNumber - extendToTop, move.Original.EndLineNumberExclusive + extendToBottom),
                    new LineRange(move.Modified.StartLineNumber - extendToTop, move.Modified.EndLineNumberExclusive + extendToBottom));
            }
        }

        return moves;
    }

    // Monaco's Range constructor swaps (1,1,1,0) into an empty range; ours rejects column 0, hence Math.Max(1, ...).
    private static bool AreLinesSimilar(string line1, string line2, ITimeout timeout)
    {
        if (DefaultLinesDiffComputer.TrimJs(line1) == DefaultLinesDiffComputer.TrimJs(line2))
        {
            return true;
        }

        if (line1.Length > 300 && line2.Length > 300)
        {
            return false;
        }

        DiffAlgorithmResult result = new MyersDiffAlgorithm().Compute(
            new LinesSliceCharSequence([line1], new Range(1, 1, 1, Math.Max(1, line1.Length)), false),
            new LinesSliceCharSequence([line2], new Range(1, 1, 1, Math.Max(1, line2.Length)), false),
            timeout);

        int commonNonSpaceCharCount = 0;
        foreach (SequenceDiff seq in SequenceDiff.Invert([.. result.Diffs], line1.Length))
        {
            for (int idx = seq.Seq1Range.Start; idx < seq.Seq1Range.EndExclusive; idx++)
            {
                if (!LineRangeFragment.IsSpace(line1[idx]))
                {
                    commonNonSpaceCharCount++;
                }
            }
        }

        // Faithful to Monaco: the loop bound is line1.Length even when counting the characters of line2.
        int CountNonWsChars(string str)
        {
            int count = 0;
            for (int i = 0; i < line1.Length; i++)
            {
                // JS charCodeAt is NaN out of range, which is not a space.
                if (i >= str.Length || !LineRangeFragment.IsSpace(str[i]))
                {
                    count++;
                }
            }

            return count;
        }

        int longerLineLength = CountNonWsChars(line1.Length > line2.Length ? line1 : line2);
        return (double)commonNonSpaceCharCount / longerLineLength > 0.6 && longerLineLength > 10;
    }
}
