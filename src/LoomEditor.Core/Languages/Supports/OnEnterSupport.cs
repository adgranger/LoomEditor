// Ported from microsoft/vscode: src/vs/editor/common/languages/supports/onEnter.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using System.Text.RegularExpressions;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Languages.Supports;

/// <summary>Decides what pressing Enter does from the on-enter rules and the brackets of a language (Monaco: <c>OnEnterSupport</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed class OnEnterSupport
{
    /// <summary>The brackets used when the language has none: <c>()</c>, <c>{}</c>, <c>[]</c>.</summary>
    public static readonly IReadOnlyList<CharacterPair> DefaultBrackets =
    [
        new("(", ")"),
        new("{", "}"),
        new("[", "]"),
    ];

    private readonly List<ProcessedBracketPair> _brackets;
    private readonly IReadOnlyList<OnEnterRule> _regExpRules;

    public OnEnterSupport(IReadOnlyList<CharacterPair>? brackets, IReadOnlyList<OnEnterRule>? onEnterRules)
    {
        brackets ??= DefaultBrackets;

        _brackets = [];
        foreach (CharacterPair bracket in brackets)
        {
            Regex? openRegExp = CreateOpenBracketRegExp(bracket.Open);
            Regex? closeRegExp = CreateCloseBracketRegExp(bracket.Close);
            if (openRegExp is not null && closeRegExp is not null)
            {
                _brackets.Add(new ProcessedBracketPair(bracket.Open, bracket.Close, openRegExp, closeRegExp));
            }
        }

        _regExpRules = onEnterRules ?? [];
    }

    public EnterAction? OnEnter(EditorAutoIndentStrategy autoIndent, string previousLineText, string beforeEnterText, string afterEnterText)
    {
        // (1): `regExpRules`
        if (autoIndent >= EditorAutoIndentStrategy.Advanced)
        {
            foreach (OnEnterRule rule in _regExpRules)
            {
                // Monaco resets `lastIndex` to disable the effect of the "g" flag; .NET regexes are stateless.
                bool regResult = rule.BeforeText.IsMatch(beforeEnterText)
                    && (rule.AfterText?.IsMatch(afterEnterText) ?? true)
                    && (rule.PreviousLineText?.IsMatch(previousLineText) ?? true);

                if (regResult)
                {
                    return rule.Action;
                }
            }
        }

        // (2): Special indent-outdent
        if (autoIndent >= EditorAutoIndentStrategy.Brackets)
        {
            if (beforeEnterText.Length > 0 && afterEnterText.Length > 0)
            {
                foreach (ProcessedBracketPair bracket in _brackets)
                {
                    if (bracket.OpenRegExp.IsMatch(beforeEnterText) && bracket.CloseRegExp.IsMatch(afterEnterText))
                    {
                        return new EnterAction(IndentAction.IndentOutdent);
                    }
                }
            }
        }

        // (4): Open bracket based logic
        if (autoIndent >= EditorAutoIndentStrategy.Brackets)
        {
            if (beforeEnterText.Length > 0)
            {
                foreach (ProcessedBracketPair bracket in _brackets)
                {
                    if (bracket.OpenRegExp.IsMatch(beforeEnterText))
                    {
                        return new EnterAction(IndentAction.Indent);
                    }
                }
            }
        }

        return null;
    }

    private static Regex? CreateOpenBracketRegExp(string bracket)
    {
        string str = Strings.EscapeRegExpCharacters(bracket);
        // JavaScript's `!/\B/.test(str.charAt(0))`: the first character is an ASCII word character.
        if (str.Length > 0 && IsAsciiWordCharacter(str[0]))
        {
            str = @"\b" + str;
        }

        str += @"\s*$";
        return SafeRegExp(str);
    }

    private static Regex? CreateCloseBracketRegExp(string bracket)
    {
        string str = Strings.EscapeRegExpCharacters(bracket);
        // JavaScript's `!/\B/.test(str.charAt(str.length - 1))`: the last character is an ASCII word character.
        if (str.Length > 0 && IsAsciiWordCharacter(str[^1]))
        {
            str += @"\b";
        }

        str = @"^\s*" + str;
        return SafeRegExp(str);
    }

    private static bool IsAsciiWordCharacter(char c) => char.IsAsciiLetterOrDigit(c) || c == '_';

    private static Regex? SafeRegExp(string def)
    {
        try
        {
            return new Regex(def, RegexOptions.CultureInvariant);
        }
        catch (ArgumentException err)
        {
            Debug.WriteLine(err);
            return null;
        }
    }

    /// <summary>A bracket pair and its regexes (Monaco: <c>IProcessedBracketPair</c>).</summary>
    private readonly record struct ProcessedBracketPair(string Open, string Close, Regex OpenRegExp, Regex CloseRegExp);
}
