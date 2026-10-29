using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using LoomEditor.Lsp.JsonRpc;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.FakeServer;

public sealed partial class FakeLanguageServer
{
    private static readonly string[] Keywords = ["if", "else", "for", "class"];

    private Func<JsonElement?> FindFeature(string method, JsonElement? @params) => method switch
    {
        LspMethods.Completion => () => Completion(Required(@params, LspJsonContext.Default.CompletionParams)),
        LspMethods.CompletionResolve => Resolve(Required(@params, LspJsonContext.Default.LspCompletionItem)),
        LspMethods.Hover => () => Hover(Required(@params, LspJsonContext.Default.TextDocumentPositionParams).Position),
        LspMethods.Definition => () => Definition(Required(@params, LspJsonContext.Default.TextDocumentPositionParams).Position),
        _ => throw new JsonRpcException(JsonRpcErrorCodes.MethodNotFound, $"Method not found: {method}"),
    };

    private static T Required<T>(JsonElement? @params, JsonTypeInfo<T> info)
        where T : class
        => Parse(@params, info) ?? throw new JsonRpcException(JsonRpcErrorCodes.InvalidParams, "Invalid params");

    private static JsonElement? Serialize<T>(T value, JsonTypeInfo<T> info) => JsonSerializer.SerializeToElement(value, info);

    private (string Text, int Offset, int[] Starts) Snapshot(LspPosition position)
    {
        string text = DocumentText;
        return (text, OffsetOf(text, position.Line, position.Character), LineStarts(text));
    }

    private JsonElement? Completion(CompletionParams request)
    {
        (string text, int offset, int[] starts) = Snapshot(request.Position);
        List<LspCompletionItem> items = [];

        if (request.Context?.TriggerCharacter == ".")
        {
            int end = offset;
            while (end < text.Length && IsIdentifierPart(text[end]))
            {
                end++;
            }

            LspRange insert = RangeOf(starts, offset, offset);
            LspRange replace = RangeOf(starts, offset, end);
            foreach ((string label, int kind, bool commit) in new[] { ("Length", 10, false), ("Count", 10, false), ("ToString", 2, true), ("Equals", 2, true) })
            {
                LspCompletionItem item = NewItem(label, kind);
                item.TextEdit = Serialize(new LspInsertReplaceEdit { NewText = label, Insert = insert, Replace = replace }, LspJsonContext.Default.LspInsertReplaceEdit);
                item.CommitCharacters = commit ? ["("] : null;
                items.Add(item);
            }
        }

        SortedSet<string> words = new(StringComparer.Ordinal);
        foreach ((int start, int end) in Words(text))
        {
            if (end - start >= 2 && end != offset)
            {
                words.Add(text[start..end]);
            }
        }

        foreach (string word in words)
        {
            LspCompletionItem item = NewItem(word, LspCompletionItemKind.Text);
            item.InsertText = word;
            items.Add(item);
        }

        foreach (string keyword in Keywords)
        {
            LspCompletionItem item = NewItem(keyword, LspCompletionItemKind.Keyword);
            item.InsertText = keyword;
            item.Preselect = keyword == "if" ? true : null;
            items.Add(item);
        }

        if (options.Snippet)
        {
            LspCompletionItem item = NewItem("forloop", LspCompletionItemKind.Snippet);
            item.InsertText = "for (${1:i} = 0; $1 < ${2:n}; $1++) {\n\t$0\n}";
            item.InsertTextFormat = LspInsertTextFormat.Snippet;
            items.Add(item);
        }

        return Serialize(new LspCompletionList { IsIncomplete = options.Incomplete, Items = [.. items] }, LspJsonContext.Default.LspCompletionList);
    }

    private static LspCompletionItem NewItem(string label, int kind) => new()
    {
        Label = label,
        Kind = kind,
        SortText = label,
        Data = JsonElement.Parse($"{{\"label\":\"{label}\",\"n\":1}}"),
    };

    private static Func<JsonElement?> Resolve(LspCompletionItem item) => () =>
    {
        item.Detail = $"detail of {item.Label}";
        item.Documentation = Serialize(
            new LspMarkupContent { Kind = LspMarkupKind.Markdown, Value = $"**{item.Label}** documentation" }, LspJsonContext.Default.LspMarkupContent);
        if (item.Label == "Length")
        {
            LspPosition origin = new() { Line = 0, Character = 0 };
            item.AdditionalTextEdits = [new LspTextEdit { Range = new LspRange { Start = origin, End = origin }, NewText = "// resolved\n" }];
        }

        return Serialize(item, LspJsonContext.Default.LspCompletionItem);
    };

    private JsonElement? Hover(LspPosition position)
    {
        (string text, int offset, int[] starts) = Snapshot(position);
        if (WordAt(text, offset) is not { } word)
        {
            return null;
        }

        string name = text[word.Start..word.End];
        LspHover hover = new()
        {
            Contents = Serialize(
                new LspMarkupContent { Kind = LspMarkupKind.Markdown, Value = $"```fake\n{name}\n```\nHover for **{name}**" }, LspJsonContext.Default.LspMarkupContent)!.Value,
            Range = RangeOf(starts, word.Start, word.End),
        };
        return Serialize(hover, LspJsonContext.Default.LspHover);
    }

    private static (int Start, int End)? WordAt(string text, int offset)
    {
        foreach ((int start, int end) in Words(text))
        {
            if (start <= offset && offset <= end)
            {
                return (start, end);
            }
        }

        return null;
    }

    private JsonElement? Definition(LspPosition position)
    {
        (string text, int offset, int[] starts) = Snapshot(position);
        if (WordAt(text, offset) is not { } at)
        {
            return null;
        }

        string word = text[at.Start..at.End];
        if (word == "external")
        {
            LspRange origin = new() { Start = new LspPosition { Line = 0, Character = 0 }, End = new LspPosition { Line = 0, Character = 8 } };
            return Serialize(new LspLocation { Uri = "file:///other/file.txt", Range = origin }, LspJsonContext.Default.LspLocation);
        }

        string uri = DocumentUri ?? "";
        List<(int Start, int End)> words = Words(text);
        for (int i = 0; i + 1 < words.Count; i++)
        {
            (int keywordStart, int keywordEnd) = words[i];
            (int nameStart, int nameEnd) = words[i + 1];
            string keyword = text[keywordStart..keywordEnd];
            if ((keyword is not ("def" or "class"))
                || text[nameStart..nameEnd] != word
                || PositionOf(starts, keywordStart).Line != PositionOf(starts, nameStart).Line
                || !string.IsNullOrWhiteSpace(text[keywordEnd..nameStart])
                || keywordEnd == nameStart)
            {
                continue;
            }

            LspRange declared = RangeOf(starts, nameStart, nameEnd);
            if (keyword == "def")
            {
                return Serialize(new LspLocation { Uri = uri, Range = declared }, LspJsonContext.Default.LspLocation);
            }

            int lineStart = starts[PositionOf(starts, nameStart).Line];
            LspLocationLink link = new()
            {
                OriginSelectionRange = RangeOf(starts, at.Start, at.End),
                TargetUri = uri,
                TargetRange = RangeOf(starts, lineStart, LineEnd(text, lineStart)),
                TargetSelectionRange = declared,
            };
            return Serialize<LspLocationLink[]>([link], LspJsonContext.Default.LspLocationLinkArray);
        }

        return null;
    }
}
