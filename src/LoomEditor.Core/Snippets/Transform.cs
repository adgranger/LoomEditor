// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetParser.ts (Transform)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;

namespace LoomEditor.Core.Snippets;

/// <summary>The <c>/regex/format/flags</c> part of a placeholder or variable (Monaco: <c>Transform</c>). Its children are the format.</summary>
/// <remarks>
/// <para>
/// Behavior differences: the pattern is a .NET regular expression, built at run time and interpreted (Native AOT-safe). The flags
/// <c>i</c>, <c>m</c> and <c>s</c> map to the matching <see cref="RegexOptions"/>, <c>g</c> replaces every match, and <c>u</c>,
/// <c>v</c>, <c>y</c>, <c>d</c> are accepted and ignored. <see cref="ToTextmateString"/> and <see cref="Clone"/> keep every flag
/// letter (Monaco keeps <c>i</c> and <c>g</c> only). Named groups are numbered after the unnamed ones by .NET.
/// </para>
/// <para>Threading: not thread-safe (a snippet is owned by one session).</para>
/// </remarks>
internal sealed class Transform : SnippetMarker
{
    /// <summary>The compiled pattern (an empty pattern until <see cref="SetRegexp"/> is called).</summary>
    public Regex Regexp { get; private set; } = new(string.Empty, RegexOptions.CultureInvariant);

    /// <summary>The pattern as written in the snippet.</summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>The JavaScript flag letters as written in the snippet.</summary>
    public string Flags { get; private set; } = string.Empty;

    private bool IsGlobal => Flags.Contains('g', StringComparison.Ordinal);

    /// <summary>Builds <see cref="Regexp"/> from <paramref name="source"/> and the JavaScript <paramref name="flags"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="source"/> is not a valid pattern.</exception>
    public void SetRegexp(string source, string flags)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(flags);

        var options = RegexOptions.CultureInvariant;
        if (flags.Contains('i', StringComparison.Ordinal))
        {
            options |= RegexOptions.IgnoreCase;
        }

        if (flags.Contains('m', StringComparison.Ordinal))
        {
            options |= RegexOptions.Multiline;
        }

        if (flags.Contains('s', StringComparison.Ordinal))
        {
            options |= RegexOptions.Singleline;
        }

        Regexp = new Regex(source, options);
        Source = source;
        Flags = flags;
    }

    /// <summary>Replaces the matches of the pattern (all with <c>g</c>, else the first) by the format.</summary>
    public string Resolve(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        bool didMatch = false;
        string Evaluate(Match match)
        {
            didMatch = true;
            string[] groups = new string[match.Groups.Count];
            for (int i = 0; i < groups.Length; i++)
            {
                groups[i] = match.Groups[i].Value;
            }

            return Replace(groups);
        }

        string result = IsGlobal ? Regexp.Replace(value, Evaluate) : Regexp.Replace(value, Evaluate, 1);

        // When the regex did not match and the transform has else branches, run those.
        if (!didMatch && ChildList.Any(child => child is FormatString { ElseValue.Length: > 0 }))
        {
            result = Replace([]);
        }

        return result;
    }

    private string Replace(string[] groups)
    {
        var result = new System.Text.StringBuilder();
        foreach (SnippetMarker marker in ChildList)
        {
            if (marker is FormatString format)
            {
                string value = format.Index < groups.Length ? groups[format.Index] : string.Empty;
                result.Append(format.Resolve(value));
            }
            else
            {
                result.Append(marker);
            }
        }

        return result.ToString();
    }

    /// <summary>A transform inserts nothing by itself.</summary>
    public override string ToString() => string.Empty;

    /// <inheritdoc />
    public override string ToTextmateString() =>
        $"/{Source}/{string.Concat(Children.Select(child => child.ToTextmateString()))}/{Flags}";

    /// <inheritdoc />
    public override SnippetMarker Clone()
    {
        var clone = new Transform();
        clone.SetRegexp(Source, Flags);
        foreach (SnippetMarker child in ChildList)
        {
            SnippetMarker childClone = child.Clone();
            childClone.Parent = clone;
            clone.ChildList.Add(childClone);
        }

        return clone;
    }
}
