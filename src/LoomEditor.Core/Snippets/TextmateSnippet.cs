// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetParser.ts (TextmateSnippet)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>The placeholders of a snippet, and the one with the highest index.</summary>
/// <param name="All">Every placeholder, in the order of the tree walk.</param>
/// <param name="Last">The placeholder with the highest index (the first one met on a tie), or <see langword="null"/>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record PlaceholderInfo(IReadOnlyList<Placeholder> All, Placeholder? Last);

/// <summary>The root of a parsed snippet (Monaco: <c>TextmateSnippet</c>).</summary>
/// <remarks>Threading: not thread-safe (a snippet is owned by one session).</remarks>
internal sealed class TextmateSnippet : SnippetMarker
{
    private PlaceholderInfo? _placeholders;

    /// <summary>The placeholders, cached until the tree changes.</summary>
    public PlaceholderInfo PlaceholderInfo
    {
        get
        {
            if (_placeholders is null)
            {
                var all = new List<Placeholder>();
                Placeholder? last = null;
                Walk(candidate =>
                {
                    if (candidate is Placeholder placeholder)
                    {
                        all.Add(placeholder);
                        last = last is null || last.Index < placeholder.Index ? placeholder : last;
                    }

                    return true;
                });
                _placeholders = new PlaceholderInfo(all, last);
            }

            return _placeholders;
        }
    }

    /// <summary>Every placeholder, in the order of the tree walk.</summary>
    public IReadOnlyList<Placeholder> Placeholders => PlaceholderInfo.All;

    /// <summary>The offset of <paramref name="marker"/> in the inserted text, or <c>-1</c> when it is not in this snippet.</summary>
    public int Offset(SnippetMarker marker)
    {
        ArgumentNullException.ThrowIfNull(marker);

        int pos = 0;
        bool found = false;
        Walk(candidate =>
        {
            if (candidate == marker)
            {
                found = true;
                return false;
            }

            pos += candidate.Length;
            return true;
        });

        return found ? pos : -1;
    }

    /// <summary>The length of the text <paramref name="marker"/> and its descendants insert (Monaco: <c>fullLen</c>).</summary>
    public static int FullLength(SnippetMarker marker)
    {
        ArgumentNullException.ThrowIfNull(marker);

        int length = 0;
        SnippetMarker.Walk([marker], candidate =>
        {
            length += candidate.Length;
            return true;
        });
        return length;
    }

    /// <summary>The placeholders that contain <paramref name="placeholder"/>, innermost first.</summary>
    public static IReadOnlyList<Placeholder> EnclosingPlaceholders(Placeholder placeholder)
    {
        ArgumentNullException.ThrowIfNull(placeholder);

        var result = new List<Placeholder>();
        SnippetMarker? parent = placeholder.Parent;
        while (parent is not null)
        {
            if (parent is Placeholder enclosing)
            {
                result.Add(enclosing);
            }

            parent = parent.Parent;
        }

        return result;
    }

    /// <summary>Resolves every variable the resolver knows.</summary>
    /// <returns>This snippet.</returns>
    public TextmateSnippet ResolveVariables(IVariableResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        Walk(candidate =>
        {
            if (candidate is Variable variable && variable.Resolve(resolver))
            {
                _placeholders = null;
            }

            return true;
        });
        return this;
    }

    /// <inheritdoc />
    public override SnippetMarker AppendChild(SnippetMarker child)
    {
        _placeholders = null;
        return base.AppendChild(child);
    }

    /// <summary>Replaces <paramref name="child"/> by <paramref name="others"/> and drops the cached placeholders.</summary>
    /// <remarks>Hides the static <see cref="SnippetMarker.Replace"/>: Monaco overrides <c>replace</c> here.</remarks>
    public new void Replace(SnippetMarker child, IReadOnlyList<SnippetMarker> others)
    {
        _placeholders = null;
        SnippetMarker.Replace(child, others);
    }

    /// <inheritdoc />
    public override string ToTextmateString() => string.Concat(Children.Select(child => child.ToTextmateString()));

    /// <inheritdoc />
    public override SnippetMarker Clone()
    {
        var clone = new TextmateSnippet();
        foreach (SnippetMarker child in ChildList)
        {
            SnippetMarker childClone = child.Clone();
            childClone.Parent = clone;
            clone.ChildList.Add(childClone);
        }

        return clone;
    }

    /// <summary>Visits the descendants depth first; stops when <paramref name="visitor"/> returns <see langword="false"/>.</summary>
    public void Walk(Func<SnippetMarker, bool> visitor) => SnippetMarker.Walk(Children, visitor);
}
