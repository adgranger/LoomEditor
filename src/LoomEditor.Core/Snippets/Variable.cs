// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetParser.ts (Variable)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>A <c>${name}</c> or <c>$name</c> variable; its children are the default value (Monaco: <c>Variable</c>).</summary>
/// <remarks>Threading: not thread-safe (a snippet is owned by one session).</remarks>
internal sealed class Variable(string name) : TransformableMarker
{
    /// <summary>The variable name, without the dollar.</summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Asks <paramref name="resolver"/> for the value, applies the transform, and replaces the children by the value.
    /// Returns <see langword="false"/> (children kept as the default value) when the variable is unknown.
    /// </summary>
    public bool Resolve(IVariableResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        string? value = resolver.Resolve(this);
        if (Transform is { } transform)
        {
            value = transform.Resolve(value ?? string.Empty);
        }

        if (value is null)
        {
            return false;
        }

        var text = new TextMarker(value) { Parent = this };
        ChildList = [text];
        return true;
    }

    /// <inheritdoc />
    public override string ToTextmateString()
    {
        string transformString = Transform?.ToTextmateString() ?? string.Empty;
        return Children.Count == 0
            ? $"${{{Name}{transformString}}}"
            : $"${{{Name}:{string.Concat(Children.Select(child => child.ToTextmateString()))}{transformString}}}";
    }

    /// <inheritdoc />
    public override SnippetMarker Clone()
    {
        var clone = new Variable(Name);
        if (Transform is { } transform)
        {
            clone.Transform = (Transform)transform.Clone();
        }

        foreach (SnippetMarker child in ChildList)
        {
            SnippetMarker childClone = child.Clone();
            childClone.Parent = clone;
            clone.ChildList.Add(childClone);
        }

        return clone;
    }
}
