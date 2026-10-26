using System.Text.Json;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.FakeServer;

public sealed partial class FakeLanguageServer
{
    private async Task PublishDiagnosticsAsync()
    {
        PublishDiagnosticsParams published;
        lock (_gate)
        {
            published = new PublishDiagnosticsParams { Uri = _uri ?? "", Version = _version, Diagnostics = ComputeDiagnostics(_text) };
        }

        await SendNotificationAsync(LspMethods.PublishDiagnostics, JsonSerializer.SerializeToElement(published, LspJsonContext.Default.PublishDiagnosticsParams))
            .ConfigureAwait(false);
    }

    private static LspDiagnostic[] ComputeDiagnostics(string text)
    {
        int[] starts = LineStarts(text);
        List<LspDiagnostic> diagnostics = [];
        foreach ((int start, int end) in Words(text))
        {
            string word = text[start..end];
            (int Severity, string? Code, int Tag)? kind = word switch
            {
                "ERROR" => (LspDiagnosticSeverity.Error, "\"FAKE001\"", 0),
                "WARN" => (LspDiagnosticSeverity.Warning, "2", 0),
                "INFO" => (LspDiagnosticSeverity.Information, null, 0),
                "HINT" => (LspDiagnosticSeverity.Hint, null, 0),
                "UNUSED" => (LspDiagnosticSeverity.Hint, null, LspDiagnosticTag.Unnecessary),
                "OLD" => (LspDiagnosticSeverity.Hint, null, LspDiagnosticTag.Deprecated),
                _ => null,
            };
            if (kind is not { } k)
            {
                continue;
            }

            diagnostics.Add(new LspDiagnostic
            {
                Range = RangeOf(starts, start, end),
                Severity = k.Severity,
                Code = k.Code is null ? null : JsonElement.Parse(k.Code),
                Source = "fake",
                Message = $"Found {word}",
                Tags = k.Tag == 0 ? null : [k.Tag],
            });
        }

        return [.. diagnostics];
    }
}
