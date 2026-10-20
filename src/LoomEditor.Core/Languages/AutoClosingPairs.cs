// Ported from microsoft/vscode: src/vs/editor/common/languages/languageConfiguration.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Languages;

/// <summary>An open and a close string (Monaco: <c>IAutoClosingPair</c>; TypeScript's structural typing lets any pair type be used).</summary>
internal interface IAutoClosingPair
{
    /// <summary>The open string.</summary>
    string Open { get; }

    /// <summary>The close string.</summary>
    string Close { get; }
}

/// <summary>A pair of strings (Monaco: <c>IAutoClosingPair</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record AutoClosingPair(string Open, string Close) : IAutoClosingPair;

/// <summary>A pair, and the token types where it is not auto closed (Monaco: <c>IAutoClosingPairConditional</c>).</summary>
/// <param name="Open">The open string.</param>
/// <param name="Close">The close string.</param>
/// <param name="NotIn"><c>"string"</c>, <c>"comment"</c> or <c>"regex"</c>; other values are ignored.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record AutoClosingPairConditional(string Open, string Close, IReadOnlyList<string>? NotIn = null) : IAutoClosingPair;

/// <summary>An auto closing pair with its <c>notIn</c> resolved (Monaco: <c>StandardAutoClosingPairConditional</c>).</summary>
/// <remarks>Threading: not thread-safe (the neutral character is cached on first use).</remarks>
internal sealed class StandardAutoClosingPairConditional : IAutoClosingPair
{
    private readonly bool _inString;
    private readonly bool _inComment;
    private readonly bool _inRegEx;
    private string? _neutralCharacter;
    private bool _neutralCharacterSearched;

    public StandardAutoClosingPairConditional(AutoClosingPairConditional source)
    {
        Open = source.Open;
        Close = source.Close;

        // initially allowed in all tokens
        _inString = true;
        _inComment = true;
        _inRegEx = true;

        if (source.NotIn is { } notInList)
        {
            foreach (string notIn in notInList)
            {
                switch (notIn)
                {
                    case "string":
                        _inString = false;
                        break;
                    case "comment":
                        _inComment = false;
                        break;
                    case "regex":
                        _inRegEx = false;
                        break;
                }
            }
        }
    }

    public string Open { get; }

    public string Close { get; }

    public bool IsOK(StandardTokenType standardToken) => standardToken switch
    {
        StandardTokenType.Other => true,
        StandardTokenType.Comment => _inComment,
        StandardTokenType.String => _inString,
        StandardTokenType.RegEx => _inRegEx,
        // TypeScript's switch has no default.
        _ => true,
    };

    public bool ShouldAutoClose(ScopedLineTokens context, int column)
    {
        // Always complete on empty line
        if (context.GetTokenCount() == 0)
        {
            return true;
        }

        int tokenIndex = context.FindTokenIndexAtOffset(column - 2);
        StandardTokenType standardTokenType = context.GetStandardTokenType(tokenIndex);
        return IsOK(standardTokenType);
    }

    /// <summary>Find a character in the range [0-9a-zA-Z] that does not appear in the open or close.</summary>
    public string? FindNeutralCharacter()
    {
        if (!_neutralCharacterSearched)
        {
            _neutralCharacterSearched = true;
            _neutralCharacter ??= FindNeutralCharacterInRange('0', '9');
            _neutralCharacter ??= FindNeutralCharacterInRange('a', 'z');
            _neutralCharacter ??= FindNeutralCharacterInRange('A', 'Z');
        }

        return _neutralCharacter;
    }

    private string? FindNeutralCharacterInRange(char fromCharCode, char toCharCode)
    {
        for (char charCode = fromCharCode; charCode <= toCharCode; charCode++)
        {
            string character = charCode.ToString();
            if (!Open.Contains(character, StringComparison.Ordinal) && !Close.Contains(character, StringComparison.Ordinal))
            {
                return character;
            }
        }

        return null;
    }
}

/// <summary>
/// The auto closing pairs of a language, indexed by either end of open and close (Monaco: <c>AutoClosingPairs</c>).
/// Keys are strings: the first or last character of an empty string is <c>""</c>, a valid key.
/// </summary>
/// <remarks>Threading: immutable after construction.</remarks>
internal sealed class AutoClosingPairs
{
    public AutoClosingPairs(IReadOnlyList<StandardAutoClosingPairConditional> autoClosingPairs)
    {
        var openByStart = new Dictionary<string, List<StandardAutoClosingPairConditional>>(StringComparer.Ordinal);
        var openByEnd = new Dictionary<string, List<StandardAutoClosingPairConditional>>(StringComparer.Ordinal);
        var closeByStart = new Dictionary<string, List<StandardAutoClosingPairConditional>>(StringComparer.Ordinal);
        var closeByEnd = new Dictionary<string, List<StandardAutoClosingPairConditional>>(StringComparer.Ordinal);
        var closeSingleChar = new Dictionary<string, List<StandardAutoClosingPairConditional>>(StringComparer.Ordinal);
        foreach (StandardAutoClosingPairConditional pair in autoClosingPairs)
        {
            AppendEntry(openByStart, CharAt(pair.Open, 0), pair);
            AppendEntry(openByEnd, CharAt(pair.Open, pair.Open.Length - 1), pair);
            AppendEntry(closeByStart, CharAt(pair.Close, 0), pair);
            AppendEntry(closeByEnd, CharAt(pair.Close, pair.Close.Length - 1), pair);
            if (pair.Close.Length == 1 && pair.Open.Length == 1)
            {
                AppendEntry(closeSingleChar, pair.Close, pair);
            }
        }

        AutoClosingPairsOpenByStart = openByStart;
        AutoClosingPairsOpenByEnd = openByEnd;
        AutoClosingPairsCloseByStart = closeByStart;
        AutoClosingPairsCloseByEnd = closeByEnd;
        AutoClosingPairsCloseSingleChar = closeSingleChar;
    }

    /// <summary>Key is first character of open.</summary>
    public IReadOnlyDictionary<string, List<StandardAutoClosingPairConditional>> AutoClosingPairsOpenByStart { get; }

    /// <summary>Key is last character of open.</summary>
    public IReadOnlyDictionary<string, List<StandardAutoClosingPairConditional>> AutoClosingPairsOpenByEnd { get; }

    /// <summary>Key is first character of close.</summary>
    public IReadOnlyDictionary<string, List<StandardAutoClosingPairConditional>> AutoClosingPairsCloseByStart { get; }

    /// <summary>Key is last character of close.</summary>
    public IReadOnlyDictionary<string, List<StandardAutoClosingPairConditional>> AutoClosingPairsCloseByEnd { get; }

    /// <summary>Key is close. Only has pairs that are a single character.</summary>
    public IReadOnlyDictionary<string, List<StandardAutoClosingPairConditional>> AutoClosingPairsCloseSingleChar { get; }

    // JavaScript's `charAt`: "" out of range.
    private static string CharAt(string text, int index) => index >= 0 && index < text.Length ? text[index].ToString() : "";

    private static void AppendEntry(Dictionary<string, List<StandardAutoClosingPairConditional>> target, string key, StandardAutoClosingPairConditional value)
    {
        if (target.TryGetValue(key, out List<StandardAutoClosingPairConditional>? list))
        {
            list.Add(value);
        }
        else
        {
            target[key] = [value];
        }
    }
}
