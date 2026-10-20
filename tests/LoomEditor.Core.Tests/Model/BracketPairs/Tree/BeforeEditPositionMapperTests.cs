using LoomEditor.Core.Model;
using LoomEditor.Core.Model.BracketPairs.Tree;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.BracketPairs.Tree;

// Monaco: suite 'Bracket Pair Colorizer - BeforeEditPositionMapper'
public class BeforeEditPositionMapperTests
{
    // Monaco: 'Single-Line 1'
    [Fact]
    public void SingleLine1()
    {
        Assert.Equal(
            [
                "0  1  2  3  x  y  7  8  9  ", // The line

                "0  0  0  0  0  0  0  0  0  0  ", // the old line numbers
                "0  1  2  3  4  5  7  8  9  10 ", // the old columns

                "0  0  0  0  0  0  ∞  ∞  ∞  ∞  ", // line count until next change
                "4  3  2  1  0  0  ∞  ∞  ∞  ∞  ", // column count until next change
            ],
            Compute(
                ["0123456789"],
                [new TextEdit(Length.Of(0, 4), Length.Of(0, 7), "xy")]));
    }

    // Monaco: 'Single-Line 2'
    [Fact]
    public void SingleLine2()
    {
        Assert.Equal(
            [
                "0  1  x  x  x  x  4  5  y  y  6  7  8  9  ",

                "0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  ",
                "0  1  2  3  4  5  4  5  6  7  6  7  8  9  10 ",

                "0  0  0  0  0  0  0  0  0  0  ∞  ∞  ∞  ∞  ∞  ",
                "2  1  0  0  0  0  2  1  0  0  ∞  ∞  ∞  ∞  ∞  ",
            ],
            Compute(
                ["0123456789"],
                [
                    new TextEdit(Length.Of(0, 2), Length.Of(0, 4), "xxxx"),
                    new TextEdit(Length.Of(0, 6), Length.Of(0, 6), "yy"),
                ]));
    }

    // Monaco: 'Multi-Line Replace 1'
    [Fact]
    public void MultiLineReplace1()
    {
        Assert.Equal(
            [
                "₀  ₁  ₂  x  y  3  4  5  6  7  8  9  ",

                "0  0  0  0  0  1  1  1  1  1  1  1  1  ",
                "0  1  2  3  4  3  4  5  6  7  8  9  10 ",

                "0  0  0  0  0  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ",
                "3  2  1  0  0  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ",
                // ------------------
                "⁰  ¹  ²  ³  ⁴  ⁵  ⁶  ⁷  ⁸  ⁹  ",

                "2  2  2  2  2  2  2  2  2  2  2  ",
                "0  1  2  3  4  5  6  7  8  9  10 ",

                "∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ",
                "∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ",
            ],
            Compute(
                [
                    "₀₁₂₃₄₅₆₇₈₉",
                    "0123456789",
                    "⁰¹²³⁴⁵⁶⁷⁸⁹",
                ],
                [new TextEdit(Length.Of(0, 3), Length.Of(1, 3), "xy")]));
    }

    // Monaco: 'Multi-Line Replace 2'
    [Fact]
    public void MultiLineReplace2()
    {
        Assert.Equal(
            [
                "₀  ₁  ₂  a  b  0  1  2  3  4  c  7  8  ",

                "0  0  0  0  0  1  1  1  1  1  1  1  1  1  ",
                "0  1  2  3  4  0  1  2  3  4  5  7  8  9  ",

                "0  0  0  0  0  0  0  0  0  0  0  ∞  ∞  ∞  ",
                "3  2  1  0  0  5  4  3  2  1  0  ∞  ∞  ∞  ",
                // ------------------
                "⁰  ¹  ²  ³  ⁴  ⁵  ⁶  ⁷  ⁸  ⁹  ",

                "2  2  2  2  2  2  2  2  2  2  2  ",
                "0  1  2  3  4  5  6  7  8  9  10 ",

                "∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ",
                "∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ",
            ],
            Compute(
                [
                    "₀₁₂₃₄₅₆₇₈₉",
                    "012345678",
                    "⁰¹²³⁴⁵⁶⁷⁸⁹",
                ],
                [
                    new TextEdit(Length.Of(0, 3), Length.Of(1, 0), "ab"),
                    new TextEdit(Length.Of(1, 5), Length.Of(1, 7), "c"),
                ]));
    }

