using LoomEditor.Core.Markers;
using LoomEditor.Core.Model;

namespace LoomEditor.Gallery.Lsp;

/// <summary>Squiggles of the four severities, without any server.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static class DemoMarkers
{
    private const string Owner = "gallery-demo";

    private static readonly (MarkerSeverity Severity, string Message)[] Kinds =
    [
        (MarkerSeverity.Error, "Demo error"),
        (MarkerSeverity.Warning, "Demo warning"),
        (MarkerSeverity.Info, "Demo info"),
        (MarkerSeverity.Hint, "Demo hint"),
    ];

    /// <summary>Puts one marker of each severity on the first word of the first four non-empty lines.</summary>
    internal static void Set(MarkerService markers, TextModel model)
    {
        List<MarkerData> data = [];
        for (int line = 1; line <= model.LineCount && data.Count < Kinds.Length; line++)
        {
            string content = model.GetLineContent(line);
            int start = 0;
            while (start < content.Length && char.IsWhiteSpace(content[start]))
            {
                start++;
            }

            if (start == content.Length)
            {
                continue;
            }

            int end = start;
            while (end < content.Length && !char.IsWhiteSpace(content[end]))
            {
                end++;
            }

            (MarkerSeverity severity, string message) = Kinds[data.Count];
            data.Add(new MarkerData(severity, message, line, start + 1, line, end + 1));
        }

        markers.Change(Owner, data);
    }

    /// <summary>Removes the demo markers.</summary>
    internal static void Clear(MarkerService markers) => markers.Remove(Owner);
}
