// Ported from microsoft/vscode: src/vs/base/test/common/strings.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Text;

public class GraphemeTests
{
    // Monaco: 'getGraphemeBreakType'
    [Fact]
    public void GetGraphemeBreakType_Monaco()
    {
        Assert.Equal(GraphemeBreakType.SpacingMark, Strings.GetGraphemeBreakType(0xBC1));
    }

    [Theory]
    [InlineData("e\u0301x", 2)]
    [InlineData("🇫🇷x", 4)]
    [InlineData("👨‍👩‍👧x", 8)]
    [InlineData("辻\U000E0100x", 3)]
    [InlineData("\r\nx", 2)]
    [InlineData("abx", 1)]
    public void NextCharLength_ClusterAtStart_ReturnsClusterLength(string text, int expected)
    {
        Assert.Equal(expected, Strings.NextCharLength(text, 0));
    }

    [Theory]
    [InlineData("e\u0301", 2)]
    [InlineData("🇫🇷", 4)]
    [InlineData("👨‍👩‍👧", 8)]
    [InlineData("辻\U000E0100", 3)]
    [InlineData("\r\n", 2)]
    [InlineData("ab", 1)]
    public void PrevCharLength_ClusterAtEnd_ReturnsClusterLength(string text, int expected)
    {
        Assert.Equal(expected, Strings.PrevCharLength(text, text.Length));
    }

    [Fact]
    public void CharCount_MixedClusters_CountsGraphemes()
    {
        Assert.Equal(3, Strings.CharCount("a👍🏽e\u0301"));
    }

    [Fact]
    public void GetCharContainingOffset_InsideSurrogatePair_ReturnsWholePair()
    {
        Assert.Equal((1, 3), Strings.GetCharContainingOffset("a😀b", 2));
    }

    [Fact]
    public void Raw_GeneratedTable_HasWholeNodesAndRootSentinel()
    {
        var raw = GraphemeBreakData.Raw;
        Assert.Equal(0, raw.Length % 3);
        Assert.Equal(0, raw[0]);
        Assert.Equal(0, raw[1]);
        Assert.Equal(0, raw[2]);
    }
}
