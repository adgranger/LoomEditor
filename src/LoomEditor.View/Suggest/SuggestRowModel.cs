// Inspired by microsoft/vscode: src/vs/editor/contrib/suggest/browser/suggestWidgetRenderer.ts (ItemRenderer.renderElement)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Filters;
using LoomEditor.Core.Languages.Features;
using LoomEditor.View.Markdown;

namespace LoomEditor.View.Suggest;

/// <summary>What one row of the completion list shows; pure so it can be tested without the UI.</summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
internal sealed record SuggestRowModel(
    string Label,
    IReadOnlyList<FilterMatch> Matches,
    string? LabelDetail,
    string? LabelDescription,
    string BadgeGlyph,
    string KindLabel,
    bool IsDeprecated,
    bool IsSelected)
{
    private const int MaxDetailLength = 400;

    /// <summary>Builds the row of <paramref name="item"/>; the letters to highlight come from its fuzzy score.</summary>
    public static SuggestRowModel Create(SuggestItem item, bool isSelected)
    {
        ArgumentNullException.ThrowIfNull(item);
        CompletionItem completion = item.Completion;
        return new SuggestRowModel(
            item.TextLabel,
            FuzzyScore.IsDefault(item.Score) ? [] : Fuzzy.CreateMatches(item.Score),
            completion.LabelDetail,
            completion.LabelDescription,
            SuggestKindBadges.Glyph(completion.Kind),
            SuggestKindBadges.ToLabel(completion.Kind),
            completion.Tags?.Contains(CompletionItemTag.Deprecated) == true,
            isSelected);
    }

    /// <summary>The detail strip: the detail, a blank line, then the documentation as plain text, cut to 400 characters; empty when there is neither.</summary>
    public static string DetailText(SuggestItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        CompletionItem completion = item.Completion;
        string documentation = completion.Documentation is { } doc ? MarkdownParser.Parse(doc.Value).ToPlainText() : "";
        string text = string.IsNullOrEmpty(completion.Detail)
            ? documentation
            : documentation.Length == 0 ? completion.Detail : completion.Detail + "\n\n" + documentation;
        return text.Length > MaxDetailLength ? text[..MaxDetailLength] + "…" : text;
    }
}
