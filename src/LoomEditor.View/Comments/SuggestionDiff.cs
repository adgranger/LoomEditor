namespace LoomEditor.View.Comments;

/// <summary>Kind of a row of the suggested change diff.</summary>
internal enum SuggestionDiffRowKind
{
    /// <summary>Unchanged line.</summary>
    Context,

    /// <summary>Line removed (red).</summary>
    Removed,

    /// <summary>Line added (green).</summary>
    Added,
}

/// <summary>One row of the suggested change diff.</summary>
/// <param name="Kind">The kind of row.</param>
/// <param name="Text">The line text.</param>
/// <remarks>Threading: immutable, thread-safe.</remarks>
internal sealed record SuggestionDiffRow(SuggestionDiffRowKind Kind, string Text)
{
    /// <summary>The gutter marker: space, <c>-</c> or <c>+</c>.</summary>
    public char Marker => Kind switch
    {
        SuggestionDiffRowKind.Removed => '-',
        SuggestionDiffRowKind.Added => '+',
        _ => ' ',
    };
}

/// <summary>The rows of a suggested change and its screen-reader description.</summary>
/// <param name="Rows">The rows, top to bottom.</param>
/// <param name="IsNoOp">The suggestion equals the old line.</param>
/// <param name="IsDeletion">The suggestion deletes the line.</param>
/// <remarks>Threading: immutable, thread-safe.</remarks>
internal sealed record SuggestionDiffResult(IReadOnlyList<SuggestionDiffRow> Rows, bool IsNoOp, bool IsDeletion)
{
    /// <summary>A plain-text description for UI Automation.</summary>
    public string Describe()
    {
        if (IsNoOp)
        {
            return CommentStrings.NoChanges + ".";
        }

        string removed = string.Join(" ", Rows.Where(r => r.Kind == SuggestionDiffRowKind.Removed).Select(r => CommentStrings.DescribeRemoved + Show(r.Text) + "."));
        if (IsDeletion)
        {
            bool many = Rows.Count(r => r.Kind == SuggestionDiffRowKind.Removed) > 1;
            return removed + " " + (many ? CommentStrings.DescribeLinesDeleted : CommentStrings.DescribeLineDeleted);
        }

        string added = CommentStrings.DescribeAdded + string.Join("; ", Rows.Where(r => r.Kind == SuggestionDiffRowKind.Added).Select(r => Show(r.Text))) + ".";
        return removed.Length == 0 ? added : removed + " " + added;
    }

    private static string Show(string text) => text.Length == 0 ? CommentStrings.DescribeEmpty : text;
}

/// <summary>Computes the diff of a range of lines replaced by a suggestion block (line LCS).</summary>
/// <remarks>Threading: pure and thread-safe. No intra-line highlighting.</remarks>
internal static class SuggestionDiff
{
    private const long MaxTableCells = 250_000;

    /// <summary>Compares the old line (ordinally) to the lines of <paramref name="block"/>.</summary>
    public static SuggestionDiffResult Compute(string oldLine, SuggestionBlock block) => Compute([oldLine], block);

    /// <summary>Compares the old lines (ordinally) to the lines of <paramref name="block"/>.</summary>
    public static SuggestionDiffResult Compute(IReadOnlyList<string> oldLines, SuggestionBlock block)
    {
        if (oldLines.Count == 0)
        {
            throw new ArgumentException("At least one old line is required.", nameof(oldLines));
        }

        IReadOnlyList<string> lines = block.Lines;
        if (lines.Count == 0)
        {
            return new SuggestionDiffResult([.. oldLines.Select(l => new SuggestionDiffRow(SuggestionDiffRowKind.Removed, l))], false, true);
        }

        if (lines.SequenceEqual(oldLines, StringComparer.Ordinal))
        {
            return new SuggestionDiffResult([.. oldLines.Select(l => new SuggestionDiffRow(SuggestionDiffRowKind.Context, l))], true, false);
        }

        var rows = new List<SuggestionDiffRow>();
        int n = oldLines.Count;
        int m = lines.Count;
        if ((long)n * m > MaxTableCells)
        {
            rows.AddRange(oldLines.Select(l => new SuggestionDiffRow(SuggestionDiffRowKind.Removed, l)));
            rows.AddRange(lines.Select(l => new SuggestionDiffRow(SuggestionDiffRowKind.Added, l)));
            return new SuggestionDiffResult(rows, false, false);
        }

        var dp = new int[n + 1, m + 1];
        for (int a = n - 1; a >= 0; a--)
        {
            for (int b = m - 1; b >= 0; b--)
            {
                dp[a, b] = string.Equals(oldLines[a], lines[b], StringComparison.Ordinal)
                    ? dp[a + 1, b + 1] + 1
                    : Math.Max(dp[a + 1, b], dp[a, b + 1]);
            }
        }

        int i = 0;
        int j = 0;
        while (i < n && j < m)
        {
            if (string.Equals(oldLines[i], lines[j], StringComparison.Ordinal))
            {
                rows.Add(new(SuggestionDiffRowKind.Context, oldLines[i]));
                i++;
                j++;
            }
            else if (dp[i + 1, j] >= dp[i, j + 1])
            {
                rows.Add(new(SuggestionDiffRowKind.Removed, oldLines[i++]));
            }
            else
            {
                rows.Add(new(SuggestionDiffRowKind.Added, lines[j++]));
            }
        }

        for (; i < n; i++)
        {
            rows.Add(new(SuggestionDiffRowKind.Removed, oldLines[i]));
        }

        for (; j < m; j++)
        {
            rows.Add(new(SuggestionDiffRowKind.Added, lines[j]));
        }

        return new SuggestionDiffResult(rows, false, false);
    }
}
