// Ported from microsoft/vscode: src/vs/editor/contrib/smartSelect/browser/bracketSelections.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Features.SmartSelect;

/// <summary>The inner and outer range of each bracket pair around each position (Monaco: <c>BracketSelectionRangeProvider</c>).</summary>
/// <remarks>
/// Behavior difference: Monaco yields to the event loop every 30 ms for at most 2 rounds; here the search runs without yielding
/// but stops after <see cref="MaxDuration"/> (2 rounds of 30 ms) for each position, keeping the ranges it found.
/// Threading: UI thread only.
/// </remarks>
internal sealed class BracketSelectionRangeProvider : ISelectionRangeProvider
{
    /// <summary>The time budget of the search around one position.</summary>
    public TimeSpan MaxDuration { get; init; } = TimeSpan.FromMilliseconds(60);

    public IReadOnlyList<IReadOnlyList<Range>> ProvideSelectionRanges(TextModel model, IReadOnlyList<Position> positions)
    {
        var result = new List<IReadOnlyList<Range>>();
        foreach (Position position in positions)
        {
            var bucket = new List<Range>();
            result.Add(bucket);

            var ranges = new Dictionary<string, Queue<Range>>();
            var clock = Stopwatch.StartNew();
            BracketsRight(clock, model, position, ranges);
            BracketsLeft(clock, model, position, ranges, bucket);
        }

        return result;
    }

    private void BracketsRight(Stopwatch clock, TextModel model, Position pos, Dictionary<string, Queue<Range>> ranges)
    {
        var counts = new Dictionary<string, int>();
        while (model.BracketPairs.FindNextBracket(pos) is { } bracket)
        {
            if (clock.Elapsed > MaxDuration)
            {
                break;
            }

            if (bracket.BracketInfo.IsOpeningBracket)
            {
                string key = bracket.BracketInfo.BracketText;
                // wait for closing
                counts[key] = counts.GetValueOrDefault(key) + 1;
            }
            else
            {
                string key = ((ClosingBracketKind)bracket.BracketInfo).GetOpeningBrackets()[0].BracketText;
                // process closing
                int val = counts.GetValueOrDefault(key) - 1;
                counts[key] = Math.Max(0, val);
                if (val < 0)
                {
                    if (!ranges.TryGetValue(key, out Queue<Range>? list))
                    {
                        list = new Queue<Range>();
                        ranges[key] = list;
                    }

                    list.Enqueue(bracket.Range);
                }
            }

            pos = bracket.Range.End;
        }
    }

    private void BracketsLeft(Stopwatch clock, TextModel model, Position pos, Dictionary<string, Queue<Range>> ranges, List<Range> bucket)
    {
        var counts = new Dictionary<string, int>();
        while (model.BracketPairs.FindPrevBracket(pos) is { } bracket)
        {
            if (clock.Elapsed > MaxDuration)
            {
                break;
            }

            if (!bracket.BracketInfo.IsOpeningBracket)
            {
                string key = ((ClosingBracketKind)bracket.BracketInfo).GetOpeningBrackets()[0].BracketText;
                // wait for opening
                counts[key] = counts.GetValueOrDefault(key) + 1;
            }
            else
            {
                string key = bracket.BracketInfo.BracketText;
                // opening
                int val = counts.GetValueOrDefault(key) - 1;
                counts[key] = Math.Max(0, val);
                if (val < 0 && ranges.TryGetValue(key, out Queue<Range>? list))
                {
                    Range closing = list.Dequeue();
                    if (list.Count == 0)
                    {
                        ranges.Remove(key);
                    }

                    var innerBracket = new Range(bracket.Range.End, closing.Start);
                    var outerBracket = new Range(bracket.Range.Start, closing.End);
                    bucket.Add(innerBracket);
                    bucket.Add(outerBracket);
                    AddBracketLeading(model, outerBracket, bucket);
                }
            }

            pos = bracket.Range.Start;
        }
    }

    private static void AddBracketLeading(TextModel model, Range bracket, List<Range> bucket)
    {
        if (bracket.StartLineNumber == bracket.EndLineNumber)
        {
            return;
        }

        // xxxxxxxx {
        //
        // }
        int startLine = bracket.StartLineNumber;
        int column = model.GetLineFirstNonWhitespaceColumn(startLine);
        if (column != 0 && column != bracket.StartColumn)
        {
            bucket.Add(new Range(new Position(startLine, column), bracket.End));
            bucket.Add(new Range(new Position(startLine, 1), bracket.End));
        }

        // xxxxxxxx
        // {
        //
        // }
        int aboveLine = startLine - 1;
        if (aboveLine > 0)
        {
            int aboveColumn = model.GetLineFirstNonWhitespaceColumn(aboveLine);
            if (aboveColumn == bracket.StartColumn && aboveColumn != model.GetLineLastNonWhitespaceColumn(aboveLine))
            {
                bucket.Add(new Range(new Position(aboveLine, aboveColumn), bracket.End));
                bucket.Add(new Range(new Position(aboveLine, 1), bracket.End));
            }
        }
    }
}
