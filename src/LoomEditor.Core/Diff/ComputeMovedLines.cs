// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/computeMovedLines.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Diff.Algorithms;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Diff;

/// <summary>Finds the blocks of lines that moved between the two texts (Monaco: <c>computeMovedLines</c>).</summary>
/// <remarks>Threading: stateless apart from the per-call context.</remarks>
internal static partial class ComputeMovedLines
{
    /// <summary><c>computeMovedLines</c>. The <paramref name="changes"/> list is not modified.</summary>
    public static IReadOnlyList<LineRangeMapping> Compute(
        IReadOnlyList<DetailedLineRangeMapping> changes,
        string[] originalLines,
        string[] modifiedLines,
        int[] hashedOriginalLines,
        int[] hashedModifiedLines,
        ITimeout timeout)
    {
        var context = new LineRangeFragmentContext();
        (List<LineRangeMapping> moves, HashSet<DetailedLineRangeMapping> excludedChanges) =
            ComputeMovesFromSimpleDeletionsToSimpleInsertions(changes, originalLines, modifiedLines, timeout, context);

        if (!timeout.IsValid())
        {
            return [];
        }

        List<DetailedLineRangeMapping> filteredChanges = changes.Where(c => !excludedChanges.Contains(c)).ToList();
        moves.AddRange(ComputeUnchangedMoves(filteredChanges, hashedOriginalLines, hashedModifiedLines, originalLines, modifiedLines, timeout));

        moves = JoinCloseConsecutiveMoves(moves);

        // Ignore too short moves
        moves = moves.Where(current =>
        {
            string[] lines = originalLines.Skip(current.Original.StartLineNumber - 1).Take(current.Original.Length)
                .Select(DefaultLinesDiffComputer.TrimJs).ToArray();
            string originalText = string.Join('\n', lines);
            return originalText.Length >= 15 && lines.Count(l => l.Length >= 2) >= 2;
        }).ToList();

        return RemoveMovesInSameDiff(changes, moves);
    }

    private static (List<LineRangeMapping> Moves, HashSet<DetailedLineRangeMapping> ExcludedChanges) ComputeMovesFromSimpleDeletionsToSimpleInsertions(
        IReadOnlyList<DetailedLineRangeMapping> changes, string[] originalLines, string[] modifiedLines, ITimeout timeout, LineRangeFragmentContext context)
    {
        var moves = new List<LineRangeMapping>();

        List<LineRangeFragment> deletions = changes
            .Where(c => c.Modified.IsEmpty && c.Original.Length >= 3)
            .Select(d => new LineRangeFragment(d.Original, originalLines, d, context))
            .ToList();
        List<LineRangeFragment> insertions = changes
            .Where(c => c.Original.IsEmpty && c.Modified.Length >= 3)
            .Select(d => new LineRangeFragment(d.Modified, modifiedLines, d, context))
            .ToList();

        var excludedChanges = new HashSet<DetailedLineRangeMapping>();

        foreach (LineRangeFragment deletion in deletions)
        {
            double highestSimilarity = -1;
            LineRangeFragment? best = null;
            foreach (LineRangeFragment insertion in insertions)
            {
                double similarity = deletion.ComputeSimilarity(insertion);
                if (similarity > highestSimilarity)
                {
                    highestSimilarity = similarity;
                    best = insertion;
                }
            }

            if (highestSimilarity > 0.90 && best is not null)
            {
                insertions.Remove(best);
                moves.Add(new LineRangeMapping(deletion.Range, best.Range));
                excludedChanges.Add(deletion.Source);
                excludedChanges.Add(best.Source);
            }

            if (!timeout.IsValid())
            {
                return (moves, excludedChanges);
            }
        }

        return (moves, excludedChanges);
    }

    private static List<LineRangeMapping> JoinCloseConsecutiveMoves(List<LineRangeMapping> moves)
    {
        if (moves.Count == 0)
        {
            return moves;
        }

        List<LineRangeMapping> sorted = moves.OrderBy(m => m.Original.StartLineNumber).ToList();

        var result = new List<LineRangeMapping> { sorted[0] };
        for (int i = 1; i < sorted.Count; i++)
        {
            LineRangeMapping last = result[^1];
            LineRangeMapping current = sorted[i];

            int originalDist = current.Original.StartLineNumber - last.Original.EndLineNumberExclusive;
            int modifiedDist = current.Modified.StartLineNumber - last.Modified.EndLineNumberExclusive;
            bool currentMoveAfterLast = originalDist >= 0 && modifiedDist >= 0;

            if (currentMoveAfterLast && originalDist + modifiedDist <= 2)
            {
                result[^1] = last.Join(current);
                continue;
            }

            result.Add(current);
        }

        return result;
    }

    private static List<LineRangeMapping> RemoveMovesInSameDiff(IReadOnlyList<DetailedLineRangeMapping> changes, List<LineRangeMapping> moves) =>
        moves.Where(m =>
        {
            LineRangeMapping diffBeforeEndOfMoveOriginal =
                FindLastMonotonous(changes, c => c.Original.StartLineNumber < m.Original.EndLineNumberExclusive)
                ?? new LineRangeMapping(new LineRange(1, 1), new LineRange(1, 1));
            DetailedLineRangeMapping? diffBeforeEndOfMoveModified =
                FindLastMonotonous(changes, c => c.Modified.StartLineNumber < m.Modified.EndLineNumberExclusive);

            return !ReferenceEquals(diffBeforeEndOfMoveOriginal, diffBeforeEndOfMoveModified);
        }).ToList();

    /// <summary><c>findLastMonotonous</c> (also used for Monaco's <c>MonotonousArray</c>, which only caches the search position).</summary>
    private static T? FindLastMonotonous<T>(IReadOnlyList<T> array, Func<T, bool> predicate)
        where T : class
    {
        int low = 0;
        int high = array.Count;
        while (low < high)
        {
            int mid = low + ((high - low) / 2);
            if (predicate(array[mid]))
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low == 0 ? null : array[low - 1];
    }
}
