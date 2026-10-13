// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.
// Monaco tests these functions only indirectly: all tests here are LoomEditor additions.

using LoomEditor.Core.Model.PieceTree;

namespace LoomEditor.Core.Tests.Model.PieceTree;

public class LineStartsTests
{
    [Fact]
    public void CreateFast_EmptyText_ReturnsSingleZero()
    {
        Assert.Equal([0], LineStarts.CreateFast(""));
    }

    [Fact]
    public void CreateFast_MixedLineBreaks_ReturnsStartOfEachLine()
    {
        Assert.Equal([0, 2, 5, 7], LineStarts.CreateFast("a\nb\r\nc\rd"));
    }

    [Fact]
    public void CreateFast_CrLfIsOneBreakAndLfCrIsTwo()
    {
        Assert.Equal([0, 3], LineStarts.CreateFast("a\r\nb"));
        Assert.Equal([0, 2, 3], LineStarts.CreateFast("a\n\rb"));
    }

    [Fact]
    public void CreateFast_TextEndingWithLineBreak_LastStartEqualsLength()
    {
        const string Text = "a\r\n";
        var starts = LineStarts.CreateFast(Text);
        Assert.Equal(Text.Length, starts[^1]);
    }

    [Fact]
    public void CreateFast_ReturnsNewMutableList()
    {
        var a = LineStarts.CreateFast("a\nb");
        a.Add(100);
        Assert.Equal(2, LineStarts.CreateFast("a\nb").Count);
    }

    [Fact]
    public void Create_CountsEachKindOfLineBreak()
    {
        var result = LineStarts.Create("a\r\nb\nc\rd\r\n");
        Assert.Equal(2, result.CarriageReturnLineFeedCount);
        Assert.Equal(1, result.LineFeedCount);
        Assert.Equal(1, result.CarriageReturnCount);
    }

    [Fact]
    public void Create_LfCr_CountsOneLfAndOneCr()
    {
        var result = LineStarts.Create("a\n\rb");
        Assert.Equal([0, 2, 3], result.Starts);
        Assert.Equal(1, result.LineFeedCount);
        Assert.Equal(1, result.CarriageReturnCount);
        Assert.Equal(0, result.CarriageReturnLineFeedCount);
    }

    [Theory]
    [InlineData("abc\tdef", true)]
    [InlineData("é", false)]
    [InlineData("\u0001", false)]
    [InlineData("a\r\nb\nc\rd", true)]
    [InlineData("", true)]
    public void Create_IsBasicAscii_FollowsMonacoRule(string text, bool expected)
    {
        Assert.Equal(expected, LineStarts.Create(text).IsBasicAscii);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("a\nb\r\nc\rd")]
    [InlineData("\r\n\r\n")]
    [InlineData("\n\r")]
    public void Create_Starts_EqualCreateFast(string text)
    {
        Assert.Equal(LineStarts.CreateFast(text), LineStarts.Create(text).Starts);
    }
}
