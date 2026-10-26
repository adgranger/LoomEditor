using LoomEditor.Lsp.Editor;

namespace LoomEditor.Lsp.Tests.Editor;

public class LspSnippetTests
{
    [Fact]
    public void Plain_Unchanged() => Assert.Equal("hello world", LspSnippet.ToPlainText("hello world"));

    [Fact]
    public void FinalTabStop_Removed() => Assert.Equal("ab", LspSnippet.ToPlainText("a$0b"));

    [Fact]
    public void TabStops_Removed() => Assert.Equal("ab", LspSnippet.ToPlainText("$1a${2}b"));

    [Fact]
    public void Placeholder_Default_Kept() => Assert.Equal("x value y", LspSnippet.ToPlainText("x ${1:value} y"));

    [Fact]
    public void Placeholder_Nested() => Assert.Equal("ab", LspSnippet.ToPlainText("${1:a${2:b}}"));

    [Fact]
    public void Choice_FirstOption() => Assert.Equal("one", LspSnippet.ToPlainText("${1|one,two,three|}"));

    [Fact]
    public void Variable_Removed() => Assert.Equal("ab", LspSnippet.ToPlainText("a$TM_FILENAME${CLIPBOARD}b"));

    [Fact]
    public void VariableWithDefault_Default() => Assert.Equal("dflt", LspSnippet.ToPlainText("${NAME:dflt}"));

    [Fact]
    public void Escapes() => Assert.Equal("$1 } \\ \\n", LspSnippet.ToPlainText("\\$1 \\} \\\\ \\n"));

    [Fact]
    public void UnterminatedBrace_Literal() => Assert.Equal("${1:abc", LspSnippet.ToPlainText("${1:abc"));

    [Fact]
    public void LoneDollar_Literal() => Assert.Equal("a$ b$", LspSnippet.ToPlainText("a$ b$"));

    [Fact]
    public void Empty_Empty() => Assert.Equal(string.Empty, LspSnippet.ToPlainText(string.Empty));

    [Fact]
    public void RealisticMethodSnippet()
        => Assert.Equal("Console.WriteLine(value)", LspSnippet.ToPlainText("Console.WriteLine(${1:value})$0"));
}
