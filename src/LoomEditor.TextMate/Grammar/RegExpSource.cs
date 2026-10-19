// Ported from microsoft/vscode-textmate: src/rule.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LoomEditor.TextMate.Oniguruma;

namespace LoomEditor.TextMate.Grammar;

/// <summary>
/// <c>RegExpSource</c>: the source of one rule regex, with <c>\z</c> rewritten, its <c>\A</c> / <c>\G</c> variants
/// (<c>IRegExpSourceAnchorCache</c>) and its back references to the begin captures.
/// </summary>
/// <remarks>Threading: single-threaded, like the grammar that owns it.</remarks>
internal sealed partial class RegExpSource
{
    // A disallowed anchor: the letter of \A or \G becomes U+FFFF (an escaped U+FFFF matches nothing in source text).
    private const char Disallowed = char.MaxValue;

    private AnchorCache? _anchorCache;

    /// <summary>Creates a source: <c>\z</c> becomes <c>$(?!\n)(?&lt;!\n)</c>, and <c>\A</c> / <c>\G</c> are detected.</summary>
    public RegExpSource(string regExpSource, RuleId ruleId)
    {
        ArgumentNullException.ThrowIfNull(regExpSource);
        if (regExpSource.Length != 0)
        {
            int len = regExpSource.Length;
            int lastPushedPos = 0;
            var output = new StringBuilder();

            bool hasAnchor = false;
            for (int pos = 0; pos < len; pos++)
            {
                char ch = regExpSource[pos];

                if (ch == '\\')
                {
                    if (pos + 1 < len)
                    {
                        char nextCh = regExpSource[pos + 1];
                        if (nextCh == 'z')
                        {
                            output.Append(regExpSource, lastPushedPos, pos - lastPushedPos);
                            output.Append("$(?!\\n)(?<!\\n)");
                            lastPushedPos = pos + 2;
                        }
                        else if (nextCh is 'A' or 'G')
                        {
                            hasAnchor = true;
                        }

                        pos++;
                    }
                }
            }

            HasAnchor = hasAnchor;
            if (lastPushedPos == 0)
            {
                // No \z hit
                Source = regExpSource;
            }
            else
            {
                output.Append(regExpSource, lastPushedPos, len - lastPushedPos);
                Source = output.ToString();
            }
        }
        else
        {
            HasAnchor = false;
            Source = regExpSource;
        }

        _anchorCache = HasAnchor ? BuildAnchorCache() : null;

        RuleId = ruleId;
        HasBackReferences = HasBackReferencesRegex().IsMatch(Source);
    }

    /// <summary><c>source</c>.</summary>
    public string Source { get; private set; }

    /// <summary><c>ruleId</c>: the rule this regex belongs to, or <see cref="RuleId.End"/> / <see cref="RuleId.While"/>.</summary>
    public RuleId RuleId { get; }

    /// <summary><c>hasAnchor</c>: whether the source contains <c>\A</c> or <c>\G</c>.</summary>
    public bool HasAnchor { get; }

    /// <summary><c>hasBackReferences</c>: whether the source contains <c>\1</c>, <c>\2</c>, ….</summary>
    public bool HasBackReferences { get; }

    /// <summary><c>clone</c>.</summary>
    public RegExpSource Clone() => new(Source, RuleId);

    /// <summary><c>setSource</c>.</summary>
    public void SetSource(string newSource)
    {
        if (Source == newSource)
        {
            return;
        }

        Source = newSource;

        if (HasAnchor)
        {
            _anchorCache = BuildAnchorCache();
        }
    }

    /// <summary>
    /// <c>resolveBackReferences</c>: replaces each <c>\N</c> with the escaped text of capture N of
    /// <paramref name="lineText"/> ("" when there is no such capture).
    /// </summary>
    public string ResolveBackReferences(string lineText, IReadOnlyList<OnigCaptureIndex> captureIndices)
    {
        string[] capturedValues = [.. captureIndices.Select(capture => Strings.JsSubstring(lineText, capture.Start, capture.End))];
        return BackReferencingEnd().Replace(Source, match =>
        {
            bool found = int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int index)
                && index < capturedValues.Length;
            return Strings.EscapeRegExpCharacters(found ? capturedValues[index] : string.Empty);
        });
    }

    /// <summary><c>resolveAnchors</c>: the source where a disallowed <c>\A</c> / <c>\G</c> becomes U+FFFF.</summary>
    public string ResolveAnchors(bool allowA, bool allowG)
    {
        if (!HasAnchor || _anchorCache is not AnchorCache cache)
        {
            return Source;
        }

        if (allowA)
        {
            return allowG ? cache.A1G1 : cache.A1G0;
        }

        return allowG ? cache.A0G1 : cache.A0G0;
    }

    // _buildAnchorCache
    private AnchorCache BuildAnchorCache()
    {
        char[] a0G0 = Source.ToCharArray();
        char[] a0G1 = Source.ToCharArray();
        char[] a1G0 = Source.ToCharArray();
        char[] a1G1 = Source.ToCharArray();

        for (int pos = 0, len = Source.Length; pos < len; pos++)
        {
            char ch = Source[pos];
            if (ch == '\\')
            {
                if (pos + 1 < len)
                {
                    char nextCh = Source[pos + 1];
                    if (nextCh == 'A')
                    {
                        a0G0[pos + 1] = Disallowed;
                        a0G1[pos + 1] = Disallowed;
                        a1G0[pos + 1] = 'A';
                        a1G1[pos + 1] = 'A';
                    }
                    else if (nextCh == 'G')
                    {
                        a0G0[pos + 1] = Disallowed;
                        a0G1[pos + 1] = 'G';
                        a1G0[pos + 1] = Disallowed;
                        a1G1[pos + 1] = 'G';
                    }

                    pos++;
                }
            }
        }

        return new AnchorCache(new string(a0G0), new string(a0G1), new string(a1G0), new string(a1G1));
    }

    // HAS_BACK_REFERENCES: /\\(\d+)/ with ASCII digits.
    [GeneratedRegex(@"\\([0-9]+)", RegexOptions.CultureInvariant)]
    private static partial Regex HasBackReferencesRegex();

    // BACK_REFERENCING_END: /\\(\d+)/g with ASCII digits.
    [GeneratedRegex(@"\\([0-9]+)", RegexOptions.CultureInvariant)]
    private static partial Regex BackReferencingEnd();

    // IRegExpSourceAnchorCache
    private readonly record struct AnchorCache(string A0G0, string A0G1, string A1G0, string A1G1);
}
