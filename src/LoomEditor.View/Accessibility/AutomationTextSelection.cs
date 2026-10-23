using LoomEditor.Core.Text;

namespace LoomEditor.View.Accessibility;

/// <summary>The pure half of <c>Select</c>, <c>AddToSelection</c> and <c>RemoveFromSelection</c>: from the current selections and a range to the new selections.</summary>
/// <remarks>Threading: stateless, any thread.</remarks>
internal static class AutomationTextSelection
{
    /// <summary>The range becomes the only selection (anchor at its start, position at its end).</summary>
    public static IReadOnlyList<Selection> Select(IReadOnlyList<Selection> current, Range range) => [FromRange(range)];

    /// <summary>The current selections followed by the range (the cursor model merges overlapping ones).</summary>
    public static IReadOnlyList<Selection> Add(IReadOnlyList<Selection> current, Range range) => [.. current, FromRange(range)];

    /// <summary>
    /// The current selections without the first one whose range equals <paramref name="range"/> (whatever its direction); unchanged when there is none.
    /// Removing the only selection collapses it to its position.
    /// </summary>
    public static IReadOnlyList<Selection> Remove(IReadOnlyList<Selection> current, Range range)
    {
        int index = -1;
        for (int i = 0; i < current.Count; i++)
        {
            if (current[i].Range == range)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return current;
        }

        if (current.Count == 1)
        {
            return [Selection.Collapsed(current[0].Position)];
        }

        return [.. current.Where((_, i) => i != index)];
    }

    private static Selection FromRange(Range range) => new(range.StartLineNumber, range.StartColumn, range.EndLineNumber, range.EndColumn);
}
