// Ported from microsoft/vscode: src/vs/editor/common/model/textModelSearch.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Search;

internal static partial class TextModelSearch
{
    /// <summary>Counts the line feeds before an offset, to turn \n-joined offsets into CRLF model offsets.</summary>
    /// <remarks>Threading: immutable after construction.</remarks>
    private sealed class LineFeedCounter
    {
        private readonly List<int> _lineFeedsOffsets = [];

        public LineFeedCounter(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    _lineFeedsOffsets.Add(i);
                }
            }
        }

        public int FindLineFeedCountBeforeOffset(int offset)
        {
            var offsets = _lineFeedsOffsets;
            var min = 0;
            var max = offsets.Count - 1;

            if (max == -1)
            {
                // no line feeds
                return 0;
            }

            if (offset <= offsets[0])
            {
                // before first line feed
                return 0;
            }

            while (min < max)
            {
                var mid = min + ((max - min) / 2);

                if (offsets[mid] >= offset)
                {
                    max = mid - 1;
                }
                else if (offsets[mid + 1] >= offset)
                {
                    // bingo!
                    min = mid;
                    max = mid;
                }
                else
                {
                    min = mid + 1;
                }
            }

            return min + 1;
        }
    }
}
