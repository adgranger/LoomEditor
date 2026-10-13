using LoomEditor.Core.Model;
using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;
using LoomEditor.Core.Tests.Model.TestHelpers;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model;

/// <summary>
/// Ported from Monaco <c>textModel.test.ts</c>: <c>TextModelData.fromString</c>, part of
/// <c>Editor Model - TextModel</c> and <c>TextModel.mightContainRTL</c> (<c>nope</c>, <c>yes</c>).
/// Assertions with out-of-bounds ranges and <c>#50471</c> are ported with <c>ValidateRange</c> (P1-21);
/// the two <c>setValue</c> RTL tests with <c>SetValue</c> (P1-22).
/// </summary>
public class TextModelReadTests
{
    private const string HebrewText = "Hello,\nזוהי עובדה מבוססת שדעתו";
    private const string ArabicText = "Hello,\nهناك حقيقة مثبتة منذ زمن طويل";

    private static void AssertBufferData(string text, string eol, string[] lines, bool containsRtl, bool isBasicAscii)
    {
        // Monaco: createTextBuffer(text, TextModel.DEFAULT_CREATION_OPTIONS.defaultEOL)
        PieceTreeTextBuffer buffer = TestTextBuffers.Create([text]);

        Assert.Equal(eol, buffer.GetEol());
        Assert.Equal(lines, buffer.GetLinesContent());
        Assert.Equal(containsRtl, buffer.MightContainRtl());
        Assert.Equal(isBasicAscii, !buffer.MightContainNonBasicAscii());
    }

    // Monaco: 'one line text'
    [Fact]
    public void FromString_OneLineText()
    {
        AssertBufferData("Hello world!", "\n", ["Hello world!"], false, true);
    }

    // Monaco: 'multiline text'
    [Fact]
    public void FromString_MultilineText()
    {
        AssertBufferData("Hello,\r\ndear friend\nHow\rare\r\nyou?", "\r\n", ["Hello,", "dear friend", "How", "are", "you?"], false, true);
    }

    // Monaco: 'Non Basic ASCII 1'
    [Fact]
    public void FromString_NonBasicAscii1()
    {
        AssertBufferData("Hello,\nZürich", "\n", ["Hello,", "Zürich"], false, false);
    }

    // Monaco: 'containsRTL 1'
    [Fact]
    public void FromString_ContainsRtl1()
    {
        AssertBufferData(HebrewText, "\n", ["Hello,", "זוהי עובדה מבוססת שדעתו"], true, false);
    }

    // Monaco: 'containsRTL 2'
    [Fact]
    public void FromString_ContainsRtl2()
    {
        AssertBufferData(ArabicText, "\n", ["Hello,", "هناك حقيقة مثبتة منذ زمن طويل"], true, false);
    }

    // Monaco: 'getValueLengthInRange' (assertions with ranges inside the text)
    [Theory]
    [InlineData("\r\n")]
    [InlineData("\n")]
    public void GetValueLengthInRange_ValidRanges(string eol)
    {
        TextModel m = TestTextModels.Create($"My First Line{eol}My Second Line{eol}My Third Line");

        Assert.Equal("".Length, m.GetValueLengthInRange(new Range(1, 1, 1, 1)));
        Assert.Equal("M".Length, m.GetValueLengthInRange(new Range(1, 1, 1, 2)));
        Assert.Equal("y".Length, m.GetValueLengthInRange(new Range(1, 2, 1, 3)));
        Assert.Equal("My First Line".Length, m.GetValueLengthInRange(new Range(1, 1, 1, 14)));
        Assert.Equal($"My First Line{eol}".Length, m.GetValueLengthInRange(new Range(1, 1, 2, 1)));
        Assert.Equal($"y First Line{eol}".Length, m.GetValueLengthInRange(new Range(1, 2, 2, 1)));
        Assert.Equal($"y First Line{eol}M".Length, m.GetValueLengthInRange(new Range(1, 2, 2, 2)));
        Assert.Equal($"y First Line{eol}My Second Line{eol}".Length, m.GetValueLengthInRange(new Range(1, 2, 3, 1)));
    }

    // Monaco: 'getValueLengthInRange different EOL' (assertions with ranges inside the text)
    [Fact]
    public void GetValueLengthInRange_DifferentEol_CrlfText()
    {
        TextModel m = TestTextModels.Create("My First Line\r\nMy Second Line\r\nMy Third Line");

        Assert.Equal("My First Line\r\n".Length, m.GetValueLengthInRange(new Range(1, 1, 2, 1), EndOfLinePreference.TextDefined));
        Assert.Equal("My First Line\r\n".Length, m.GetValueLengthInRange(new Range(1, 1, 2, 1), EndOfLinePreference.CRLF));
        Assert.Equal("My First Line\n".Length, m.GetValueLengthInRange(new Range(1, 1, 2, 1), EndOfLinePreference.LF));
    }

