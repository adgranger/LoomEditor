// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/indentRangeProvider.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;

namespace LoomEditor.View.Features.Folding;

/// <summary>The collector of the ranges found by <see cref="IndentRanges.ComputeRanges"/> (Monaco: <c>RangesCollector</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class RangesCollector(IFoldingLimitReporter foldingRangesLimit)
{
    private readonly List<int> _startIndexes = [];
    private readonly List<int> _endIndexes = [];
    private readonly List<int> _indentOccurrences = [];

    public void InsertFirst(int startLineNumber, int endLineNumber, int indent)
    {
        if (startLineNumber > FoldingRegions.MaxLineNumber || endLineNumber > FoldingRegions.MaxLineNumber)
        {
            return;
        }

        _startIndexes.Add(startLineNumber);
        _endIndexes.Add(endLineNumber);
        if (indent < 1000)
        {
            while (_indentOccurrences.Count <= indent)
            {
                _indentOccurrences.Add(0);
            }

            _indentOccurrences[indent]++;
        }
    }

    public FoldingRegions ToIndentRanges(TextModel model)
    {
        int length = _startIndexes.Count;
        int limit = foldingRangesLimit.Limit;
        if (length <= limit)
        {
            foldingRangesLimit.Update(length, null);

            // reverse and create arrays of the exact length
            var startIndexes = new uint[length];
            var endIndexes = new uint[length];
            for (int i = length - 1, k = 0; i >= 0; i--, k++)
            {
                startIndexes[k] = (uint)_startIndexes[i];
                endIndexes[k] = (uint)_endIndexes[i];
            }

            return new FoldingRegions(startIndexes, endIndexes);
        }
        else
        {
            foldingRangesLimit.Update(length, limit);

            int entries = 0;
            int maxIndent = _indentOccurrences.Count;
            for (int i = 0; i < _indentOccurrences.Count; i++)
            {
                int n = _indentOccurrences[i];
                if (n != 0)
                {
                    if (n + entries > limit)
                    {
                        maxIndent = i;
                        break;
                    }

                    entries += n;
                }
            }

            int tabSize = model.Options.TabSize;
            // reverse and create arrays of the exact length
            var startIndexes = new uint[limit];
            var endIndexes = new uint[limit];
            for (int i = length - 1, k = 0; i >= 0; i--)
            {
                int startIndex = _startIndexes[i];
                string lineContent = model.GetLineContent(startIndex);
                int indent = TextModelUtils.ComputeIndentLevel(lineContent, tabSize);
                if (indent < maxIndent || (indent == maxIndent && entries++ < limit))
                {
                    startIndexes[k] = (uint)startIndex;
                    endIndexes[k] = (uint)_endIndexes[i];
                    k++;
                }
            }

            return new FoldingRegions(startIndexes, endIndexes);
        }
    }
}

/// <summary>Finds the folding ranges of a model from indentation and markers (Monaco: <c>computeRanges</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static class IndentRanges
{
    private const int MaxFoldingRegionsForIndentDefault = 5000;

    private static readonly IFoldingLimitReporter FoldingRangesLimitDefault = new DefaultLimitReporter();

    private sealed class PreviousRegion
    {
        public int Indent; // indent or -2 if a marker
        public int EndAbove; // end line number for the region above
        public int Line; // start line of the region. Only used for marker regions.
    }

    private sealed class DefaultLimitReporter : IFoldingLimitReporter
    {
        public int Limit => MaxFoldingRegionsForIndentDefault;

        public void Update(int computed, int? limited)
        {
        }
    }

    public static FoldingRegions ComputeRanges(TextModel model, bool offSide, FoldingMarkers? markers = null, IFoldingLimitReporter? foldingRangesLimit = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        int tabSize = model.Options.TabSize;
        var result = new RangesCollector(foldingRangesLimit ?? FoldingRangesLimitDefault);

        Regex? pattern = null;
        Regex? startPattern = null;
        Regex? endPattern = null;
        if (markers is not null)
        {
            // Monaco compares the JavaScript flags; the .NET options play that role (the `g` flag has no equivalent).
            if (markers.Start.Options == markers.End.Options)
            {
                pattern = new Regex($"({markers.Start})|(?:{markers.End})", markers.Start.Options);
            }
            else
            {
                startPattern = markers.Start;
                endPattern = markers.End;
            }
        }

        var previousRegions = new List<PreviousRegion>();
        int lineCount = model.LineCount + 1;
        previousRegions.Add(new PreviousRegion { Indent = -1, EndAbove = lineCount, Line = lineCount }); // sentinel, to make sure there's at least one entry

        for (int line = model.LineCount; line > 0; line--)
        {
            string lineContent = model.GetLineContent(line);
            int indent = TextModelUtils.ComputeIndentLevel(lineContent, tabSize);
            PreviousRegion previous = previousRegions[^1];
            if (indent == -1)
            {
                if (offSide)
                {
                    // for offSide languages, empty lines are associated to the previous block
                    // note: the next block is already written to the results, so this only
                    // impacts the end position of the block before
                    previous.EndAbove = line;
                }

                continue; // only whitespace
            }

            bool isStartMatch = false;
            bool isEndMatch = false;
            if (pattern is not null)
            {
                Match m = pattern.Match(lineContent);
                if (m.Success)
                {
                    // An empty capture is falsy in JavaScript.
                    isStartMatch = m.Groups[1].Success && m.Groups[1].Length > 0;
                    isEndMatch = !isStartMatch;
                }
            }
            else
            {
                if (startPattern is not null)
                {
                    isStartMatch = startPattern.IsMatch(lineContent);
                }

                if (!isStartMatch && endPattern is not null)
                {
                    isEndMatch = endPattern.IsMatch(lineContent);
                }
            }

            if (isStartMatch || isEndMatch)
            {
                // folding pattern match
                if (isStartMatch)
                {
                    // start pattern match: discard all regions until the folding pattern
                    int i = previousRegions.Count - 1;
                    while (i > 0 && previousRegions[i].Indent != -2)
                    {
                        i--;
                    }

                    if (i > 0)
                    {
                        previousRegions.RemoveRange(i + 1, previousRegions.Count - (i + 1));
                        previous = previousRegions[i];

                        // new folding range from pattern, includes the end line
                        result.InsertFirst(line, previous.Line, indent);
                        previous.Line = line;
                        previous.Indent = indent;
                        previous.EndAbove = line;
                        continue;
                    }

                    // no end marker found, treat line as a regular line
                }
                else
                {
                    // end pattern match
                    previousRegions.Add(new PreviousRegion { Indent = -2, EndAbove = line, Line = line });
                    continue;
                }
            }

            if (previous.Indent > indent)
            {
                // discard all regions with larger indent
                do
                {
                    previousRegions.RemoveAt(previousRegions.Count - 1);
                    previous = previousRegions[^1];
                }
                while (previous.Indent > indent);

                // new folding range
                int endLineNumber = previous.EndAbove - 1;
                if (endLineNumber - line >= 1)
                {
                    // needs at east size 1
                    result.InsertFirst(line, endLineNumber, indent);
                }
            }

            if (previous.Indent == indent)
            {
                previous.EndAbove = line;
            }
            else
            {
                // previous.indent < indent: new region with a bigger indent
                previousRegions.Add(new PreviousRegion { Indent = indent, EndAbove = line, Line = line });
            }
        }

        return result.ToIndentRanges(model);
    }
}