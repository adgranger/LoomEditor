// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetParser.ts (TransformableMarker, Placeholder)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>A tabstop, with or without a default value (Monaco: <c>Placeholder</c>).</summary>
/// <remarks>
/// The <c>transform</c> of <c>TransformableMarker</c> is not ported yet (P10-20: <c>Transform</c> does not exist), so
/// <see cref="ToTextmateString"/> and <see cref="Clone"/> ignore it for now.
/// Threading: not thread-safe (a snippet is owned by one session).
/// </remarks>
internal sealed class Placeholder(int index) : SnippetMarker
{
    /// <summary>The tabstop number; <c>0</c> is the final tabstop.</summary>
    public int Index { get; set; } = index;

    /// <summary>True for <c>$0</c>.</summary>
    public bool IsFinalTabstop => Index == 0;

    /// <summary>The choice when it is the only child, else <see langword="null"/>.</summary>
    public Choice? Choice => ChildList.Count == 1 ? ChildList[0] as Choice : null;

    /// <summary>Orders by index, the final tabstop (<c>0</c>) after all the others (Monaco: <c>compareByIndex</c>).</summary>
    public static int CompareByIndex(Placeholder a, Placeholder b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.Index == b.Index)
        {
            return 0;
        }

        if (a.IsFinalTabstop)
        {
            return 1;
        }

        if (b.IsFinalTabstop)
        {
            return -1;
        }

        return a.Index < b.Index ? -1 : 1;
    }

    /// <inheritdoc />
    public override string ToTextmateString()
    {
        if (ChildList.Count == 0)
        {
            return $"${Index}";
        }

        if (Choice is { } choice)
        {
            return $"${{{Index}|{choice.ToTextmateString()}|}}";
        }

        return $"${{{Index}:{string.Concat(ChildList.Select(child => child.ToTextmateString()))}}}";
    }

    /// <inheritdoc />
    public override SnippetMarker Clone()
    {
        var clone = new Placeholder(Index);
        foreach (SnippetMarker child in ChildList)
        {
            SnippetMarker childClone = child.Clone();
            childClone.Parent = clone;
            clone.ChildList.Add(childClone);
        }

        return clone;
    }
}
