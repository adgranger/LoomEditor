namespace LoomEditor.View.DiffEditor;

/// <summary>
/// The selection of changed lines of a diff editor: a set of (side, line) restricted to the lines of the change blocks, plus the anchor of a range extension.
/// <c>modified</c> false is the original side. Lines are 1-based model lines of that side.
/// </summary>
/// <remarks>
/// Every mutator returns true only when the set of selected lines really changed. A pair is "same index in the same block": original line <c>o + i</c> pairs with
/// modified line <c>m + i</c> when <c>i</c> is below both line counts of the block.
/// <para>Threading: UI thread only (not synchronized).</para>
/// </remarks>
internal sealed class DiffLineSelection
{
    private readonly HashSet<int> _original = [];
    private readonly HashSet<int> _modified = [];
    private IReadOnlyList<DiffChangeBlock> _blocks = [];
    private (bool Modified, int Line)? _anchor;
    private (bool Modified, int Line)[]? _lines;

    /// <summary>The selected lines: the original side ascending, then the modified side ascending.</summary>
    public IReadOnlyList<(bool Modified, int Line)> Lines =>
        _lines ??= [.. _original.Order().Select(l => (false, l)), .. _modified.Order().Select(l => (true, l))];

    /// <summary>Replaces the blocks. <paramref name="keepSelection"/> false clears the selection and the anchor; true drops the lines that are no longer changed lines.</summary>
    public bool SetBlocks(IReadOnlyList<DiffChangeBlock> blocks, bool keepSelection)
    {
        _blocks = blocks;
        if (!keepSelection)
        {
            return Clear();
        }

        if (_anchor is { } anchor && !IsChangedLine(anchor.Modified, anchor.Line))
        {
            _anchor = null;
        }

        int removed = _original.RemoveWhere(l => !IsChangedLine(false, l)) + _modified.RemoveWhere(l => !IsChangedLine(true, l));
        return Changed(removed > 0);
    }

    /// <summary>Whether the line is inside a change block of that side.</summary>
    public bool IsChangedLine(bool modified, int line) => FindBlock(modified, line) >= 0;

    /// <summary>Whether the line is selected.</summary>
    public bool IsSelected(bool modified, int line) => Side(modified).Contains(line);

    /// <summary>Flips one changed line and makes it the anchor.</summary>
    public bool Toggle(bool modified, int line)
    {
        if (!IsChangedLine(modified, line))
        {
            return false;
        }

        HashSet<int> set = Side(modified);
        if (!set.Remove(line))
        {
            set.Add(line);
        }

        _anchor = (modified, line);
        return Changed(true);
    }

    /// <summary>Selects the line and its partner if either is unselected, else unselects both; without a partner, as <see cref="Toggle"/>. The anchor is the line.</summary>
    public bool TogglePair(bool modified, int line)
    {
        int index = FindBlock(modified, line);
        if (index < 0)
        {
            return false;
        }

        DiffChangeBlock block = _blocks[index];
        int offset = line - Start(block, modified);
        if (offset >= Count(block, !modified))
        {
            return Toggle(modified, line);
        }

        int partner = Start(block, !modified) + offset;
        HashSet<int> set = Side(modified);
        HashSet<int> other = Side(!modified);
        if (set.Contains(line) && other.Contains(partner))
        {
            set.Remove(line);
            other.Remove(partner);
        }
        else
        {
            set.Add(line);
            other.Add(partner);
        }

        _anchor = (modified, line);
        return Changed(true);
    }

    /// <summary>
    /// Adds every changed line of that side between the anchor and <paramref name="line"/>, inclusive; the anchor does not move. Without an anchor, or with the anchor
    /// on the other side, as <see cref="Toggle"/>.
    /// </summary>
    public bool ExtendTo(bool modified, int line)
    {
        if (_anchor is not { } anchor || anchor.Modified != modified)
        {
            return Toggle(modified, line);
        }

        int from = Math.Min(anchor.Line, line);
        int to = Math.Max(anchor.Line, line);
        HashSet<int> set = Side(modified);
        bool changed = false;
        for (int i = Math.Max(LastBlockStartingAtOrBefore(modified, from), 0); i < _blocks.Count && Start(_blocks[i], modified) <= to; i++)
        {
            int start = Start(_blocks[i], modified);
            int end = Math.Min(start + Count(_blocks[i], modified) - 1, to);
            for (int l = Math.Max(start, from); l <= end; l++)
            {
                changed |= set.Add(l);
            }
        }

        return Changed(changed);
    }

    /// <summary>Replaces the selection; lines that are not changed lines are ignored. Clears the anchor.</summary>
    public bool Set(IEnumerable<(bool modified, int line)> lines)
    {
        var original = new HashSet<int>();
        var modified = new HashSet<int>();
        foreach ((bool isModified, int line) in lines)
        {
            if (IsChangedLine(isModified, line))
            {
                (isModified ? modified : original).Add(line);
            }
        }

        _anchor = null;
        if (original.SetEquals(_original) && modified.SetEquals(_modified))
        {
            return false;
        }

        _original.Clear();
        _original.UnionWith(original);
        _modified.Clear();
        _modified.UnionWith(modified);
        return Changed(true);
    }

    /// <summary>Unselects every line and clears the anchor.</summary>
    public bool Clear()
    {
        _anchor = null;
        bool changed = _original.Count > 0 || _modified.Count > 0;
        _original.Clear();
        _modified.Clear();
        return Changed(changed);
    }

    private static int Start(DiffChangeBlock block, bool modified) => modified ? block.ModifiedStartLineNumber : block.OriginalStartLineNumber;

    private static int Count(DiffChangeBlock block, bool modified) => modified ? block.ModifiedLineCount : block.OriginalLineCount;

    private HashSet<int> Side(bool modified) => modified ? _modified : _original;

    private bool Changed(bool changed)
    {
        if (changed)
        {
            _lines = null;
        }

        return changed;
    }

    private int FindBlock(bool modified, int line)
    {
        int index = LastBlockStartingAtOrBefore(modified, line);
        return index >= 0 && line < Start(_blocks[index], modified) + Count(_blocks[index], modified) ? index : -1;
    }

    // Blocks are separated by unchanged lines, so their starts are strictly increasing on each side.
    private int LastBlockStartingAtOrBefore(bool modified, int line)
    {
        int low = 0;
        int high = _blocks.Count - 1;
        int found = -1;
        while (low <= high)
        {
            int mid = low + ((high - low) / 2);
            if (Start(_blocks[mid], modified) <= line)
            {
                found = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return found;
    }
}
