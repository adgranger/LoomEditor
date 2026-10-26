using System.Globalization;
using System.Text.Json;
using LoomEditor.Core.Markers;
using LoomEditor.Core.Model;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Editor;

internal static partial class LspConvert
{
    /// <summary>A missing or unknown severity is an error (the safe choice, as VS Code does).</summary>
    /// <remarks>Threading: pure.</remarks>
    public static MarkerSeverity ToSeverity(int? severity) => severity switch
    {
        LspDiagnosticSeverity.Warning => MarkerSeverity.Warning,
        LspDiagnosticSeverity.Information => MarkerSeverity.Info,
        LspDiagnosticSeverity.Hint => MarkerSeverity.Hint,
        _ => MarkerSeverity.Error,
    };

    /// <summary>Converts a diagnostic, validating its range against <paramref name="model"/>.</summary>
    /// <remarks>Threading: UI thread (reads the model).</remarks>
    public static MarkerData ToMarker(LspDiagnostic diagnostic, TextModel model, int? documentVersion)
    {
        Range range = ToRange(diagnostic.Range, model);
        return new MarkerData(
            ToSeverity(diagnostic.Severity),
            diagnostic.Message,
            range.StartLineNumber,
            range.StartColumn,
            range.EndLineNumber,
            range.EndColumn,
            Code: CodeToString(diagnostic.Code),
            Source: diagnostic.Source,
            ModelVersionId: documentVersion,
            Tags: ToMarkerTags(diagnostic.Tags));
    }

    /// <summary>A string stays; an integer keeps its written text; any other number uses the invariant text; the rest is null.</summary>
    /// <remarks>Threading: pure.</remarks>
    public static string? CodeToString(JsonElement? code)
    {
        if (code is not { } c)
        {
            return null;
        }

        return c.ValueKind switch
        {
            JsonValueKind.String => c.GetString(),
            JsonValueKind.Number => c.TryGetInt64(out _) ? c.GetRawText() : c.GetDouble().ToString(CultureInfo.InvariantCulture),
            _ => null,
        };
    }

    /// <summary>Maps the known tags; null when none is valid.</summary>
    /// <remarks>Threading: pure.</remarks>
    public static IReadOnlyList<MarkerTag>? ToMarkerTags(int[]? tags)
    {
        if (tags is null)
        {
            return null;
        }

        List<MarkerTag> result = [];
        foreach (int tag in tags)
        {
            if (tag == LspDiagnosticTag.Unnecessary)
            {
                result.Add(MarkerTag.Unnecessary);
            }
            else if (tag == LspDiagnosticTag.Deprecated)
            {
                result.Add(MarkerTag.Deprecated);
            }
        }

        return result.Count == 0 ? null : result;
    }
}
