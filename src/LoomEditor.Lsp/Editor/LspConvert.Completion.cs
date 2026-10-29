using System.Text.Json;
using LoomEditor.Core.Languages.Features;
using LoomEditor.Core.Text;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Editor;

internal static partial class LspConvert
{
    // Indexed by the LSP CompletionItemKind (1..25); index 0 is unused.
    private static readonly CompletionItemKind[] Kinds =
    [
        CompletionItemKind.Text, CompletionItemKind.Text, CompletionItemKind.Method, CompletionItemKind.Function,
        CompletionItemKind.Constructor, CompletionItemKind.Field, CompletionItemKind.Variable, CompletionItemKind.Class,
        CompletionItemKind.Interface, CompletionItemKind.Module, CompletionItemKind.Property, CompletionItemKind.Unit,
        CompletionItemKind.Value, CompletionItemKind.Enum, CompletionItemKind.Keyword, CompletionItemKind.Snippet,
        CompletionItemKind.Color, CompletionItemKind.File, CompletionItemKind.Reference, CompletionItemKind.Folder,
        CompletionItemKind.EnumMember, CompletionItemKind.Constant, CompletionItemKind.Struct, CompletionItemKind.Event,
        CompletionItemKind.Operator, CompletionItemKind.TypeParameter,
    ];

    /// <summary>A missing or unknown kind is <see cref="CompletionItemKind.Text"/>.</summary>
    /// <remarks>Threading: pure.</remarks>
    public static CompletionItemKind ToCompletionItemKind(int? lspKind)
        => lspKind is >= 1 and < 26 ? Kinds[lspKind.Value] : CompletionItemKind.Text;

    /// <summary>Converts an item; the default range is filled in later by the suggest items.</summary>
    /// <remarks>Threading: pure, runs off the UI thread; ranges are not validated.</remarks>
    public static CompletionItem ToCompletionItem(LspCompletionItem item)
    {
        (LspRange? insert, LspRange? replace, string? newText) = LspUnions.ParseTextEdit(item.TextEdit);
        string text = newText ?? item.InsertText ?? item.Label;

        string? documentation = LspUnions.ParseDocumentation(item.Documentation, out bool isMarkdown);

        return new CompletionItem(item.Label, text, ToCompletionItemKind(item.Kind))
        {
            LabelDetail = item.LabelDetails?.Detail,
            LabelDescription = item.LabelDetails?.Description,
            Detail = item.Detail,
            SortText = item.SortText,
            FilterText = item.FilterText,
            InsertTextRules = item.InsertTextFormat == LspInsertTextFormat.Snippet ? InsertTextRules.InsertAsSnippet : InsertTextRules.None,
            Preselect = item.Preselect == true,
            ProviderData = item,
            Range = newText is null ? null : new CompletionRange(ToRangeUnvalidated(insert!), ToRangeUnvalidated(replace!)),
            Tags = item.Deprecated == true || item.Tags?.Contains(LspCompletionItemTag.Deprecated) == true ? [CompletionItemTag.Deprecated] : null,
            Documentation = string.IsNullOrEmpty(documentation) ? null : isMarkdown ? new MarkdownString(documentation) : MarkdownString.FromText(documentation),
            CommitCharacters = ToCommitCharacters(item.CommitCharacters),
            AdditionalTextEdits = item.AdditionalTextEdits is { Length: > 0 } edits
                ? [.. edits.Select(e => new TextEdit(ToRangeUnvalidated(e.Range), e.NewText))]
                : null,
        };
    }

    private static char[]? ToCommitCharacters(string[]? characters)
    {
        char[]? result = characters?.Where(c => !string.IsNullOrEmpty(c)).Select(c => c[0]).ToArray();
        return result is { Length: > 0 } ? result : null;
    }

    /// <summary>A new item converted from the resolved LSP item (<c>ProviderData</c> is the resolved item).</summary>
    /// <remarks>Threading: pure.</remarks>
    public static CompletionItem ApplyResolved(LspCompletionItem resolved) => ToCompletionItem(resolved);

    /// <summary>Converts a completion result (<c>null</c>, an item array or a <c>CompletionList</c>).</summary>
    /// <remarks>Threading: pure, runs off the UI thread.</remarks>
    public static CompletionList ToCompletionList(JsonElement? result)
    {
        IReadOnlyList<LspCompletionItem> items = LspUnions.ParseCompletionResult(result, out bool incomplete);
        return new CompletionList([.. items.Select(ToCompletionItem)], incomplete);
    }

    /// <summary>Maps the editor trigger kind to the LSP value.</summary>
    /// <remarks>Threading: pure.</remarks>
    public static int ToLspTriggerKind(CompletionTriggerKind kind) => kind switch
    {
        CompletionTriggerKind.TriggerCharacter => LspCompletionTriggerKind.TriggerCharacter,
        CompletionTriggerKind.TriggerForIncompleteCompletions => LspCompletionTriggerKind.TriggerForIncompleteCompletions,
        _ => LspCompletionTriggerKind.Invoked,
    };

    /// <summary>Builds the LSP completion context.</summary>
    /// <remarks>Threading: pure.</remarks>
    public static CompletionContextDto ToLspContext(CompletionContext context)
        => new() { TriggerKind = ToLspTriggerKind(context.TriggerKind), TriggerCharacter = context.TriggerCharacter };
}
