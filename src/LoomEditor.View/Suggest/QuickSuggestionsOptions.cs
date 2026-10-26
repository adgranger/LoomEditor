// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts (IQuickSuggestionsOptions, QuickSuggestionsOptions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Tokens;

namespace LoomEditor.View.Suggest;

/// <summary>Whether typing opens the list for a kind of token (Monaco: <c>'on' | 'inline' | 'off'</c>).</summary>
internal enum QuickSuggestionValue
{
    /// <summary>The list opens.</summary>
    On,

    /// <summary>Behaves as <see cref="Off"/>: there is no ghost text (Monaco <c>offWhenInlineCompletions</c> is not ported).</summary>
    Inline,

    /// <summary>The list does not open.</summary>
    Off,
}

/// <summary>Quick suggestions per token kind (Monaco: <c>editor.quickSuggestions</c>).</summary>
/// <param name="Other">Code outside comments and strings.</param>
/// <param name="Comments">Inside comments.</param>
/// <param name="Strings">Inside strings.</param>
/// <remarks>Threading: immutable record, safe from any thread.</remarks>
internal sealed record QuickSuggestionsOptions(
    QuickSuggestionValue Other = QuickSuggestionValue.On,
    QuickSuggestionValue Comments = QuickSuggestionValue.Off,
    QuickSuggestionValue Strings = QuickSuggestionValue.Off)
{
    /// <summary>The VS Code defaults.</summary>
    public static readonly QuickSuggestionsOptions Default = new();

    /// <summary>Whether every kind of token is off (<c>inline</c> counts as off).</summary>
    public bool IsAllOff => IsOff(Other) && IsOff(Comments) && IsOff(Strings);

    /// <summary>Whether every kind of token is on.</summary>
    public bool IsAllOn => Other == QuickSuggestionValue.On && Comments == QuickSuggestionValue.On && Strings == QuickSuggestionValue.On;

    /// <summary>The value for a token type (Monaco: <c>valueFor</c>; a regular expression counts as other).</summary>
    public QuickSuggestionValue ValueFor(StandardTokenType type) => type switch
    {
        StandardTokenType.Comment => Comments,
        StandardTokenType.String => Strings,
        _ => Other,
    };

    private static bool IsOff(QuickSuggestionValue value) => value is QuickSuggestionValue.Off or QuickSuggestionValue.Inline;
}
