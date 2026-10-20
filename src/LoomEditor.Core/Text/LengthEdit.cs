// Ported from microsoft/vscode: src/vs/editor/common/core/edits/lengthEdit.ts (LengthEdit), edits/edit.ts (BaseEdit)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

/// <summary>
/// Like a normal edit, but only captures the length information (Monaco: <c>LengthEdit</c>). The generic
/// <c>BaseEdit</c> is folded into this class, with the members that are used: construction checks, <c>equals</c>,
/// <c>normalize</c>, <c>compose</c>, <c>isEmpty</c>, <c>getLengthDelta</c>, <c>getNewDataLength</c>. Not ported:
/// <c>fromEdit</c> (no other kind of edit), <c>decomposeSplit</c> and the <c>applyToOffset*</c> family.
/// </summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class LengthEdit
{
    /// <summary><c>empty</c>.</summary>
    public static readonly LengthEdit Empty = new([]);

    /// <summary>Creates an edit; the replacements must be disjoint and sorted.</summary>
    /// <exception cref="ArgumentException">The replacements overlap or are not sorted.</exception>
    public LengthEdit(IReadOnlyList<LengthReplacement> replacements)
    {
        ArgumentNullException.ThrowIfNull(replacements);
        int lastEndEx = -1;
        foreach (LengthReplacement replacement in replacements)
        {
            if (replacement.ReplaceRange.Start < lastEndEx)
            {
                throw new ArgumentException($"Edits must be disjoint and sorted. Found {replacement} after {lastEndEx}");
            }

            lastEndEx = replacement.ReplaceRange.EndExclusive;
        }

        Replacements = replacements;
    }

    /// <summary><c>replacements</c>.</summary>
    public IReadOnlyList<LengthReplacement> Replacements { get; }

    /// <summary><c>isEmpty</c>.</summary>
    public bool IsEmpty => Replacements.Count == 0;

    /// <summary><c>create</c>.</summary>
    public static LengthEdit Create(IReadOnlyList<LengthReplacement> replacements) => new(replacements);

    /// <summary><c>single</c>.</summary>
    public static LengthEdit Single(LengthReplacement replacement) => new([replacement]);

    /// <summary><c>replace</c>.</summary>
    public static LengthEdit Replace(OffsetRange range, int newLength) => new([new LengthReplacement(range, newLength)]);

    /// <summary><c>insert</c>.</summary>
    public static LengthEdit Insert(int offset, int newLength) => new([new LengthReplacement(OffsetRange.EmptyAt(offset), newLength)]);

    /// <summary><c>delete</c>.</summary>
    public static LengthEdit Delete(OffsetRange range) => new([new LengthReplacement(range, 0)]);

    /// <summary><c>compose</c> (static): applies <paramref name="edits"/> one after the other.</summary>
    public static LengthEdit Compose(IEnumerable<LengthEdit> edits)
    {
        LengthEdit e = Empty;
        foreach (LengthEdit edit in edits)
        {
            e = e.Compose(edit);
        }

        return e;
    }

    /// <summary><c>equals</c>: structural equality, not equality of effect (see <see cref="Normalize"/>).</summary>
    public bool Equals(LengthEdit other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Replacements.Count == other.Replacements.Count && Replacements.Zip(other.Replacements).All(p => p.First.Equals(p.Second));
    }

    /// <summary><c>toString</c>.</summary>
    public override string ToString() => $"[{string.Join(", ", Replacements)}]";

    /// <summary>Creates an edit that reverts this edit (<c>inverse</c>).</summary>
    public LengthEdit Inverse()
    {
        var edits = new List<LengthReplacement>();
        int offset = 0;
        foreach (LengthReplacement e in Replacements)
        {
            edits.Add(new LengthReplacement(OffsetRange.OfStartAndLength(e.ReplaceRange.Start + offset, e.NewLength), e.ReplaceRange.Length));
            offset += e.NewLength - e.ReplaceRange.Length;
        }

        return new LengthEdit(edits);
    }

    /// <summary><c>getLengthDelta</c>.</summary>
    public int GetLengthDelta() => Replacements.Sum(r => r.GetLengthDelta());

    /// <summary><c>getNewDataLength</c>.</summary>
    public int GetNewDataLength(int dataLength) => dataLength + GetLengthDelta();

    /// <summary>
    /// <c>normalize</c>: removes empty replacements and joins touching replacements. Two edits have an equal normalized
    /// edit if and only if they have the same effect on any input.
    /// </summary>
    public LengthEdit Normalize()
    {
        var newReplacements = new List<LengthReplacement>();
        LengthReplacement? lastReplacement = null;
        foreach (LengthReplacement r in Replacements)
        {
            if (r.NewLength == 0 && r.ReplaceRange.Length == 0)
            {
                continue;
            }

            if (lastReplacement is not null && lastReplacement.ReplaceRange.EndExclusive == r.ReplaceRange.Start)
            {
                lastReplacement = lastReplacement.TryJoinTouching(r);
                continue;
            }

            if (lastReplacement is not null)
            {
                newReplacements.Add(lastReplacement);
            }

            lastReplacement = r;
        }

        if (lastReplacement is not null)
        {
            newReplacements.Add(lastReplacement);
        }

        return new LengthEdit(newReplacements);
    }

    /// <summary>Combines two edits into one with the same effect: <c>other.apply(this.apply(s0)) = this.compose(other).apply(s0)</c>.</summary>
    public LengthEdit Compose(LengthEdit other)
    {
        ArgumentNullException.ThrowIfNull(other);
        LengthEdit edits1 = Normalize();
        LengthEdit edits2 = other.Normalize();

        if (edits1.IsEmpty)
        {
            return edits2;
        }

        if (edits2.IsEmpty)
        {
            return edits1;
        }

        var edit1Queue = new List<LengthReplacement>(edits1.Replacements);
        var result = new List<LengthReplacement>();

        int edit1ToEdit2 = 0;

        foreach (LengthReplacement r2 in edits2.Replacements)
        {
            // Copy over edit1 unmodified until it touches edit2.
            while (edit1Queue.Count > 0)
            {
                LengthReplacement r1 = edit1Queue[0];
                if (r1.ReplaceRange.Start + edit1ToEdit2 + r1.NewLength >= r2.ReplaceRange.Start)
                {
                    break;
                }

                edit1Queue.RemoveAt(0);
                result.Add(r1);
                edit1ToEdit2 += r1.NewLength - r1.ReplaceRange.Length;
            }

            int firstEdit1ToEdit2 = edit1ToEdit2;
            LengthReplacement? firstIntersecting = null; // or touching
            LengthReplacement? lastIntersecting = null; // or touching

            while (edit1Queue.Count > 0)
            {
                LengthReplacement r1 = edit1Queue[0];
                if (r1.ReplaceRange.Start + edit1ToEdit2 > r2.ReplaceRange.EndExclusive)
                {
                    break;
                }

                // else we intersect, because the new end of edit1 is after or equal to our start
                firstIntersecting ??= r1;
                lastIntersecting = r1;
                edit1Queue.RemoveAt(0);

                edit1ToEdit2 += r1.NewLength - r1.ReplaceRange.Length;
            }

            if (firstIntersecting is null)
            {
                result.Add(r2.Delta(-edit1ToEdit2));
                continue;
            }

            int newReplaceRangeStart = Math.Min(firstIntersecting.ReplaceRange.Start, r2.ReplaceRange.Start - firstEdit1ToEdit2);

            int prefixLength = r2.ReplaceRange.Start - (firstIntersecting.ReplaceRange.Start + firstEdit1ToEdit2);
            if (prefixLength > 0)
            {
                result.Add(LengthReplacement.Slice(OffsetRange.EmptyAt(newReplaceRangeStart), new OffsetRange(0, prefixLength)));
            }

            if (lastIntersecting is null)
            {
                throw new InvalidOperationException("Invariant violation: lastIntersecting is undefined");
            }

            int suffixLength = lastIntersecting.ReplaceRange.EndExclusive + edit1ToEdit2 - r2.ReplaceRange.EndExclusive;
            if (suffixLength > 0)
            {
                LengthReplacement e = LengthReplacement.Slice(
                    OffsetRange.OfStartAndLength(lastIntersecting.ReplaceRange.EndExclusive, 0),
                    new OffsetRange(lastIntersecting.NewLength - suffixLength, lastIntersecting.NewLength));
                edit1Queue.Insert(0, e);
                edit1ToEdit2 -= e.NewLength - e.ReplaceRange.Length;
            }

            var newReplaceRange = new OffsetRange(newReplaceRangeStart, r2.ReplaceRange.EndExclusive - edit1ToEdit2);
            result.Add(LengthReplacement.Slice(newReplaceRange, new OffsetRange(0, r2.NewLength)));
        }

        result.AddRange(edit1Queue);

        return new LengthEdit(result).Normalize();
    }

    /// <summary>
    /// <c>applyArray</c>: copies <paramref name="arr"/>, dropping the replaced items and inserting
    /// <paramref name="fillItem"/> for every new item.
    /// </summary>
    public T[] ApplyArray<T>(IReadOnlyList<T> arr, T fillItem)
    {
        ArgumentNullException.ThrowIfNull(arr);
        var newArr = new T[GetNewDataLength(arr.Count)];

        int srcPos = 0;
        int dstPos = 0;

        foreach (LengthReplacement replacement in Replacements)
        {
            // Copy items before the current replacement
            for (int i = srcPos; i < replacement.ReplaceRange.Start; i++)
            {
                newArr[dstPos++] = arr[i];
            }

            // Skip the replaced items in the source array
            srcPos = replacement.ReplaceRange.EndExclusive;

            // Fill with the provided fillItem for insertions
            for (int i = 0; i < replacement.NewLength; i++)
            {
                newArr[dstPos++] = fillItem;
            }
        }

        // Copy any remaining items from the original array
        while (srcPos < arr.Count)
        {
            newArr[dstPos++] = arr[srcPos++];
        }

        return newArr;
    }
}
