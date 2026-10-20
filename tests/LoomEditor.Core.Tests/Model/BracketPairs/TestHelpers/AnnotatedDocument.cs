// Ported from microsoft/vscode: src/vs/editor/test/common/model/bracketPairColorizer/getBracketPairsInRange.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.BracketPairs.TestHelpers;

/// <summary>Converts between the offsets and the positions of a text (Monaco: <c>PositionOffsetTransformer</c>).</summary>
/// <remarks>
/// Difference from Monaco, whose <c>getPosition</c> uses <c>findIndex</c> and so always answers the first line: it answers the
/// last line that starts at or before the offset. The results are the same for single-line texts.
/// </remarks>
internal sealed class PositionOffsetTransformer
{
    private readonly List<int> _lineStartOffsetByLineIdx = [0];

    public PositionOffsetTransformer(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                _lineStartOffsetByLineIdx.Add(i + 1);
            }
        }
    }

    public int GetOffset(Position position) => _lineStartOffsetByLineIdx[position.LineNumber - 1] + position.Column - 1;

    public Position GetPosition(int offset)
    {
        var lineIdx = _lineStartOffsetByLineIdx.FindLastIndex(lineStartOffset => lineStartOffset <= offset);
        return new Position(lineIdx + 1, offset - _lineStartOffsetByLineIdx[lineIdx] + 1);
    }
}

/// <summary>
/// A text with positions marked by the superscript digits <c>⁰</c> … <c>⁹</c>; the marks are removed from the text
/// (Monaco: <c>AnnotatedDocument</c>).
/// </summary>
internal sealed class AnnotatedDocument
{
    private const string Numbers = "⁰¹²³⁴⁵⁶⁷⁸⁹";

    private readonly Dictionary<int, Position> _positions = [];

    public AnnotatedDocument(string src)
    {
        var text = new System.Text.StringBuilder();
        var offsetPositions = new Dictionary<int, int>();

        var offset = 0;
        foreach (var c in src)
        {
            var idx = Numbers.IndexOf(c, StringComparison.Ordinal);
            if (idx >= 0)
            {
                offsetPositions[idx] = offset;
            }
            else
            {
                text.Append(c);
                offset++;
            }
        }

        Text = text.ToString();

        var mapper = new PositionOffsetTransformer(Text);
        foreach (var (idx, markOffset) in offsetPositions)
        {
            _positions[idx] = mapper.GetPosition(markOffset);
        }
    }

    /// <summary>The text without the marks.</summary>
    public string Text { get; }

    /// <summary>The range between the marks <paramref name="start"/> and <paramref name="end"/>.</summary>
    public Range GetRange(int start, int end) => new(_positions[start], _positions[end]);
}
