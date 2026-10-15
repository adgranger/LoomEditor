using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Text;

public class IndentationTests
{
    [Theory]
    [InlineData("\t  x", 4, true, "      x")]
    [InlineData("        x", 4, false, "\t\tx")]
    [InlineData("x", 4, true, "x")]
    [InlineData("  \t  ", 4, false, "\t  ")]
    [InlineData("", 4, false, "")]
    public void NormalizeIndentation_Scenario_ExpectedResult(string input, int indentSize, bool insertSpaces, string expected)
    {
        Assert.Equal(expected, Indentation.NormalizeIndentation(input, indentSize, insertSpaces));
    }
}
