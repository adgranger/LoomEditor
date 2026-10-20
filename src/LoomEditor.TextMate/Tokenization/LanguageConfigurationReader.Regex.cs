// Ported from microsoft/vscode: src/vs/workbench/contrib/codeEditor/common/languageConfigurationExtensionPoint.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;

namespace LoomEditor.TextMate.Tokenization;

/// <summary>The sections made of regular expressions, and the JavaScript → .NET regex mapping (ADR 0009, decision 6).</summary>
/// <remarks>Threading: stateless.</remarks>
internal static partial class LanguageConfigurationReader
{
    private static List<OnEnterRule>? ExtractValidOnEnterRules(string languageId, JsonObject configuration, Action<string> warn)
    {
        if (!TryGetArray(languageId, configuration, "onEnterRules", warn, out JsonArray? source))
        {
            return null;
        }

        List<OnEnterRule>? result = null;
        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] is not JsonObject onEnterRule)
            {
                warn($"[{languageId}]: language configuration: expected `onEnterRules[{i}]` to be an object.");
                continue;
            }

            if (onEnterRule["action"] is not JsonObject actionObject)
            {
                warn($"[{languageId}]: language configuration: expected `onEnterRules[{i}].action` to be an object.");
                continue;
            }

            IndentAction indentAction;
            switch (AsString(actionObject["indent"]))
            {
                case "none":
                    indentAction = IndentAction.None;
                    break;
                case "indent":
                    indentAction = IndentAction.Indent;
                    break;
                case "indentOutdent":
                    indentAction = IndentAction.IndentOutdent;
                    break;
                case "outdent":
                    indentAction = IndentAction.Outdent;
                    break;
                default:
                    warn($"[{languageId}]: language configuration: expected `onEnterRules[{i}].action.indent` to be 'none', 'indent', 'indentOutdent' or 'outdent'.");
                    continue;
            }

            var action = new EnterAction(indentAction);
            JsonNode? appendText = actionObject["appendText"];
            if (IsTruthy(appendText))
            {
                if (AsString(appendText) is { } text)
                {
                    action = action with { AppendText = text };
                }
                else
                {
                    warn($"[{languageId}]: language configuration: expected `onEnterRules[{i}].action.appendText` to be undefined or a string.");
                }
            }

            JsonNode? removeText = actionObject["removeText"];
            if (IsTruthy(removeText))
            {
                if (removeText.GetValueKind() == JsonValueKind.Number)
                {
                    action = action with { RemoveText = (int)removeText.GetValue<double>() };
                }
                else
                {
                    warn($"[{languageId}]: language configuration: expected `onEnterRules[{i}].action.removeText` to be undefined or a number.");
                }
            }

            if (ParseRegex(languageId, $"onEnterRules[{i}].beforeText", onEnterRule["beforeText"], warn) is not { } beforeText)
            {
                continue;
            }

            var resultingOnEnterRule = new OnEnterRule(beforeText, action);
            if (onEnterRule["afterText"] is { } afterTextNode
                && ParseRegex(languageId, $"onEnterRules[{i}].afterText", afterTextNode, warn) is { } afterText)
            {
                resultingOnEnterRule = resultingOnEnterRule with { AfterText = afterText };
            }

            if (onEnterRule["previousLineText"] is { } previousLineTextNode
                && ParseRegex(languageId, $"onEnterRules[{i}].previousLineText", previousLineTextNode, warn) is { } previousLineText)
            {
                resultingOnEnterRule = resultingOnEnterRule with { PreviousLineText = previousLineText };
            }

            (result ??= []).Add(resultingOnEnterRule);
        }

        return result;
    }

    // A non-object `indentationRules` reads every pattern as undefined, as in JavaScript.
    private static IndentationRule? MapIndentationRules(string languageId, JsonObject? indentationRules, Action<string> warn)
    {
        if (ParseRegex(languageId, "indentationRules.increaseIndentPattern", indentationRules?["increaseIndentPattern"], warn) is not { } increaseIndentPattern)
        {
            return null;
        }

        if (ParseRegex(languageId, "indentationRules.decreaseIndentPattern", indentationRules?["decreaseIndentPattern"], warn) is not { } decreaseIndentPattern)
        {
            return null;
        }

        var result = new IndentationRule(decreaseIndentPattern, increaseIndentPattern);
        JsonNode? indentNextLinePattern = indentationRules?["indentNextLinePattern"];
        if (IsTruthy(indentNextLinePattern))
        {
            result = result with { IndentNextLinePattern = ParseRegex(languageId, "indentationRules.indentNextLinePattern", indentNextLinePattern, warn) };
        }

        JsonNode? unIndentedLinePattern = indentationRules?["unIndentedLinePattern"];
        if (IsTruthy(unIndentedLinePattern))
        {
            result = result with { UnIndentedLinePattern = ParseRegex(languageId, "indentationRules.unIndentedLinePattern", unIndentedLinePattern, warn) };
        }

        return result;
    }

    /// <summary>
    /// Monaco's <c>_parseRegex</c>: a string is a pattern without flags; an object has a string <c>pattern</c> and
    /// optional string <c>flags</c>.
    /// </summary>
    /// <remarks>
    /// Behavior differences: the pattern goes through <see cref="TranslateJavaScriptPattern"/>; .NET <c>\w</c>,
    /// <c>\d</c> and <c>\b</c> are Unicode-aware where JavaScript's are ASCII; the flags <c>g</c>, <c>u</c>, <c>y</c>
    /// and <c>d</c> are ignored (so astral letters do not match <c>\p{L}</c>).
    /// </remarks>
    private static Regex? ParseRegex(string languageId, string confPath, JsonNode? value, Action<string> warn)
    {
        if (AsString(value) is { } pattern)
        {
            return CreateRegex(languageId, confPath, pattern, RegexOptions.None, warn);
        }

        if (value is JsonObject valueObject)
        {
            if (AsString(valueObject["pattern"]) is not { } objectPattern)
            {
                warn($"[{languageId}]: language configuration: expected `{confPath}.pattern` to be a string.");
                return null;
            }

            string flags = string.Empty;
            if (valueObject.TryGetPropertyValue("flags", out JsonNode? flagsNode))
            {
                if (AsString(flagsNode) is not { } flagsText)
                {
                    warn($"[{languageId}]: language configuration: expected `{confPath}.flags` to be a string.");
                    return null;
                }

                flags = flagsText;
            }

            var options = RegexOptions.None;
            foreach (char flag in flags)
            {
                switch (flag)
                {
                    case 'i':
                        options |= RegexOptions.IgnoreCase;
                        break;
                    case 'm':
                        options |= RegexOptions.Multiline;
                        break;
                    case 's':
                        options |= RegexOptions.Singleline;
                        break;
                    case 'g' or 'u' or 'y' or 'd':
                        break;
                    default:
                        // JavaScript throws a SyntaxError for an unknown flag.
                        warn($"[{languageId}]: Invalid regular expression in `{confPath}`: invalid flags '{flags}'");
                        return null;
                }
            }

            return CreateRegex(languageId, confPath, objectPattern, options, warn);
        }

        warn($"[{languageId}]: language configuration: expected `{confPath}` to be a string or an object.");
        return null;
    }

    private static Regex? CreateRegex(string languageId, string confPath, string pattern, RegexOptions options, Action<string> warn)
    {
        try
        {
            return new Regex(TranslateJavaScriptPattern(pattern), options | RegexOptions.CultureInvariant);
        }
        catch (ArgumentException e)
        {
            warn($"[{languageId}]: Invalid regular expression in `{confPath}`: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Rewrites the JavaScript-only Unicode property names used by the bundled files into .NET ones:
    /// <c>\p{Number}</c> → <c>\p{N}</c>, <c>\p{Nonspacing_Mark}</c> → <c>\p{Mn}</c>, and <c>\p{Alphabetic}</c> →
    /// <c>[\p{L}\p{Nl}\p{Mn}\p{Mc}]</c> (without the brackets inside a character class). Everything else is kept.
    /// </summary>
    /// <remarks>
    /// Behavior difference: Unicode Alphabetic is L + Nl + Other_Alphabetic, which .NET does not have;
    /// Other_Alphabetic is approximated by the Mn and Mc marks.
    /// </remarks>
    internal static string TranslateJavaScriptPattern(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        var result = new StringBuilder(pattern.Length);
        bool inClass = false;
        int i = 0;
        while (i < pattern.Length)
        {
            char c = pattern[i];
            if (c == '\\' && i + 1 < pattern.Length)
            {
                if (pattern[i + 1] == 'p' && TranslateProperty(pattern, i, inClass) is { } translated)
                {
                    result.Append(translated.Text);
                    i += translated.Length;
                    continue;
                }

                // Any other escape (including \[ and \]) is copied with the escaped character.
                result.Append(c).Append(pattern[i + 1]);
                i += 2;
                continue;
            }

            if (c == '[' && !inClass)
            {
                inClass = true;
            }
            else if (c == ']' && inClass)
            {
                inClass = false;
            }

            result.Append(c);
            i++;
        }

        return result.ToString();
    }

    private static (string Text, int Length)? TranslateProperty(string pattern, int start, bool inClass)
    {
        foreach ((string name, string outside, string inside) in PropertyNames)
        {
            string escape = $"\\p{{{name}}}";
            if (string.CompareOrdinal(pattern, start, escape, 0, escape.Length) == 0)
            {
                return (inClass ? inside : outside, escape.Length);
            }
        }

        return null;
    }

    private static readonly (string Name, string Outside, string Inside)[] PropertyNames =
    [
        ("Number", @"\p{N}", @"\p{N}"),
        ("Nonspacing_Mark", @"\p{Mn}", @"\p{Mn}"),
        ("Alphabetic", @"[\p{L}\p{Nl}\p{Mn}\p{Mc}]", @"\p{L}\p{Nl}\p{Mn}\p{Mc}"),
    ];
}
