// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/accessibleDiffViewer.ts (View._render, _createRow)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using System.Text;
using LoomEditor.View.Accessibility;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.WinUI.Controls.DiffEditor;

/// <summary>
/// What one row of the accessible diff viewer shows and announces: the line numbers, the sign, the plain text and the screen reader
/// label (Monaco: the DOM of <c>_createRow</c> and the header, without the DOM).
/// </summary>
/// <param name="Element">The row of the model.</param>
/// <param name="OriginalNumber">The line number of the original column; empty when the row has none.</param>
/// <param name="ModifiedNumber">The line number of the modified column; empty when the row has none.</param>
/// <param name="Sign">"+" for an added row, "−" for a deleted one, else empty.</param>
/// <param name="Text">The text of the line with its tabs expanded (the summary for a header).</param>
/// <param name="AutomationName">The label the screen reader reads.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record DiffViewerRowItem(ViewElement Element, string OriginalNumber, string ModifiedNumber, string Sign, string Text, string AutomationName)
{
    /// <summary>Builds the item of a row of the current group; the labels come from <see cref="AccessibilityMessages"/>.</summary>
    public static DiffViewerRowItem Create(ViewElement element, ViewElementGroup group, int groupIndex, int groupCount, IAccessibleDiffViewerHost host, int tabSize)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(group);
        ArgumentNullException.ThrowIfNull(host);

        if (element.Type == LineType.Header)
        {
            var (o, m) = (group.Range.Original, group.Range.Modified);
            return new DiffViewerRowItem(
                element, string.Empty, string.Empty, string.Empty,
                AccessibilityMessages.DiffHeaderText(groupIndex + 1, groupCount, o.StartLineNumber, o.Length, m.StartLineNumber, m.Length),
                AccessibilityMessages.DiffHeaderLabel(groupIndex + 1, groupCount, o.StartLineNumber, o.Length, m.StartLineNumber, m.Length));
        }

        int? original = element.GetOriginalLineNumber();
        int? modified = element.GetModifiedLineNumber();

        // Monaco: the modified text for added and unchanged rows, the original text for deleted rows.
        string content = modified is { } modifiedLine ? host.GetModifiedLine(modifiedLine) : host.GetOriginalLine(original!.Value);
        return new DiffViewerRowItem(
            element,
            Number(original),
            Number(modified),
            element.Type switch { LineType.Added => "+", LineType.Deleted => "−", _ => string.Empty },
            ExpandTabs(content, tabSize),
            AccessibilityMessages.DiffRowLabel(element.Type, content, original, modified));
    }

    /// <summary>Replaces each tab by the spaces up to the next tab stop.</summary>
    internal static string ExpandTabs(string text, int tabSize)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!text.Contains('\t', StringComparison.Ordinal))
        {
            return text;
        }

        int size = Math.Max(1, tabSize);
        var builder = new StringBuilder(text.Length + 8);
        foreach (char c in text)
        {
            if (c == '\t')
            {
                builder.Append(' ', size - (builder.Length % size));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static string Number(int? lineNumber) => lineNumber is { } n ? n.ToString(CultureInfo.InvariantCulture) : string.Empty;
}
