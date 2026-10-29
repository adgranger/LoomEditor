// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (CompletionItem)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Languages.Features;

/// <summary>
/// One suggestion of a completion list. Mutable on purpose: the result of
/// <see cref="ICompletionItemProvider.ResolveCompletionItemAsync"/> is merged into the item (<see cref="MergeFrom"/>).
/// </summary>
/// <remarks>
/// Threading: not thread-safe; owned by the UI thread once handed to the editor.
/// Not ported: <c>command</c>, <c>action</c>, <c>extensionId</c>, <c>_id</c>.
/// </remarks>
public sealed class CompletionItem
{
    /// <summary>Creates an item. An empty <paramref name="label"/> is allowed (the item is then invalid for the editor).</summary>
    /// <exception cref="ArgumentNullException"><paramref name="label"/> or <paramref name="insertText"/> is null.</exception>
    public CompletionItem(string label, string insertText, CompletionItemKind kind = CompletionItemKind.Text)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(insertText);
        Label = label;
        InsertText = insertText;
        Kind = kind;
    }

    /// <summary>The text shown in the list.</summary>
    public string Label { get; set; }

    /// <summary>Monaco's <c>label.detail</c>: shown right after the label.</summary>
    public string? LabelDetail { get; set; }

    /// <summary>Monaco's <c>label.description</c>: shown after <see cref="LabelDetail"/>.</summary>
    public string? LabelDescription { get; set; }

    /// <summary>The kind, used for the icon.</summary>
    public CompletionItemKind Kind { get; set; }

    /// <summary>Extra information (deprecated...).</summary>
    public IReadOnlyList<CompletionItemTag>? Tags { get; set; }

    /// <summary>A human-readable string with additional information (type or symbol information).</summary>
    public string? Detail { get; set; }

    /// <summary>The documentation.</summary>
    public MarkdownString? Documentation { get; set; }

    /// <summary>The text used to sort items; defaults to <see cref="Label"/>.</summary>
    public string? SortText { get; set; }

    /// <summary>The text used to filter items; defaults to <see cref="Label"/>.</summary>
    public string? FilterText { get; set; }

    /// <summary>Select this item when showing.</summary>
    public bool Preselect { get; set; }

    /// <summary>The text inserted when the item is accepted: plain text, or a snippet template when <see cref="InsertTextRules"/> has <see cref="Features.InsertTextRules.InsertAsSnippet"/>.</summary>
    public string InsertText { get; set; }

    /// <summary>How <see cref="InsertText"/> is interpreted (Monaco: <c>insertTextRules</c>).</summary>
    public InsertTextRules InsertTextRules { get; set; }

    /// <summary>The range to edit; null means the provider wants the default range.</summary>
    public CompletionRange? Range { get; set; }

    /// <summary>Characters that accept the item when typed.</summary>
    public IReadOnlyList<char>? CommitCharacters { get; set; }

    /// <summary>Edits applied together with the insertion, outside the item range.</summary>
    public IReadOnlyList<TextEdit>? AdditionalTextEdits { get; set; }

    /// <summary>Opaque data owned by the provider (an LSP provider keeps the raw item here).</summary>
    public object? ProviderData { get; set; }

    /// <summary>
    /// Copies the non-null members a provider can resolve (<see cref="Detail"/>, <see cref="Documentation"/>,
    /// <see cref="AdditionalTextEdits"/>, <see cref="LabelDetail"/>, <see cref="LabelDescription"/>, <see cref="Tags"/>)
    /// of <paramref name="resolved"/> into this item. Other members are never touched.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="resolved"/> is null.</exception>
    public void MergeFrom(CompletionItem resolved)
    {
        ArgumentNullException.ThrowIfNull(resolved);
        Detail = resolved.Detail ?? Detail;
        Documentation = resolved.Documentation ?? Documentation;
        AdditionalTextEdits = resolved.AdditionalTextEdits ?? AdditionalTextEdits;
        LabelDetail = resolved.LabelDetail ?? LabelDetail;
        LabelDescription = resolved.LabelDescription ?? LabelDescription;
        Tags = resolved.Tags ?? Tags;
    }
}
