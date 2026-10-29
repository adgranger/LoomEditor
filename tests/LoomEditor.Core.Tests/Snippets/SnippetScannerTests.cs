using LoomEditor.Core.Snippets;

namespace LoomEditor.Core.Tests.Snippets;

public class SnippetScannerTests
{
    private static void AssertTokens(SnippetScanner scanner, string text, params TokenType[] expected)
    {
        scanner.Text(text);
        foreach (TokenType type in expected)
        {
            Assert.Equal(type, scanner.Next().Type);
        }
    }

    // Monaco: 'Snippet parser - Scanner' > 'Scanner'
    [Fact]
    public void Scanner()
    {
        var scanner = new SnippetScanner();
        Assert.Equal(TokenType.EOF, scanner.Next().Type);

        AssertTokens(scanner, "abc", TokenType.VariableName, TokenType.EOF);
        AssertTokens(scanner, "{{abc}}", TokenType.CurlyOpen, TokenType.CurlyOpen, TokenType.VariableName, TokenType.CurlyClose, TokenType.CurlyClose, TokenType.EOF);
        AssertTokens(scanner, "abc() ", TokenType.VariableName, TokenType.Format, TokenType.EOF);
        AssertTokens(scanner, "abc 123", TokenType.VariableName, TokenType.Format, TokenType.Int, TokenType.EOF);
        AssertTokens(scanner, "$foo", TokenType.Dollar, TokenType.VariableName, TokenType.EOF);
        AssertTokens(scanner, "$foo_bar", TokenType.Dollar, TokenType.VariableName, TokenType.EOF);
        AssertTokens(scanner, "$foo-bar", TokenType.Dollar, TokenType.VariableName, TokenType.Dash, TokenType.VariableName, TokenType.EOF);
        AssertTokens(scanner, "${foo}", TokenType.Dollar, TokenType.CurlyOpen, TokenType.VariableName, TokenType.CurlyClose, TokenType.EOF);
        AssertTokens(
            scanner, "${1223:foo}",
            TokenType.Dollar, TokenType.CurlyOpen, TokenType.Int, TokenType.Colon, TokenType.VariableName, TokenType.CurlyClose, TokenType.EOF);
        AssertTokens(scanner, "\\${}", TokenType.Backslash, TokenType.Dollar, TokenType.CurlyOpen, TokenType.CurlyClose);
        AssertTokens(
            scanner, "${foo/regex/format/option}",
            TokenType.Dollar, TokenType.CurlyOpen, TokenType.VariableName, TokenType.Forwardslash, TokenType.VariableName, TokenType.Forwardslash,
            TokenType.VariableName, TokenType.Forwardslash, TokenType.VariableName, TokenType.CurlyClose, TokenType.EOF);
    }

    [Fact]
    public void Next_ReportsPositionsAndLengths()
    {
        var scanner = new SnippetScanner();
        scanner.Text("${12:ab c}");

        Assert.Equal(new SnippetToken(TokenType.Dollar, 0, 1), scanner.Next());
        Assert.Equal(new SnippetToken(TokenType.CurlyOpen, 1, 1), scanner.Next());
        SnippetToken number = scanner.Next();
        Assert.Equal(new SnippetToken(TokenType.Int, 2, 2), number);
        Assert.Equal("12", scanner.TokenText(number));
        Assert.Equal(new SnippetToken(TokenType.Colon, 4, 1), scanner.Next());
        Assert.Equal(new SnippetToken(TokenType.VariableName, 5, 2), scanner.Next());
        Assert.Equal(new SnippetToken(TokenType.Format, 7, 1), scanner.Next());
        Assert.Equal(new SnippetToken(TokenType.VariableName, 8, 1), scanner.Next());
        Assert.Equal(new SnippetToken(TokenType.CurlyClose, 9, 1), scanner.Next());
        Assert.Equal(new SnippetToken(TokenType.EOF, 10, 0), scanner.Next());
    }
}
