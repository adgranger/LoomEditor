// Ported from microsoft/vscode: src/vs/editor/common/languages/supports/electricCharacter.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages.Supports;

/// <summary>
/// The line will be indented at the same level of the line which contains the matching given bracket type
/// (Monaco: <c>IElectricAction</c>).
/// </summary>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct ElectricAction(string MatchOpenBracket);

/// <summary>Tells when typing a closing bracket should re-indent the line (Monaco: <c>BracketElectricCharacterSupport</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class BracketElectricCharacterSupport(RichEditBrackets? richEditBrackets)
{
    private readonly RichEditBrackets? _richEditBrackets = richEditBrackets;

    /// <summary>The last character of every close bracket, distinct, in first occurrence order.</summary>
    public IReadOnlyList<string> GetElectricCharacters()
    {
        var result = new List<string>();
        if (_richEditBrackets is not null)
        {
            foreach (RichEditBracket bracket in _richEditBrackets.Brackets)
            {
                foreach (string close in bracket.Close)
                {
                    string lastChar = close.Length > 0 ? close[^1].ToString() : "";
                    result.Add(lastChar);
                }
            }
        }

        return [.. result.Distinct(StringComparer.Ordinal)];
    }

    public ElectricAction? OnElectricCharacter(string character, ScopedLineTokens context, int column)
    {
        if (_richEditBrackets is null || _richEditBrackets.Brackets.Count == 0)
        {
            return null;
        }

        int tokenIndex = context.FindTokenIndexAtOffset(column - 1);
        if (ScopedLineTokens.IgnoreBracketsInToken(context.GetStandardTokenType(tokenIndex)))
        {
            return null;
        }

        string lineContent = context.GetLineContent();
        // JavaScript's `substring` clamps its end to the length.
        string text = lineContent[..Math.Min(column - 1, lineContent.Length)] + character;

        if (BracketsUtils.FindPrevBracketInRange(_richEditBrackets.ReversedRegex, 1, text, 0, text.Length) is not { } r)
        {
            return null;
        }

        string bracketText = text.Substring(r.StartColumn - 1, r.EndColumn - r.StartColumn).ToLowerInvariant();

        bool isOpen = _richEditBrackets.TextIsOpenBracket.TryGetValue(bracketText, out bool value) && value;
        if (isOpen)
        {
            return null;
        }

        string textBeforeBracket = context.GetActualLineContentBefore(r.StartColumn - 1);
        // `/^\s*$/`: JavaScript's `\s` and char.IsWhiteSpace differ only on U+0085 and U+FEFF.
        if (!string.IsNullOrWhiteSpace(textBeforeBracket))
        {
            // There is other text on the line before the bracket
            return null;
        }

        return new ElectricAction(bracketText);
    }
}
