// Ported from microsoft/vscode: src/vs/editor/test/common/core/lineTokens.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Tests.Tokens;

public class LineTokensTests
{
    private const string Original = "Hello (32768)world, (65536)this (98304)is (131072)a (163840)lovely (196608)day(229376)";

    private static LineTokens CreateLineTokens(string text, (int StartIndex, int Foreground)[] tokens)
    {
        var binTokens = new uint[tokens.Length << 1];
        for (int i = 0; i < tokens.Length; i++)
        {
            binTokens[i << 1] = (uint)(i + 1 < tokens.Length ? tokens[i + 1].StartIndex : text.Length);
            binTokens[(i << 1) + 1] = (uint)tokens[i].Foreground << MetadataConsts.ForegroundOffset;
        }

        return new LineTokens(binTokens, text, new LanguageIdCodec());
    }

    private static LineTokens CreateTestLineTokens() =>
        CreateLineTokens(
            "Hello world, this is a lovely day",
            [(0, 1), (6, 2), (13, 3), (18, 4), (21, 5), (23, 6), (30, 7)]);

    private static string RenderLineTokens(LineTokens tokens)
    {
        var result = new StringBuilder();
        string str = tokens.GetLineContent();
        int lastOffset = 0;
        for (int i = 0; i < tokens.GetCount(); i++)
        {
            result.Append(str, lastOffset, tokens.GetEndOffset(i) - lastOffset);
            result.Append('(').Append(tokens.GetMetadata(i)).Append(')');
            lastOffset = tokens.GetEndOffset(i);
        }

        return result.ToString();
    }

    private static void AssertViewLineTokens(IViewLineTokens actual, (int EndIndex, int Foreground)[] expected)
    {
        var actualTokens = new (int EndIndex, int Foreground)[actual.GetCount()];
        for (int i = 0; i < actualTokens.Length; i++)
        {
            actualTokens[i] = (actual.GetEndOffset(i), actual.GetForeground(i));
        }

        Assert.Equal(expected, actualTokens);
    }

    // Monaco: 'withInserted 1'
    [Fact]
    public void WithInserted_1()
    {
        LineTokens lineTokens = CreateTestLineTokens();
        Assert.Equal(Original, RenderLineTokens(lineTokens));

        LineTokens lineTokens2 = lineTokens.WithInserted([(0, "1", 0), (6, "2", 0), (9, "3", 0)]);

        Assert.Equal(
            "1(0)Hello (32768)2(0)wor(65536)3(0)ld, (65536)this (98304)is (131072)a (163840)lovely (196608)day(229376)",
            RenderLineTokens(lineTokens2));
    }

    // Monaco: 'withInserted (tokens at the same position)'
    [Fact]
    public void WithInserted_TokensAtTheSamePosition()
    {
        LineTokens lineTokens = CreateTestLineTokens();
        Assert.Equal(Original, RenderLineTokens(lineTokens));

        LineTokens lineTokens2 = lineTokens.WithInserted([(0, "1", 0), (0, "2", 0), (0, "3", 0)]);

        Assert.Equal(
            "1(0)2(0)3(0)Hello (32768)world, (65536)this (98304)is (131072)a (163840)lovely (196608)day(229376)",
            RenderLineTokens(lineTokens2));
    }

    // Monaco: 'withInserted (tokens at the end)'
    [Fact]
    public void WithInserted_TokensAtTheEnd()
    {
        LineTokens lineTokens = CreateTestLineTokens();
        Assert.Equal(Original, RenderLineTokens(lineTokens));
        int length = "Hello world, this is a lovely day".Length;

        LineTokens lineTokens2 = lineTokens.WithInserted([(length - 1, "1", 0), (length, "2", 0)]);

        Assert.Equal(
            "Hello (32768)world, (65536)this (98304)is (131072)a (163840)lovely (196608)da(229376)1(0)y(229376)2(0)",
            RenderLineTokens(lineTokens2));
    }

    // Monaco: 'basics'
    [Fact]
    public void Basics()
    {
        LineTokens lineTokens = CreateTestLineTokens();

        Assert.Equal("Hello world, this is a lovely day", lineTokens.GetLineContent());
        Assert.Equal(33, lineTokens.GetLineContent().Length);
        Assert.Equal(7, lineTokens.GetCount());

        int[] starts = [0, 6, 13, 18, 21, 23, 30];
        int[] ends = [6, 13, 18, 21, 23, 30, 33];
        for (int i = 0; i < 7; i++)
        {
            Assert.Equal(starts[i], lineTokens.GetStartOffset(i));
            Assert.Equal(ends[i], lineTokens.GetEndOffset(i));
        }
    }

    // Monaco: 'findToken'
    [Fact]
    public void FindToken()
    {
        LineTokens lineTokens = CreateTestLineTokens();

        int[] expected =
        [
            0, 0, 0, 0, 0, 0, // 0..5
            1, 1, 1, 1, 1, 1, 1, // 6..12
            2, 2, 2, 2, 2, // 13..17
            3, 3, 3, // 18..20
            4, 4, // 21..22
            5, 5, 5, 5, 5, 5, 5, // 23..29
            6, 6, 6, 6, 6, // 30..34
        ];
        for (int offset = 0; offset < expected.Length; offset++)
        {
            Assert.Equal(expected[offset], lineTokens.FindTokenIndexAtOffset(offset));
        }
    }

    // Monaco: 'inflate'
    [Fact]
    public void Inflate()
    {
        AssertViewLineTokens(
            CreateTestLineTokens().Inflate(),
            [(6, 1), (13, 2), (18, 3), (21, 4), (23, 5), (30, 6), (33, 7)]);
    }

    // Monaco: 'sliceAndInflate'
    [Fact]
    public void SliceAndInflate()
    {
        LineTokens lineTokens = CreateTestLineTokens();

        AssertViewLineTokens(
            lineTokens.SliceAndInflate(0, 33, 0),
            [(6, 1), (13, 2), (18, 3), (21, 4), (23, 5), (30, 6), (33, 7)]);
        AssertViewLineTokens(
            lineTokens.SliceAndInflate(0, 32, 0),
            [(6, 1), (13, 2), (18, 3), (21, 4), (23, 5), (30, 6), (32, 7)]);
        AssertViewLineTokens(
            lineTokens.SliceAndInflate(0, 30, 0),
            [(6, 1), (13, 2), (18, 3), (21, 4), (23, 5), (30, 6)]);
        AssertViewLineTokens(
            lineTokens.SliceAndInflate(0, 30, 1),
            [(7, 1), (14, 2), (19, 3), (22, 4), (24, 5), (31, 6)]);
        AssertViewLineTokens(lineTokens.SliceAndInflate(6, 18, 0), [(7, 2), (12, 3)]);
        AssertViewLineTokens(lineTokens.SliceAndInflate(7, 18, 0), [(6, 2), (11, 3)]);
        AssertViewLineTokens(lineTokens.SliceAndInflate(6, 17, 0), [(7, 2), (11, 3)]);
        AssertViewLineTokens(lineTokens.SliceAndInflate(6, 19, 0), [(7, 2), (12, 3), (13, 4)]);
    }
}