    // Monaco: 'Multi-Line Replace 3'
    [Fact]
    public void MultiLineReplace3()
    {
        Assert.Equal(
            [
                "₀  ₁  ₂  a  b  0  1  2  3  4  c  7  d  ⁴  ⁵  ⁶  ⁷  ⁸  ⁹  ",

                "0  0  0  0  0  1  1  1  1  1  1  1  1  2  2  2  2  2  2  2  ",
                "0  1  2  3  4  0  1  2  3  4  5  7  8  4  5  6  7  8  9  10 ",

                "0  0  0  0  0  0  0  0  0  0  0  0  0  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ",
                "3  2  1  0  0  5  4  3  2  1  0  1  0  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ",
            ],
            Compute(
                [
                    "₀₁₂₃₄₅₆₇₈₉",
                    "012345678",
                    "⁰¹²³⁴⁵⁶⁷⁸⁹",
                ],
                [
                    new TextEdit(Length.Of(0, 3), Length.Of(1, 0), "ab"),
                    new TextEdit(Length.Of(1, 5), Length.Of(1, 7), "c"),
                    new TextEdit(Length.Of(1, 8), Length.Of(2, 4), "d"),
                ]));
    }

    // Monaco: 'Multi-Line Insert 1'
    [Fact]
    public void MultiLineInsert1()
    {
        Assert.Equal(
            [
                "0  1  2  a  ",

                "0  0  0  0  0  ",
                "0  1  2  3  4  ",

                "0  0  0  0  0  ",
                "3  2  1  0  0  ",
                // ------------------
                "b  5  6  7  8  ",

                "1  0  0  0  0  0  ",
                "0  5  6  7  8  9  ",

                "0  ∞  ∞  ∞  ∞  ∞  ",
                "0  ∞  ∞  ∞  ∞  ∞  ",
            ],
            Compute(
                ["012345678"],
                [new TextEdit(Length.Of(0, 3), Length.Of(0, 5), "a\nb")]));
    }

    // Monaco: 'Multi-Line Insert 2'
    [Fact]
    public void MultiLineInsert2()
    {
        Assert.Equal(
            [
                "0  1  2  a  ",

                "0  0  0  0  0  ",
                "0  1  2  3  4  ",

                "0  0  0  0  0  ",
                "3  2  1  0  0  ",
                // ------------------
                "b  5  6  x  ",

                "1  0  0  0  0  ",
                "0  5  6  7  8  ",

                "0  0  0  0  0  ",
                "0  2  1  0  0  ",
                // ------------------
                "y  8  ",

                "1  0  0  ",
                "0  8  9  ",

                "0  ∞  ∞  ",
                "0  ∞  ∞  ",
            ],
            Compute(
                ["012345678"],
                [
                    new TextEdit(Length.Of(0, 3), Length.Of(0, 5), "a\nb"),
                    new TextEdit(Length.Of(0, 7), Length.Of(0, 8), "x\ny"),
                ]));
    }

    // Monaco: 'Multi-Line Replace/Insert 1'
    [Fact]
    public void MultiLineReplaceInsert1()
    {
        Assert.Equal(
            [
                "₀  ₁  ₂  a  a  a  ",
                "0  0  0  0  0  0  0  ",
                "0  1  2  3  4  5  6  ",

                "0  0  0  0  0  0  0  ",
                "3  2  1  0  0  0  0  ",
                // ------------------
                "b  b  b  1  2  3  4  5  6  7  8  ",

                "1  1  1  1  1  1  1  1  1  1  1  1  ",
                "0  1  2  1  2  3  4  5  6  7  8  9  ",

                "0  0  0  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ",
                "0  0  0  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ",
                // ------------------
                "⁰  ¹  ²  ³  ⁴  ⁵  ⁶  ⁷  ⁸  ⁹  ",

                "2  2  2  2  2  2  2  2  2  2  2  ",
                "0  1  2  3  4  5  6  7  8  9  10 ",

                "∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ",
                "∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ",
            ],
            Compute(
                [
                    "₀₁₂₃₄₅₆₇₈₉",
                    "012345678",
                    "⁰¹²³⁴⁵⁶⁷⁸⁹",
                ],
                [new TextEdit(Length.Of(0, 3), Length.Of(1, 1), "aaa\nbbb")]));
    }

    // Monaco: 'Multi-Line Replace/Insert 2'
    [Fact]
    public void MultiLineReplaceInsert2()
    {
        Assert.Equal(
            [
                "₀  ₁  ₂  a  a  a  ",

                "0  0  0  0  0  0  0  ",
                "0  1  2  3  4  5  6  ",

                "0  0  0  0  0  0  0  ",
                "3  2  1  0  0  0  0  ",
                // ------------------
                "b  b  b  1  2  3  4  x  ",

                "1  1  1  1  1  1  1  1  1  ",
                "0  1  2  1  2  3  4  5  6  ",

                "0  0  0  0  0  0  0  0  0  ",
                "0  0  0  4  3  2  1  0  0  ",
                // ------------------
                "y  5  6  k  ",

                "2  1  1  1  1  ",
                "0  5  6  7  8  ",

                "0  0  0  0  0  ",
                "0  2  1  0  0  ",
                // ------------------
                "l  ⁴  ⁵  ⁶  ⁷  ⁸  ⁹  ",

                "2  2  2  2  2  2  2  2  ",
                "0  4  5  6  7  8  9  10 ",

                "0  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ",
                "0  ∞  ∞  ∞  ∞  ∞  ∞  ∞  ",
            ],
            Compute(
                [
                    "₀₁₂₃₄₅₆₇₈₉",
                    "012345678",
                    "⁰¹²³⁴⁵⁶⁷⁸⁹",
                ],
                [
                    new TextEdit(Length.Of(0, 3), Length.Of(1, 1), "aaa\nbbb"),
                    new TextEdit(Length.Of(1, 5), Length.Of(1, 5), "x\ny"),
                    new TextEdit(Length.Of(1, 7), Length.Of(2, 4), "k\nl"),
                ]));
    }

