using LoomEditor.Core.Model;
using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;
using Position = LoomEditor.Core.Text.Position;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeTextBufferQueryTests
{
    private static PieceTreeTextBuffer Create(string text, bool normalizeEol = true) => TestTextBuffers.Create([text], normalizeEol);

    [Fact]
    public void GetRangeAt_InsideLine_ReturnsRangeOnSameLine()
    {
        PieceTreeTextBuffer buffer = Create("hello\nworld");

        Assert.Equal(new Range(1, 2, 1, 5), buffer.GetRangeAt(1, 3));
        Assert.Equal(new Range(2, 2, 2, 4), buffer.GetRangeAt(7, 2));
    }

    [Fact]
    public void GetRangeAt_AcrossLines_ReturnsMultiLineRange()
    {
        PieceTreeTextBuffer buffer = Create("hello\nworld\nfoo");

        Assert.Equal(new Range(1, 4, 2, 3), buffer.GetRangeAt(3, 5));
        Assert.Equal(new Range(1, 1, 3, 4), buffer.GetRangeAt(0, buffer.GetLength()));
    }

    [Fact]
    public void GetValueInRange_CrlfBuffer_HonorsEndOfLinePreference()
    {
        PieceTreeTextBuffer buffer = Create("a\r\nb\r\nc");
        var range = new Range(1, 1, 3, 2);

        Assert.Equal("\r\n", buffer.GetEol());
        Assert.Equal("a\r\nb\r\nc", buffer.GetValueInRange(range));
        Assert.Equal("a\r\nb\r\nc", buffer.GetValueInRange(range, EndOfLinePreference.TextDefined));
        Assert.Equal("a\nb\nc", buffer.GetValueInRange(range, EndOfLinePreference.LF));
        Assert.Equal("a\r\nb\r\nc", buffer.GetValueInRange(range, EndOfLinePreference.CRLF));
    }

    [Fact]
    public void GetValueInRange_EmptyRange_ReturnsEmptyString()
    {
        PieceTreeTextBuffer buffer = Create("abc");

        Assert.Equal(string.Empty, buffer.GetValueInRange(new Range(1, 2, 1, 2)));
    }

    [Theory]
    [InlineData("a\r\nb\r\nc")]
    [InlineData("a\nb\nc")]
    public void GetValueLengthInRange_Ranges_EqualsLengthOfGetValueInRange(string text)
    {
        PieceTreeTextBuffer buffer = Create(text);
        Range[] ranges =
        [
            new(1, 1, 1, 2),
            new(1, 1, 2, 1),
            new(1, 2, 3, 1),
            new(1, 1, 3, 2),
            new(2, 1, 2, 1),
        ];

        foreach (Range range in ranges)
        {
            foreach (EndOfLinePreference eol in Enum.GetValues<EndOfLinePreference>())
            {
                Assert.Equal(
                    buffer.GetValueInRange(range, eol).Length,
                    buffer.GetValueLengthInRange(range, eol));
            }
        }
    }

    [Fact]
    public void GetCharacterCountInRange_SurrogatePair_CountsAsOne()
    {
        PieceTreeTextBuffer buffer = Create("a\U0001F600b");

        Assert.True(buffer.MightContainNonBasicAscii());
        Assert.Equal(4, buffer.GetLength());
        Assert.Equal(3, buffer.GetCharacterCountInRange(new Range(1, 1, 1, 5)));
        Assert.Equal(1, buffer.GetCharacterCountInRange(new Range(1, 2, 1, 4)));
    }

    [Fact]
    public void GetCharacterCountInRange_AsciiMultiLine_CountsLineBreaksWithPreference()
    {
        PieceTreeTextBuffer buffer = Create("ab\ncd");
        var range = new Range(1, 1, 2, 3);

        Assert.Equal(5, buffer.GetCharacterCountInRange(range));
        Assert.Equal(6, buffer.GetCharacterCountInRange(range, EndOfLinePreference.CRLF));
    }

    [Fact]
    public void GetCharacterCountInRange_NonAsciiMultiLine_CountsLineBreaks()
    {
        PieceTreeTextBuffer buffer = Create("a\U0001F600\nb");
        var range = new Range(1, 1, 2, 2);

        Assert.Equal(4, buffer.GetCharacterCountInRange(range));
        Assert.Equal(5, buffer.GetCharacterCountInRange(range, EndOfLinePreference.CRLF));
    }

    [Fact]
    public void GetLineMinMaxColumn_Lines_ReturnOneAndLengthPlusOne()
    {
        PieceTreeTextBuffer buffer = Create("abc\n\nhello");

        Assert.Equal(1, buffer.GetLineMinColumn(1));
        Assert.Equal(4, buffer.GetLineMaxColumn(1));
        Assert.Equal(1, buffer.GetLineMinColumn(2));
        Assert.Equal(1, buffer.GetLineMaxColumn(2));
        Assert.Equal(6, buffer.GetLineMaxColumn(3));
    }

    [Fact]
    public void GetLineFirstAndLastNonWhitespaceColumn_Lines_ReturnColumns()
    {
        PieceTreeTextBuffer buffer = Create("  ab  \n \t \n\nx");

        Assert.Equal(3, buffer.GetLineFirstNonWhitespaceColumn(1));
        Assert.Equal(5, buffer.GetLineLastNonWhitespaceColumn(1));
        Assert.Equal(0, buffer.GetLineFirstNonWhitespaceColumn(2));
        Assert.Equal(0, buffer.GetLineLastNonWhitespaceColumn(2));
        Assert.Equal(0, buffer.GetLineFirstNonWhitespaceColumn(3));
        Assert.Equal(0, buffer.GetLineLastNonWhitespaceColumn(3));
        Assert.Equal(1, buffer.GetLineFirstNonWhitespaceColumn(4));
        Assert.Equal(2, buffer.GetLineLastNonWhitespaceColumn(4));
    }

    [Fact]
    public void MightContainFlags_PlainAscii_AreFalse()
    {
        PieceTreeTextBuffer buffer = Create("hello\nworld");

        Assert.False(buffer.MightContainRtl());
        Assert.False(buffer.MightContainNonBasicAscii());
        Assert.False(buffer.MightContainUnusualLineTerminators());
    }

    [Fact]
    public void MightContainFlags_SpecialText_AreTrue()
    {
        Assert.True(Create("abc " + (char)0x05D0).MightContainRtl());
        Assert.True(Create("caf" + (char)0x00E9).MightContainNonBasicAscii());
        Assert.True(Create("a" + (char)0x2028 + "b").MightContainUnusualLineTerminators());
    }

    [Fact]
    public void ResetMightContainUnusualLineTerminators_AfterReset_IsFalse()
    {
        PieceTreeTextBuffer buffer = Create("a" + (char)0x2028 + "b");

        buffer.ResetMightContainUnusualLineTerminators();

        Assert.False(buffer.MightContainUnusualLineTerminators());
    }

    [Fact]
    public void Equals_SameTextDifferentChunks_IsTrue()
    {
        PieceTreeTextBuffer a = TestTextBuffers.Create(["ab", "c"]);
        PieceTreeTextBuffer b = TestTextBuffers.Create(["abc"]);
        PieceTreeTextBuffer c = TestTextBuffers.Create(["abd"]);

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
    }

    [Fact]
    public void Equals_DifferentBom_IsFalse()
    {
        PieceTreeTextBuffer withBom = Create("﻿abc");
        PieceTreeTextBuffer withoutBom = Create("abc");

        Assert.Equal("﻿", withBom.GetBom());
        Assert.False(withBom.Equals(withoutBom));
    }

    [Fact]
    public void CreateSnapshot_PreserveBom_ControlsLeadingBom()
    {
        PieceTreeTextBuffer buffer = Create("﻿abc");

        Assert.Equal("﻿abc", ReadAll(buffer.CreateSnapshot(preserveBom: true)));
        Assert.Equal("abc", ReadAll(buffer.CreateSnapshot(preserveBom: false)));
    }

    [Fact]
    public void SetEol_Crlf_RewritesLineBreaks()
    {
        PieceTreeTextBuffer buffer = Create("a\nb");

        buffer.SetEol("\r\n");

        Assert.Equal("\r\n", buffer.GetEol());
        Assert.Equal(4, buffer.GetLength());
        Assert.Equal("a\r\nb", buffer.GetValueInRange(new Range(1, 1, 2, 2)));
    }

    [Fact]
    public void SetEol_InvalidValue_Throws()
    {
        PieceTreeTextBuffer buffer = Create("a\nb");

        Assert.ThrowsAny<ArgumentException>(() => buffer.SetEol("\r"));
    }

    [Fact]
    public void LineAndCharQueries_Text_DelegateToTree()
    {
        PieceTreeTextBuffer buffer = Create("ab\ncd");

        Assert.Equal(2, buffer.GetLineCount());
        Assert.Equal(5, buffer.GetLength());
        Assert.Equal(["ab", "cd"], buffer.GetLinesContent());
        Assert.Equal("cd", buffer.GetLineContent(2));
        Assert.Equal(2, buffer.GetLineLength(2));
        Assert.Equal('d', buffer.GetLineCharCode(2, 1));
        Assert.Equal('\n', buffer.GetCharCode(2));
        Assert.Equal(new Position(2, 2), buffer.GetPositionAt(4));
        Assert.Equal(4, buffer.GetOffsetAt(2, 2));
        Assert.Equal("ab\ncd", buffer.GetNearestChunk(0));
    }

    private static string ReadAll(ITextSnapshot snapshot)
    {
        var sb = new System.Text.StringBuilder();
        while (snapshot.Read() is { } chunk)
        {
            sb.Append(chunk);
        }

        return sb.ToString();
    }
}
