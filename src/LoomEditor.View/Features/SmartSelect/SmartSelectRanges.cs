// Ported from microsoft/vscode: src/vs/editor/contrib/smartSelect/browser/smartSelect.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Features.SmartSelect;

/// <summary>Monaco: <c>provideSelectionRanges</c>.</summary>
/// <remarks>
/// Behavior differences: synchronous, and there is no provider registry, so the providers are always the bracket one then the
/// word one (Monaco: <c>providers.unshift(new BracketSelectionRangeProvider())</c> when no other provider exists).
/// Threading: UI thread only.
/// </remarks>
internal static class SmartSelectRanges
{
    /// <summary>For each position, its ranges from the innermost to the outermost, each strictly containing the previous one.</summary>
    public static List<List<Range>> Provide(TextModel model, IReadOnlyList<Position> positions, SmartSelectOptions options) =>
        Provide(model, positions, options, [new BracketSelectionRangeProvider(), new WordSelectionRangeProvider(options.SelectSubwords)]);

    /// <summary>Same as <see cref="Provide(TextModel, IReadOnlyList{Position}, SmartSelectOptions)"/> with the given providers.</summary>
    public static List<List<Range>> Provide(
        TextModel model,
        IReadOnlyList<Position> positions,
        SmartSelectOptions options,
        IReadOnlyList<ISelectionRangeProvider> providers)
    {
        var allRawRanges = new List<List<Range>>();
        for (int i = 0; i < positions.Count; i++)
        {
            allRawRanges.Add([]);
        }

        foreach (ISelectionRangeProvider provider in providers)
        {
            IReadOnlyList<IReadOnlyList<Range>> allProviderRanges = provider.ProvideSelectionRanges(model, positions);
            if (allProviderRanges.Count != positions.Count)
            {
                continue;
            }

            for (int i = 0; i < positions.Count; i++)
            {
                foreach (Range range in allProviderRanges[i])
                {
                    if (range.Contains(positions[i]))
                    {
                        allRawRanges[i].Add(range);
                    }
                }
            }
        }

        return [.. allRawRanges.Select(oneRawRanges => ToRanges(model, oneRawRanges, options))];
    }

    private static List<Range> ToRanges(TextModel model, List<Range> oneRawRanges, SmartSelectOptions options)
    {
        if (oneRawRanges.Count == 0)
        {
            return [];
        }

        // sort all by start/end position
        oneRawRanges.Sort((a, b) =>
        {
            if (a.Start.IsBefore(b.Start))
            {
                return 1;
            }

            if (b.Start.IsBefore(a.Start))
            {
                return -1;
            }

            if (a.End.IsBefore(b.End))
            {
                return -1;
            }

            return b.End.IsBefore(a.End) ? 1 : 0;
        });

        // remove ranges that don't contain the former range or that are equal to the former range
        var oneRanges = new List<Range>();
        Range? last = null;
        foreach (Range range in oneRawRanges)
        {
            if (last is not { } previous || (range.Contains(previous) && range != previous))
            {
                oneRanges.Add(range);
                last = range;
            }
        }

        if (!options.SelectLeadingAndTrailingWhitespace)
        {
            return oneRanges;
        }

        // add ranges that expand trivia at line starts and ends whenever a range wraps onto a new line
        var oneRangesWithTrivia = new List<Range> { oneRanges[0] };
        for (int i = 1; i < oneRanges.Count; i++)
        {
            Range prev = oneRanges[i - 1];
            Range cur = oneRanges[i];
            if (cur.StartLineNumber != prev.StartLineNumber || cur.EndLineNumber != prev.EndLineNumber)
            {
                // add line/block range without leading/failing whitespace
                // Monaco builds a range with column 0 for a blank line; it never contains anything, and Range rejects it here.
                int firstColumn = model.GetLineFirstNonWhitespaceColumn(prev.StartLineNumber);
                int lastColumn = model.GetLineLastNonWhitespaceColumn(prev.EndLineNumber);
                Range? noWhitespace = firstColumn > 0 && lastColumn > 0 ? new Range(prev.StartLineNumber, firstColumn, prev.EndLineNumber, lastColumn) : null;
                if (noWhitespace is { } rangeNoWhitespace && rangeNoWhitespace.Contains(prev) && rangeNoWhitespace != prev && cur.Contains(rangeNoWhitespace) && cur != rangeNoWhitespace)
                {
                    oneRangesWithTrivia.Add(rangeNoWhitespace);
                }

                // add line/block range
                var rangeFull = new Range(prev.StartLineNumber, 1, prev.EndLineNumber, model.GetLineMaxColumn(prev.EndLineNumber));
                if (rangeFull.Contains(prev) && rangeFull != noWhitespace && cur.Contains(rangeFull) && cur != rangeFull)
                {
                    oneRangesWithTrivia.Add(rangeFull);
                }
            }

            oneRangesWithTrivia.Add(cur);
        }

        return oneRangesWithTrivia;
    }
}