    [Fact]
    public void FromModelContentChanges_ReversesTheChanges()
    {
        // The model gives the changes in descending order.
        var changes = new[]
        {
            new ModelContentChange(new Range(3, 2, 3, 4), 20, 2, "x\nyz"),
            new ModelContentChange(new Range(1, 1, 2, 3), 0, 5, ""),
        };

        var edits = TextEditInfo.FromModelContentChanges(changes);

        Assert.Equal(2, edits.Count);
        Assert.Equal(Length.Of(0, 0), edits[0].StartOffset);
        Assert.Equal(Length.Of(1, 2), edits[0].EndOffset);
        Assert.Equal(Length.Zero, edits[0].NewLength);
        Assert.Equal(Length.Of(2, 1), edits[1].StartOffset);
        Assert.Equal(Length.Of(2, 3), edits[1].EndOffset);
        Assert.Equal(Length.Of(1, 2), edits[1].NewLength);
        Assert.Equal("[0,0...1,2) -> 0,0", edits[0].ToString());
    }

    /// <summary>Monaco: <c>compute</c>.</summary>
    private static string[] Compute(string[] inputArr, TextEdit[] edits)
    {
        var replacements = new List<(Range Range, string Text)>();
        foreach (var e in edits)
        {
            replacements.Add((Length.ToRange(e.StartOffset, e.EndOffset), e.NewText));
        }

        var newLines = Strings.SplitLines(ApplyLineColumnEdits(string.Join('\n', inputArr), replacements));

        var mapper = new BeforeEditPositionMapper(edits);

        var result = new List<string>();

        var lineIdx = 0;
        foreach (var line in newLines)
        {
            var lineLine = string.Empty;
            var colLine = string.Empty;
            var lineStr = string.Empty;

            var colDist = string.Empty;
            var lineDist = string.Empty;

            for (var colIdx = 0; colIdx <= line.Length; colIdx++)
            {
                var before = mapper.GetOffsetBeforeChange(Length.Of(lineIdx, colIdx));
                var beforeObj = before.ToObj();
                if (colIdx < line.Length)
                {
                    lineStr += RightPad(line[colIdx].ToString(), 3);
                }

                lineLine += RightPad(Num(beforeObj.LineCount), 3);
                colLine += RightPad(Num(beforeObj.ColumnCount), 3);

                var distLen = mapper.GetDistanceToNextChange(Length.Of(lineIdx, colIdx));
                if (distLen is null)
                {
                    lineDist += "∞  ";
                    colDist += "∞  ";
                }
                else
                {
                    var dist = distLen.Value.ToObj();
                    lineDist += RightPad(Num(dist.LineCount), 3);
                    colDist += RightPad(Num(dist.ColumnCount), 3);
                }
            }

            result.Add(lineStr);

            result.Add(lineLine);
            result.Add(colLine);

            result.Add(lineDist);
            result.Add(colDist);

            lineIdx++;
        }

        return [.. result];
    }

    // Monaco: class TextEdit extends TextEditInfo
    private sealed class TextEdit(Length startOffset, Length endOffset, string newText)
        : TextEditInfo(startOffset, endOffset, Length.OfString(newText))
    {
        public string NewText { get; } = newText;
    }

    // Monaco: class PositionOffsetTransformer
    private sealed class PositionOffsetTransformer
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

        public int GetOffset(Position position)
        {
            return _lineStartOffsetByLineIdx[position.LineNumber - 1] + position.Column - 1;
        }
    }

    // Monaco: applyLineColumnEdits
    private static string ApplyLineColumnEdits(string text, List<(Range Range, string Text)> edits)
    {
        var transformer = new PositionOffsetTransformer(text);
        var offsetEdits = new List<(int StartOffset, int EndOffset, string Text)>();
        foreach (var e in edits)
        {
            offsetEdits.Add((transformer.GetOffset(e.Range.Start), transformer.GetOffset(e.Range.End), e.Text));
        }

        offsetEdits.Sort((a, b) => b.StartOffset.CompareTo(a.StartOffset));

        foreach (var edit in offsetEdits)
        {
            text = string.Concat(text.AsSpan(0, edit.StartOffset), edit.Text, text.AsSpan(edit.EndOffset));
        }

        return text;
    }

    private static string Num(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    // Monaco: rightPad
    private static string RightPad(string str, int len)
    {
        return str.PadRight(len);
    }
}
