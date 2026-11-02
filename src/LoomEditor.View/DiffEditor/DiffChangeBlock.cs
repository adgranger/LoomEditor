using LoomEditor.Core.Diff;

namespace LoomEditor.View.DiffEditor;

/// <summary>One block of changed lines of a diff: a range of original lines replaced by a range of modified lines.</summary>
/// <remarks>
/// <para>
/// Line numbers are 1-based lines of each side's text. A count of 0 is an empty side: a pure insertion (original) or a pure deletion (modified). The start line of an
/// empty side is the line <b>before which</b> the other side's lines sit; it can be that side's line count + 1. A git hunk header uses the line <b>after which</b>
/// instead: for an empty side, its start is <c>start - 1</c>.
/// </para>
/// <para>
/// The blocks follow the diff options: with <c>IgnoreTrimWhitespace</c> on (the default), a change of leading or trailing whitespace only is not a block.
/// </para>
/// <para>Threading: immutable, safe from any thread.</para>
/// </remarks>
public sealed class DiffChangeBlock
{
    internal DiffChangeBlock(int originalStartLineNumber, int originalLineCount, int modifiedStartLineNumber, int modifiedLineCount, IReadOnlyList<DiffInnerChange> innerChanges)
    {
        OriginalStartLineNumber = originalStartLineNumber;
        OriginalLineCount = originalLineCount;
        ModifiedStartLineNumber = modifiedStartLineNumber;
        ModifiedLineCount = modifiedLineCount;
        InnerChanges = innerChanges;
    }

    /// <summary>The first original line of the block; for a pure insertion, the original line the inserted lines sit before.</summary>
    public int OriginalStartLineNumber { get; }

    /// <summary>The number of original lines; 0 for a pure insertion.</summary>
    public int OriginalLineCount { get; }

    /// <summary>The first modified line of the block; for a pure deletion, the modified line the deleted lines sat before.</summary>
    public int ModifiedStartLineNumber { get; }

    /// <summary>The number of modified lines; 0 for a pure deletion.</summary>
    public int ModifiedLineCount { get; }

    /// <summary>The character-level changes inside the block, in order; empty when the diff algorithm gave none.</summary>
    public IReadOnlyList<DiffInnerChange> InnerChanges { get; }

    /// <summary><c>"{original start},{count} -> {modified start},{count}"</c>, for debugging.</summary>
    public override string ToString() => $"{OriginalStartLineNumber},{OriginalLineCount} -> {ModifiedStartLineNumber},{ModifiedLineCount}";

    internal static IReadOnlyList<DiffChangeBlock> FromMappings(IReadOnlyList<DiffMapping> mappings)
    {
        var blocks = new DiffChangeBlock[mappings.Count];
        for (int i = 0; i < blocks.Length; i++)
        {
            DetailedLineRangeMapping m = mappings[i].LineRangeMapping;
            DiffInnerChange[] inner = m.InnerChanges is { } changes ? [.. changes.Select(c => new DiffInnerChange(c.OriginalRange, c.ModifiedRange))] : [];
            blocks[i] = new DiffChangeBlock(m.Original.StartLineNumber, m.Original.Length, m.Modified.StartLineNumber, m.Modified.Length, inner);
        }

        return blocks;
    }
}
