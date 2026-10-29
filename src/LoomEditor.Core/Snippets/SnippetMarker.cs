// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetParser.ts (Marker)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>A node of a snippet tree (Monaco: <c>Marker</c>; renamed because <c>LoomEditor.Core.Markers.Marker</c> is a diagnostic).</summary>
/// <remarks>Threading: not thread-safe (a snippet is owned by one session).</remarks>
internal abstract class SnippetMarker
{
    private static int _markerIdCounter;

    protected List<SnippetMarker> ChildList = [];

    /// <summary>Monaco: <c>_markerId</c>, unique for the process.</summary>
    public int Id { get; } = ++_markerIdCounter;

    /// <summary>The parent, or <see langword="null"/> for a root.</summary>
    public SnippetMarker? Parent { get; set; }

    /// <summary>The children, in order.</summary>
    public IReadOnlyList<SnippetMarker> Children => ChildList;

    /// <summary>The deepest last descendant, or this marker if it has no child (Monaco: <c>rightMostDescendant</c>).</summary>
    public SnippetMarker RightMostDescendant => ChildList.Count > 0 ? ChildList[^1].RightMostDescendant : this;

    /// <summary>The snippet this marker is in, or <see langword="null"/> (Monaco: <c>snippet</c>).</summary>
    public TextmateSnippet? Snippet
    {
        get
        {
            SnippetMarker? candidate = this;
            while (candidate is not null)
            {
                if (candidate is TextmateSnippet snippet)
                {
                    return snippet;
                }

                candidate = candidate.Parent;
            }

            return null;
        }
    }

    /// <summary>The length of the text this marker inserts (Monaco: <c>len</c>).</summary>
    public virtual int Length => 0;

    /// <summary>Adds a child. Two consecutive text markers are merged (Monaco: <c>appendChild</c>).</summary>
    public virtual SnippetMarker AppendChild(SnippetMarker child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (child is TextMarker text && ChildList.Count > 0 && ChildList[^1] is TextMarker previous)
        {
            // This and the previous child are text: merge them.
            previous.Value += text.Value;
        }
        else
        {
            // Normal adoption of the child.
            child.Parent = this;
            ChildList.Add(child);
        }

        return this;
    }

    /// <summary>Replaces <paramref name="child"/> by <paramref name="others"/> in its parent and re-parents them (Monaco: <c>replace</c>, static because it never uses the instance).</summary>
    public static void Replace(SnippetMarker child, IReadOnlyList<SnippetMarker> others)
    {
        ArgumentNullException.ThrowIfNull(child);
        ArgumentNullException.ThrowIfNull(others);

        SnippetMarker parent = child.Parent!;
        int index = parent.ChildList.IndexOf(child);
        var newChildren = new List<SnippetMarker>(parent.ChildList);
        newChildren.RemoveAt(index);
        newChildren.InsertRange(index, others);
        parent.ChildList = newChildren;

        FixParent(others, parent);
    }

    /// <summary>Visits <paramref name="markers"/> and their descendants depth first; stops when <paramref name="visitor"/> returns <see langword="false"/>.</summary>
    public static void Walk(IEnumerable<SnippetMarker> markers, Func<SnippetMarker, bool> visitor)
    {
        ArgumentNullException.ThrowIfNull(markers);
        ArgumentNullException.ThrowIfNull(visitor);

        var stack = new Stack<SnippetMarker>(markers.Reverse());
        while (stack.Count > 0)
        {
            SnippetMarker marker = stack.Pop();
            if (!visitor(marker))
            {
                break;
            }

            // The children are read after the visit: a visitor may replace them (variable resolution).
            foreach (SnippetMarker child in marker.Children.Reverse())
            {
                stack.Push(child);
            }
        }
    }

    private static void FixParent(IReadOnlyList<SnippetMarker> children, SnippetMarker parent)
    {
        foreach (SnippetMarker child in children)
        {
            child.Parent = parent;
            FixParent(child.Children, child);
        }
    }

    /// <summary>The children concatenated.</summary>
    public override string ToString() => string.Concat(Children.Select(child => child.ToString()));

    /// <summary>The snippet syntax that parses back to this marker.</summary>
    public abstract string ToTextmateString();

    /// <summary>A deep copy.</summary>
    public abstract SnippetMarker Clone();
}
