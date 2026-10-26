using System.Text.Json;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.FakeServer;

public sealed partial class FakeLanguageServer
{
    private string _text = "";
    private int _version;
    private string? _uri;

    private async Task DidOpenAsync(JsonElement? @params)
    {
        if (Parse(@params, LspJsonContext.Default.DidOpenTextDocumentParams) is not { } open)
        {
            return;
        }

        lock (_gate)
        {
            _uri = open.TextDocument.Uri;
            _version = open.TextDocument.Version;
            _text = open.TextDocument.Text;
        }

        await PublishDiagnosticsAsync().ConfigureAwait(false);
    }

    private async Task DidChangeAsync(JsonElement? @params)
    {
        if (Parse(@params, LspJsonContext.Default.DidChangeTextDocumentParams) is not { } change)
        {
            return;
        }

        lock (_gate)
        {
            if (change.TextDocument.Uri != _uri || change.TextDocument.Version <= _version)
            {
                _received.Add("didChange(ignored)");
                return;
            }

            _version = change.TextDocument.Version;
            foreach (TextDocumentContentChangeEvent item in change.ContentChanges)
            {
                _text = ApplyChange(_text, item);
            }
        }

        await PublishDiagnosticsAsync().ConfigureAwait(false);
    }

    private async Task DidCloseAsync(JsonElement? @params)
    {
        if (Parse(@params, LspJsonContext.Default.DidCloseTextDocumentParams) is not { } close)
        {
            return;
        }

        lock (_gate)
        {
            _uri = null;
            _text = "";
            _version = 0;
        }

        PublishDiagnosticsParams empty = new() { Uri = close.TextDocument.Uri, Diagnostics = [] };
        await SendNotificationAsync(LspMethods.PublishDiagnostics, JsonSerializer.SerializeToElement(empty, LspJsonContext.Default.PublishDiagnosticsParams))
            .ConfigureAwait(false);
    }

    private static T? Parse<T>(JsonElement? @params, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> info)
        where T : class
    {
        try
        {
            return @params?.Deserialize(info);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ApplyChange(string text, TextDocumentContentChangeEvent change)
    {
        if (change.Range is not { } range)
        {
            return change.Text;
        }

        int start = OffsetOf(text, range.Start.Line, range.Start.Character);
        int end = OffsetOf(text, range.End.Line, range.End.Character);
        if (end < start)
        {
            (start, end) = (end, start);
        }

        return string.Concat(text.AsSpan(0, start), change.Text.AsSpan(), text.AsSpan(end));
    }

    /// <summary>The start offset of every line; lines end with <c>\n</c>, <c>\r\n</c> or <c>\r</c>.</summary>
    private static int[] LineStarts(string text)
    {
        List<int> starts = [0];
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
            {
                i++;
                starts.Add(i + 1);
            }
            else if (text[i] is '\r' or '\n')
            {
                starts.Add(i + 1);
            }
        }

        return [.. starts];
    }

    private static int LineEnd(string text, int start)
    {
        int end = start;
        while (end < text.Length && text[end] is not ('\r' or '\n'))
        {
            end++;
        }

        return end;
    }

    /// <summary>A line beyond the document gives the end of the document, a character beyond the line gives the line end.</summary>
    private static int OffsetOf(string text, int line, int character)
    {
        int[] starts = LineStarts(text);
        if (line >= starts.Length)
        {
            return text.Length;
        }

        int start = starts[line];
        return start + Math.Min(Math.Max(character, 0), LineEnd(text, start) - start);
    }

    private static LspPosition PositionOf(int[] starts, int offset)
    {
        int line = Array.BinarySearch(starts, offset);
        if (line < 0)
        {
            line = ~line - 1;
        }

        return new LspPosition { Line = line, Character = offset - starts[line] };
    }

    private static LspRange RangeOf(int[] starts, int start, int end)
        => new() { Start = PositionOf(starts, start), End = PositionOf(starts, end) };

    /// <summary>The identifiers of a text (<c>[A-Za-z_][A-Za-z0-9_]*</c>) as start and end offsets.</summary>
    private static List<(int Start, int End)> Words(string text)
    {
        List<(int, int)> words = [];
        int i = 0;
        while (i < text.Length)
        {
            if (!IsIdentifierStart(text[i]))
            {
                i++;
                continue;
            }

            int end = i + 1;
            while (end < text.Length && IsIdentifierPart(text[end]))
            {
                end++;
            }

            words.Add((i, end));
            i = end;
        }

        return words;
    }

    private static bool IsIdentifierStart(char c) => char.IsAsciiLetter(c) || c == '_';

    private static bool IsIdentifierPart(char c) => char.IsAsciiLetterOrDigit(c) || c == '_';
}
