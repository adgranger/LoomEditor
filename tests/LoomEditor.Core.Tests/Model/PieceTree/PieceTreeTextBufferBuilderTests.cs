// Ported from microsoft/vscode: src/vs/editor/test/common/model/linesTextBuffer/linesTextBufferBuilder.test.ts (suite 'ModelBuilder')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.
// The tests marked "New" are LoomEditor additions.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class PieceTreeTextBufferBuilderTests
{
    private static readonly string Bom = ((char)0xFEFF).ToString();

    // Monaco: testTextBufferFactory
    private static void AssertFactory(string text, string eol, bool mightContainNonBasicAscii, bool mightContainRtl)
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.CreateFactory(text).Create(DefaultEndOfLine.LF);

        Assert.Equal(mightContainNonBasicAscii, buffer.MightContainNonBasicAscii());
        Assert.Equal(mightContainRtl, buffer.MightContainRtl());
        Assert.Equal(eol, buffer.GetEol());
    }

    // Monaco: 't1'
    [Fact]
    public void T1() => AssertFactory("", "\n", false, false);

    // Monaco: 't2'
    [Fact]
    public void T2() => AssertFactory("Hello world", "\n", false, false);

    // Monaco: 't3'
    [Fact]
    public void T3() => AssertFactory("Hello world\nHow are you?", "\n", false, false);

    // Monaco: 't4'
    [Fact]
    public void T4() => AssertFactory("Hello world\nHow are you?\nIs everything good today?\nDo you enjoy the weather?", "\n", false, false);

    // Monaco: 'carriage return detection (1 \r\n 2 \n)'
    [Fact]
    public void CarriageReturnDetection_OneCrlfTwoLf_UsesLf() =>
        AssertFactory("Hello world\r\nHow are you?\nIs everything good today?\nDo you enjoy the weather?", "\n", false, false);

    // Monaco: 'carriage return detection (2 \r\n 1 \n)'
    [Fact]
    public void CarriageReturnDetection_TwoCrlfOneLf_UsesCrlf() =>
        AssertFactory("Hello world\r\nHow are you?\r\nIs everything good today?\nDo you enjoy the weather?", "\r\n", false, false);

    // Monaco: 'carriage return detection (3 \r\n 0 \n)'
    [Fact]
    public void CarriageReturnDetection_ThreeCrlf_UsesCrlf() =>
        AssertFactory("Hello world\r\nHow are you?\r\nIs everything good today?\r\nDo you enjoy the weather?", "\r\n", false, false);

    // Monaco: 'BOM handling'
    [Fact]
    public void BomHandling() => AssertFactory(Bom + "Hello world!", "\n", false, false);

    // Monaco: 'RTL handling 2'
    [Fact]
    public void RtlHandling2() => AssertFactory("Hello world!זוהי עובדה מבוססת שדעתו", "\n", true, true);

    // Monaco: 'RTL handling 3'
    [Fact]
    public void RtlHandling3() => AssertFactory("Hello world!זוהי \nעובדה מבוססת שדעתו", "\n", true, true);

    // Monaco: 'ASCII handling 1'
    [Fact]
    public void AsciiHandling1() => AssertFactory("Hello world!!\nHow do you do?", "\n", false, false);

    // Monaco: 'ASCII handling 2'
    [Fact]
    public void AsciiHandling2() => AssertFactory("Hello world!!\nHow do you do?Züricha📚📚b", "\n", true, false);

    // New
    [Fact]
    public void ContainsRtl_CharacterOutsideTheBmp_IsDetected() =>
        AssertFactory("a\U00010800b", "\n", true, true);

    // New
    [Fact]
    public void ContainsRtl_NonRtlCharacterOutsideTheBmp_IsNotDetected() =>
        AssertFactory("a📚b", "\n", true, false);

    // New
    [Fact]
    public void UnusualLineTerminators_AreDetected()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.CreateFactory("a" + (char)0x2028 + "b").Create(DefaultEndOfLine.LF);

        Assert.True(buffer.MightContainUnusualLineTerminators());
        Assert.True(buffer.MightContainNonBasicAscii());
    }

    // New
    [Fact]
    public void AcceptChunk_CrThenLfInNextChunk_IsOneCrlf()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["ab\r", "\ncd"]);

        Assert.Equal("\r\n", buffer.GetEol());
        Assert.Equal(2, buffer.GetPieceTree().GetLineCount());
        Assert.Equal(["ab", "cd"], buffer.GetPieceTree().GetLinesContent());
        Assert.Equal("ab\r\ncd", buffer.GetPieceTree().GetLinesRawContent());
    }

    // New
    [Fact]
    public void AcceptChunk_TrailingCrAtTheEnd_IsKeptAsALineBreak()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["ab\r"], normalizeEol: false);

        Assert.Equal(2, buffer.GetPieceTree().GetLineCount());
        Assert.Equal("ab\r", buffer.GetPieceTree().GetLinesRawContent());
    }

    // New
    [Fact]
    public void AcceptChunk_HighSurrogateThenLowSurrogateInNextChunk_PairIsNeverSplit()
    {
        const string High = "\uD83D";
        const string Low = "\uDCDA";
        var builder = new PieceTreeTextBufferBuilder();
        builder.AcceptChunk("a" + High);
        builder.AcceptChunk(Low + "b");
        PieceTreeTextBuffer buffer = builder.Finish().Create(DefaultEndOfLine.LF);

        Assert.Equal("a" + High + Low + "b", buffer.GetPieceTree().GetLinesRawContent());
        Assert.True(buffer.MightContainNonBasicAscii());
        Assert.Equal(2, CountPieces(buffer.GetPieceTree()));
    }

    // New
    [Fact]
    public void AcceptChunk_HighSurrogateAtTheEnd_IsKept()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["a\uD83D"]);

        Assert.Equal("a\uD83D", buffer.GetPieceTree().GetLinesRawContent());
    }

    // New
    [Fact]
    public void AcceptChunk_BomInFirstChunk_IsRemovedAndReported()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create([Bom + "abc"]);

        Assert.Equal(Bom, buffer.GetBom());
        Assert.Equal("abc", buffer.GetPieceTree().GetLinesRawContent());
    }

    // New
    [Fact]
    public void AcceptChunk_BomInLaterChunk_IsKeptAsText()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["abc", Bom + "def"]);

        Assert.Equal("", buffer.GetBom());
        Assert.Equal("abc" + Bom + "def", buffer.GetPieceTree().GetLinesRawContent());
    }

    // New
    [Fact]
    public void AcceptChunk_BomOnlyFirstChunk_NextChunkStillCountsAsFirst()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create([Bom, Bom + "x"]);

        Assert.Equal(Bom, buffer.GetBom());
        Assert.Equal("x", buffer.GetPieceTree().GetLinesRawContent());
    }

    // New
    [Fact]
    public void Create_NoLineBreak_UsesDefaultEol()
    {
        PieceTreeTextBufferFactory factory = TestTextBuffers.CreateFactory("abc");

        Assert.Equal("\r\n", factory.Create(DefaultEndOfLine.CRLF).GetEol());
    }

    // New
    [Fact]
    public void Create_EmptyBuilder_UsesDefaultEolAndHasOneLine()
    {
        PieceTreeTextBuffer buffer = new PieceTreeTextBufferBuilder().Finish().Create(DefaultEndOfLine.CRLF);

        Assert.Equal("\r\n", buffer.GetEol());
        Assert.Equal(1, buffer.GetPieceTree().GetLineCount());
        Assert.Equal(0, buffer.GetPieceTree().GetLength());
    }

    // New
    [Fact]
    public void Create_MostlyCrlf_UsesCrlfAndNormalizesText()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["a\r\nb\r\nc\nd\re"]);

        Assert.Equal("\r\n", buffer.GetEol());
        Assert.Equal("a\r\nb\r\nc\r\nd\r\ne", buffer.GetPieceTree().GetLinesRawContent());
    }

    // New
    [Fact]
    public void Create_MostlyLf_UsesLfAndNormalizesText()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["a\nb\nc\r\nd\re"]);

        Assert.Equal("\n", buffer.GetEol());
        Assert.Equal("a\nb\nc\nd\ne", buffer.GetPieceTree().GetLinesRawContent());
    }

    // New
    [Fact]
    public void Create_ExactlyHalfCrlf_UsesLf()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["a\r\nb\nc"], normalizeEol: false);

        Assert.Equal("\n", buffer.GetEol());
    }

    // New
    [Fact]
    public void Create_WithoutNormalizeEol_KeepsMixedLineBreaks()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["a\r\nb\r\nc\nd"], normalizeEol: false);

        Assert.Equal("\r\n", buffer.GetEol());
        Assert.Equal("a\r\nb\r\nc\nd", buffer.GetPieceTree().GetLinesRawContent());
    }

    // New
    [Fact]
    public void Create_NormalizesEveryChunk()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["a\nb\n", "c\r\nd\n", "e"]);

        Assert.Equal("\n", buffer.GetEol());
        Assert.Equal("a\nb\nc\nd\ne", buffer.GetPieceTree().GetLinesRawContent());
        Assert.Equal(5, buffer.GetPieceTree().GetLineCount());
    }

    // New
    [Theory]
    [InlineData("hello\nworld", 100, "hello")]
    [InlineData("hello\r\nworld", 100, "hello")]
    [InlineData("hello\rworld", 100, "hello")]
    [InlineData("hello world", 5, "hello")]
    [InlineData("hello\nworld", 0, "")]
    [InlineData("", 10, "")]
    public void GetFirstLineText_ReturnsFirstLineLimited(string text, int limit, string expected)
    {
        PieceTreeTextBufferFactory factory = TestTextBuffers.CreateFactory(text);

        Assert.Equal(expected, factory.GetFirstLineText(limit));
    }

    // New
    [Fact]
    public void EndOfLineEnums_HaveMonacoValues()
    {
        Assert.Equal(0, (int)EndOfLinePreference.TextDefined);
        Assert.Equal(1, (int)EndOfLinePreference.LF);
        Assert.Equal(2, (int)EndOfLinePreference.CRLF);
        Assert.Equal(1, (int)DefaultEndOfLine.LF);
        Assert.Equal(2, (int)DefaultEndOfLine.CRLF);
        Assert.Equal(0, (int)EndOfLineSequence.LF);
        Assert.Equal(1, (int)EndOfLineSequence.CRLF);
    }

    // New
    [Theory]
    [InlineData("", true)]
    [InlineData("abc\t\r\n~ ", true)]
    [InlineData("\u007F", false)]
    [InlineData("\u001F", false)]
    [InlineData("é", false)]
    public void IsBasicAscii_ClassifiesStrings(string text, bool expected) =>
        Assert.Equal(expected, StringClassification.IsBasicAscii(text));

    private static int CountPieces(PieceTreeBase tree)
    {
        int count = 0;
        tree.Iterate(tree.Tree.Root, node =>
        {
            if (!ReferenceEquals(node, tree.Tree.Sentinel))
            {
                count++;
            }

            return true;
        });
        return count;
    }
}
