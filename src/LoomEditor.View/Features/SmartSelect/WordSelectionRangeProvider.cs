// Ported from microsoft/vscode: src/vs/editor/contrib/smartSelect/browser/wordSelections.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Features.SmartSelect;

/// <summary>The sub-word, the word, the whitespace-only line and the whole model around each position (Monaco: <c>WordSelectionRangeProvider</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class WordSelectionRangeProvider(bool selectSubwords = true) : ISelectionRangeProvider
{
    public IReadOnlyList<IReadOnlyList<Range>> ProvideSelectionRanges(TextModel model, IReadOnlyList<Position> positions)
    {
        var result = new List<IReadOnlyList<Range>>();
        foreach (Position position in positions)
        {
            var bucket = new List<Range>();
            result.Add(bucket);
            if (selectSubwords)
            {
                AddInWordRanges(bucket, model, position);
            }

            AddWordRanges(bucket, model, position);
            AddWhitespaceLine(bucket, model, position);
            bucket.Add(model.FullModelRange);
        }

        return result;
    }

    private static bool IsLower(int ch) => ch is >= 'a' and <= 'z';

    private static bool IsUpper(int ch) => ch is >= 'A' and <= 'Z';

    // charCodeAt gives NaN out of range in JavaScript; 0 is never a letter, an underscore or a dash.
    private static int CharAt(string word, int index) => (uint)index < (uint)word.Length ? word[index] : 0;

    private static void AddInWordRanges(List<Range> bucket, TextModel model, Position pos)
    {
        if (model.GetWordAtPosition(pos) is not { } obj)
        {
            return;
        }

        string word = obj.Word;
        int offset = pos.Column - obj.StartColumn;
        int start = offset;
        int end = offset;
        int lastCh = 0;

        // LEFT anchor (start)
        for (; start >= 0; start--)
        {
            int ch = CharAt(word, start);
            if (start != offset && (ch == '_' || ch == '-'))
            {
                // foo-bar OR foo_bar
                break;
            }

            if (IsLower(ch) && IsUpper(lastCh))
            {
                // fooBar
                break;
            }

            lastCh = ch;
        }

        start += 1;

        // RIGHT anchor (end)
        for (; end < word.Length; end++)
        {
            int ch = CharAt(word, end);
            if (IsUpper(ch) && IsLower(lastCh))
            {
                // fooBar
                break;
            }

            if (ch == '_' || ch == '-')
            {
                // foo-bar OR foo_bar
                break;
            }

            lastCh = ch;
        }

        if (start < end)
        {
            bucket.Add(new Range(pos.LineNumber, obj.StartColumn + start, pos.LineNumber, obj.StartColumn + end));
        }
    }

    private static void AddWordRanges(List<Range> bucket, TextModel model, Position pos)
    {
        if (model.GetWordAtPosition(pos) is { } word)
        {
            bucket.Add(new Range(pos.LineNumber, word.StartColumn, pos.LineNumber, word.EndColumn));
        }
    }

    private static void AddWhitespaceLine(List<Range> bucket, TextModel model, Position pos)
    {
        if (model.GetLineLength(pos.LineNumber) > 0
            && model.GetLineFirstNonWhitespaceColumn(pos.LineNumber) == 0
            && model.GetLineLastNonWhitespaceColumn(pos.LineNumber) == 0)
        {
            bucket.Add(new Range(pos.LineNumber, 1, pos.LineNumber, model.GetLineMaxColumn(pos.LineNumber)));
        }
    }
}
