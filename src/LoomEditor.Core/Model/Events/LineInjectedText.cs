// Ported from microsoft/vscode: src/vs/editor/common/textModelEvents.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using LoomEditor.Core.Decorations;

namespace LoomEditor.Core.Model.Events;

/// <summary>Text injected on a line (Monaco: <c>LineInjectedText</c>).</summary>
/// <param name="ownerId">The owner of the decoration.</param>
/// <param name="lineNumber">The line (1-based).</param>
/// <param name="column">The column the text is injected at.</param>
/// <param name="options">The injected text options.</param>
/// <param name="order">0 for <c>before</c> text, 1 for <c>after</c> text.</param>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed class LineInjectedText(int ownerId, int lineNumber, int column, ModelDecorationInjectedTextOptions options, int order)
{
    public int OwnerId { get; } = ownerId;

    public int LineNumber { get; } = lineNumber;

    public int Column { get; } = column;

    public ModelDecorationInjectedTextOptions Options { get; } = options;

    public int Order { get; } = order;

    /// <summary>Inserts the injected texts (sorted by column) into the line text.</summary>
    public static string ApplyInjectedText(string lineText, IReadOnlyList<LineInjectedText>? injectedTexts)
    {
        if (injectedTexts is null || injectedTexts.Count == 0)
        {
            return lineText;
        }

        var result = new StringBuilder();
        var lastOriginalOffset = 0;
        foreach (var injectedText in injectedTexts)
        {
            var end = injectedText.Column - 1;
            if (end > lastOriginalOffset)
            {
                result.Append(lineText, lastOriginalOffset, end - lastOriginalOffset);
            }

            lastOriginalOffset = end;
            result.Append(injectedText.Options.Content);
        }

        result.Append(lineText, lastOriginalOffset, lineText.Length - lastOriginalOffset);
        return result.ToString();
    }

    /// <summary>The injected texts of the decorations, sorted by (line, column, order).</summary>
    public static List<LineInjectedText> FromDecorations(IEnumerable<ModelDecoration> decorations)
    {
        var result = new List<LineInjectedText>();
        foreach (var decoration in decorations)
        {
            if (decoration.Options.Before is { } before && OccupiesHorizontalSpace(before))
            {
                result.Add(new LineInjectedText(decoration.OwnerId, decoration.Range.StartLineNumber, decoration.Range.StartColumn, before, 0));
            }

            if (decoration.Options.After is { } after && OccupiesHorizontalSpace(after))
            {
                result.Add(new LineInjectedText(decoration.OwnerId, decoration.Range.EndLineNumber, decoration.Range.EndColumn, after, 1));
            }
        }

        result.Sort(static (a, b) =>
        {
            if (a.LineNumber != b.LineNumber)
            {
                return a.LineNumber - b.LineNumber;
            }

            return a.Column != b.Column ? a.Column - b.Column : a.Order - b.Order;
        });
        return result;
    }

    /// <summary>The ranges of the injected line rendered at a fixed width; width-only text gives an empty range.</summary>
    public static List<FixedWidthInjectedTextRange> GetFixedWidthInjectedTextRanges(IReadOnlyList<LineInjectedText>? injectedTexts)
    {
        var result = new List<FixedWidthInjectedTextRange>();
        var injectedTextLength = 0;
        foreach (var injectedText in injectedTexts ?? [])
        {
            var length = injectedText.Options.Content.Length;
            var startOffset = injectedText.Column - 1 + injectedTextLength;
            if (injectedText.Options.WidthInEm is { } widthInEm)
            {
                result.Add(new FixedWidthInjectedTextRange(startOffset, startOffset + length, widthInEm));
            }

            injectedTextLength += length;
        }

        return result;
    }

    /// <summary>A copy with another content.</summary>
    public LineInjectedText WithText(string text) =>
        new(OwnerId, LineNumber, Column, Options.WithContent(text), Order);

    private static bool OccupiesHorizontalSpace(ModelDecorationInjectedTextOptions options) =>
        options.Content.Length > 0 || options.WidthInEm is > 0;
}
