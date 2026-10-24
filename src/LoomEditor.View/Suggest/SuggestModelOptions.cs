// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts (InternalSuggestOptions, subset)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Suggest;

/// <summary>
/// The options of the completion logic (Monaco: <c>InternalSuggestOptions</c>, subset). Not ported: <c>insertMode</c> (always insert),
/// <c>localityBonus</c>, <c>shareSuggestSelections</c>, the <c>show*</c> kind switches, <c>showIcons</c>, <c>snippetsPreventQuickSuggestions</c>, <c>selectionMode</c>.
/// </summary>
/// <param name="FilterGraceful">Whether a typo in the typed word is tolerated (<c>editor.suggest.filterGraceful</c>).</param>
/// <param name="MatchOnWordStartOnly">Whether the typed word must match at a word start (<c>editor.suggest.matchOnWordStartOnly</c>).</param>
/// <param name="Snippets">Where snippets go in the list.</param>
/// <remarks>Threading: immutable record, safe from any thread.</remarks>
internal sealed record SuggestModelOptions(
    bool FilterGraceful = true,
    bool MatchOnWordStartOnly = true,
    SnippetSuggestions Snippets = SnippetSuggestions.Inline)
{
    /// <summary>Whether typing opens the list, per token kind (<c>editor.quickSuggestions</c>).</summary>
    public QuickSuggestionsOptions QuickSuggestions { get; init; } = QuickSuggestionsOptions.Default;

    /// <summary>How long typing must pause before the list opens (<c>editor.quickSuggestionsDelay</c>).</summary>
    public TimeSpan QuickSuggestionsDelay { get; init; } = TimeSpan.FromMilliseconds(10);

    /// <summary>Whether a trigger character of a provider opens the list (<c>editor.suggestOnTriggerCharacters</c>).</summary>
    public bool SuggestOnTriggerCharacters { get; init; } = true;

    /// <summary>The VS Code defaults.</summary>
    public static readonly SuggestModelOptions Default = new();
}
