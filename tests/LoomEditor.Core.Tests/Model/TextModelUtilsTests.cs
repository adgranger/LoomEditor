using LoomEditor.Core.Model;

namespace LoomEditor.Core.Tests.Model;

// Monaco has no test for computeIndentLevel on its own (the folding tests cover it): all tests are new.
public class TextModelUtilsTests
{
    [Fact]
    public void ComputeIndentLevel_TabsAndSpaces_CountsVisibleColumns()
    {
        Assert.Equal(0, TextModelUtils.ComputeIndentLevel("a", 4));
        Assert.Equal(2, TextModelUtils.ComputeIndentLevel("  a", 4));
        Assert.Equal(4, TextModelUtils.ComputeIndentLevel("\ta", 4));
        Assert.Equal(4, TextModelUtils.ComputeIndentLevel("  \ta", 4));
        Assert.Equal(8, TextModelUtils.ComputeIndentLevel("\t\ta", 4));
        Assert.Equal(2, TextModelUtils.ComputeIndentLevel("\ta", 2));
    }

    [Fact]
    public void ComputeIndentLevel_WhitespaceOnly_ReturnsMinusOne()
    {
        Assert.Equal(-1, TextModelUtils.ComputeIndentLevel("", 4));
        Assert.Equal(-1, TextModelUtils.ComputeIndentLevel(" \t ", 4));
    }
}