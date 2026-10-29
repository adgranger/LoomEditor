// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetParser.ts (Scanner)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>Cuts a snippet string into <see cref="SnippetToken"/>s (Monaco: <c>Scanner</c>).</summary>
/// <remarks>Threading: not thread-safe (a scanner is owned by one parser).</remarks>
internal sealed class SnippetScanner
{
    private static readonly Dictionary<int, TokenType> Table = new()
    {
        ['$'] = TokenType.Dollar,
        [':'] = TokenType.Colon,
        [','] = TokenType.Comma,
        ['{'] = TokenType.CurlyOpen,
        ['}'] = TokenType.CurlyClose,
        ['\\'] = TokenType.Backslash,
        ['/'] = TokenType.Forwardslash,
        ['|'] = TokenType.Pipe,
        ['+'] = TokenType.Plus,
        ['-'] = TokenType.Dash,
        ['?'] = TokenType.QuestionMark,
    };

    private string _value = string.Empty;

    /// <summary>The index of the next character to scan.</summary>
    public int Pos { get; private set; }

    /// <summary>Monaco: <c>isDigitCharacter</c>.</summary>
    public static bool IsDigitCharacter(int ch) => ch is >= '0' and <= '9';

    /// <summary>Monaco: <c>isVariableCharacter</c>.</summary>
    public static bool IsVariableCharacter(int ch) => ch == '_' || ch is >= 'a' and <= 'z' || ch is >= 'A' and <= 'Z';

    /// <summary>Starts scanning <paramref name="value"/> from its first character.</summary>
    public void Text(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _value = value;
        Pos = 0;
    }

    /// <summary>Monaco: <c>tokenText</c>.</summary>
    public string TokenText(SnippetToken token) => _value.Substring(token.Pos, token.Len);

    /// <summary>The next token, or one of type <see cref="TokenType.EOF"/> at the end of the text.</summary>
    public SnippetToken Next()
    {
        if (Pos >= _value.Length)
        {
            return new SnippetToken(TokenType.EOF, Pos, 0);
        }

        int pos = Pos;
        int len = 0;
        int ch = _value[pos];

        // Static types.
        if (Table.TryGetValue(ch, out TokenType type))
        {
            Pos += 1;
            return new SnippetToken(type, pos, 1);
        }

        // Number.
        if (IsDigitCharacter(ch))
        {
            do
            {
                len += 1;
            }
            while (IsDigitCharacter(CharAt(pos + len)));

            Pos += len;
            return new SnippetToken(TokenType.Int, pos, len);
        }

        // Variable name.
        if (IsVariableCharacter(ch))
        {
            do
            {
                len += 1;
                ch = CharAt(pos + len);
            }
            while (IsVariableCharacter(ch) || IsDigitCharacter(ch));

            Pos += len;
            return new SnippetToken(TokenType.VariableName, pos, len);
        }

        // Format: everything up to the next static token, number or variable character.
        do
        {
            len += 1;
            ch = CharAt(pos + len);
        }
        while (ch >= 0 && !Table.ContainsKey(ch) && !IsDigitCharacter(ch) && !IsVariableCharacter(ch));

        Pos += len;
        return new SnippetToken(TokenType.Format, pos, len);
    }

    // Monaco's charCodeAt gives NaN past the end: -1 here, which no test accepts.
    private int CharAt(int index) => index < _value.Length ? _value[index] : -1;
}