    // Monaco: 'getValueLengthInRange different EOL' (assertions with ranges inside the text)
    [Fact]
    public void GetValueLengthInRange_DifferentEol_LfText()
    {
        TextModel m = TestTextModels.Create("My First Line\nMy Second Line\nMy Third Line");

        Assert.Equal("My First Line\n".Length, m.GetValueLengthInRange(new Range(1, 1, 2, 1), EndOfLinePreference.TextDefined));
        Assert.Equal("My First Line\n".Length, m.GetValueLengthInRange(new Range(1, 1, 2, 1), EndOfLinePreference.LF));
        Assert.Equal("My First Line\r\n".Length, m.GetValueLengthInRange(new Range(1, 1, 2, 1), EndOfLinePreference.CRLF));
    }

    // Monaco: 'getLineFirstNonWhitespaceColumn'
    [Fact]
    public void GetLineFirstNonWhitespaceColumn_VariousLines()
    {
        TextModel model = TestTextModels.Create(string.Join('\n', "asd", " asd", "\tasd", "  asd", "\t\tasd", " ", "  ", "\t", "\t\t", "  \tasd", "", ""));
        int[] expected = [1, 2, 2, 3, 3, 0, 0, 0, 0, 4, 0, 0];

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], model.GetLineFirstNonWhitespaceColumn(i + 1));
        }
    }

    // Monaco: 'getLineLastNonWhitespaceColumn'
    [Fact]
    public void GetLineLastNonWhitespaceColumn_VariousLines()
    {
        TextModel model = TestTextModels.Create(string.Join('\n', "asd", "asd ", "asd\t", "asd  ", "asd\t\t", " ", "  ", "\t", "\t\t", "asd  \t", "", ""));
        int[] expected = [4, 4, 4, 4, 4, 0, 0, 0, 0, 4, 0, 0];

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], model.GetLineLastNonWhitespaceColumn(i + 1));
        }
    }

    // Monaco: 'nope'
    [Fact]
    public void MightContainRtl_PlainText_IsFalse()
    {
        Assert.False(TestTextModels.Create("hello world!").MightContainRtl);
    }

    // Monaco: 'yes'
    [Fact]
    public void MightContainRtl_HebrewText_IsTrue()
    {
        Assert.True(TestTextModels.Create(HebrewText).MightContainRtl);
    }

    [Fact]
    public void Constructor_EmptyText_HasOneEmptyLine()
    {
        TextModel model = TestTextModels.Create("");

        Assert.Equal(1, model.LineCount);
        Assert.Equal(new Range(1, 1, 1, 1), model.FullModelRange);
        Assert.Equal("", model.GetValue());
        Assert.Equal(1, model.VersionId);
        Assert.Equal(1, model.AlternativeVersionId);
    }

    [Fact]
    public void GetValue_TextWithBom_KeepsBomOnlyWhenAsked()
    {
        var model = new TextModel("﻿hello\nworld");

        Assert.Equal("hello\nworld", model.GetValue());
        Assert.Equal("﻿hello\nworld", model.GetValue(preserveBom: true));
        Assert.Equal("hello\nworld".Length, model.GetValueLength());
        Assert.Equal("hello\nworld".Length + 1, model.GetValueLength(preserveBom: true));
    }

    [Fact]
    public void Constructor_NoLineBreak_UsesDefaultEol()
    {
        Assert.Equal("\n", new TextModel("abc").Eol);
        Assert.Equal("\r\n", new TextModel("abc", DefaultEndOfLine.CRLF).Eol);
        Assert.Equal(EndOfLineSequence.CRLF, new TextModel("abc", DefaultEndOfLine.CRLF).EndOfLineSequence);
    }

    [Fact]
    public void GetOffsetAt_And_GetPositionAt_RoundTrip_AndClampOffsets()
    {
        TextModel model = TestTextModels.Create("ab\ncd");

        Assert.Equal(4, model.GetOffsetAt(new Position(2, 2)));
        Assert.Equal(new Position(2, 2), model.GetPositionAt(4));
        Assert.Equal(new Position(1, 1), model.GetPositionAt(-5));
        Assert.Equal(new Position(2, 3), model.GetPositionAt(100));
    }

    [Fact]
    public void GetLineContent_IllegalLineNumber_Throws()
    {
        TextModel model = TestTextModels.Create("a\nb");

        Assert.Throws<ArgumentOutOfRangeException>(() => model.GetLineContent(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => model.GetLineContent(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => model.GetLineLength(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => model.GetLineMaxColumn(3));
        Assert.Equal(2, model.GetLineMaxColumn(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => model.GetLineMinColumn(0));
        Assert.Equal(1, model.GetLineMinColumn(1));
    }
}
