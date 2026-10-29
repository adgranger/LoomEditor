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
            return removed + " " + CommentStrings.DescribeLineDeleted;
        }

        string added = CommentStrings.DescribeAdded + string.Join("; ", Rows.Where(r => r.Kind == SuggestionDiffRowKind.Added).Select(r => Show(r.Text))) + ".";
        return removed.Length == 0 ? added : removed + " " + added;
    }

    private static string Show(string text) => text.Length == 0 ? CommentStrings.DescribeEmpty : text;
}

/// <summary>Computes the diff of a one-line replacement by a suggestion block.</summary>
/// <remarks>Threading: pure and thread-safe. No intra-line highlighting.</remarks>
internal static class SuggestionDiff
{
    /// <summary>Compares the old line (ordinally) to the lines of <paramref name="block"/>.</summary>
    public static SuggestionDiffResult Compute(string oldLine, SuggestionBlock block)
    {
        IReadOnlyList<string> lines = block.Lines;
        if (lines.Count == 0)
        {
            return new SuggestionDiffResult([new(SuggestionDiffRowKind.Removed, oldLine)], false, true);
        }

        if (lines.Count == 1 && lines[0] == oldLine)
        {
            return new SuggestionDiffResult([new(SuggestionDiffRowKind.Context, oldLine)], true, false);
        }

        var rows = new List<SuggestionDiffRow>();
        int same = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            if (string.Equals(lines[i], oldLine, StringComparison.Ordinal))
            {
                same = i;
                break;
            }
        }

        if (same < 0)
        {
            rows.Add(new(SuggestionDiffRowKind.Removed, oldLine));
        }

        for (int i = 0; i < lines.Count; i++)
        {
            rows.Add(new(i == same ? SuggestionDiffRowKind.Context : SuggestionDiffRowKind.Added, lines[i]));
        }

        return new SuggestionDiffResult(rows, false, false);
    }
}
