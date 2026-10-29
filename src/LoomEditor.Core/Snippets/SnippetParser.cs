// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetParser.ts (SnippetParser)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Snippets;

/// <summary>Parses the TextMate snippet syntax into a <see cref="TextmateSnippet"/> (Monaco: <c>SnippetParser</c>).</summary>
/// <remarks>
/// A recursive-descent parser over <see cref="SnippetScanner"/> tokens. It never throws: malformed input falls back to text.
/// Behavior differences: a number that does not fit an <see cref="int"/> is not a tabstop index (the construct is text);
/// a transform with flags JavaScript would reject (anything but each of <c>dgimsuvy</c> at most once) is text, as Monaco's
/// <c>new RegExp</c> would throw.
/// Threading: not thread-safe; create one per call or per thread.
/// </remarks>
internal sealed partial class SnippetParser
{
    private SnippetScanner _scanner = new();
    private SnippetToken _token = new(TokenType.EOF, 0, 0);

    /// <summary>Monaco: <c>escape</c>: puts a backslash before <c>\</c>, <c>$</c> and <c>}</c>.</summary>
    public static string Escape(string value) => TextMarker.Escape(value);

    /// <summary>The text a snippet inserts, without tabstops and variables (Monaco: <c>asInsertText</c>).</summary>
    public static string Text(string value) => new SnippetParser().Parse(value).ToString();

    /// <summary>Whether the template uses the <c>CLIPBOARD</c> variable (Monaco: <c>guessNeedsClipboard</c>).</summary>
    public static bool GuessNeedsClipboard(string template)
    {
        ArgumentNullException.ThrowIfNull(template);

        return template.Contains("${CLIPBOARD", StringComparison.Ordinal) || template.Contains("$CLIPBOARD", StringComparison.Ordinal);
    }

    /// <summary>Parses <paramref name="value"/>.</summary>
    /// <param name="value">The snippet template.</param>
    /// <param name="insertFinalTabstop">Appends <c>$0</c> when the snippet has placeholders but no final tabstop.</param>
    /// <param name="enforceFinalTabstop">Appends <c>$0</c> when there is no final tabstop, even without placeholders.</param>
    public TextmateSnippet Parse(string value, bool insertFinalTabstop = false, bool enforceFinalTabstop = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var snippet = new TextmateSnippet();
        ParseFragment(value, snippet);
        EnsureFinalTabstop(snippet, enforceFinalTabstop, insertFinalTabstop);
        return snippet;
    }

    /// <summary>Parses <paramref name="value"/> and appends the markers to <paramref name="snippet"/>.</summary>
    /// <returns>The markers that were appended.</returns>
    public IReadOnlyList<SnippetMarker> ParseFragment(string value, TextmateSnippet snippet)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(snippet);

        int offset = snippet.Children.Count;
        _scanner = new SnippetScanner();
        _scanner.Text(value);
        _token = _scanner.Next();
        while (ParseAny(snippet))
        {
            // Nothing.
        }

        // Fill in values for placeholders: the first placeholder of an index that has a value defines the value for all
        // placeholders with that index.
        var placeholderDefaultValues = new Dictionary<int, IReadOnlyList<SnippetMarker>?>();
        var incompletePlaceholders = new List<Placeholder>();
        snippet.Walk(marker =>
        {
            if (marker is Placeholder placeholder)
            {
                if (placeholder.IsFinalTabstop)
                {
                    placeholderDefaultValues[0] = null;
                }
                else if (!placeholderDefaultValues.ContainsKey(placeholder.Index) && placeholder.Children.Count > 0)
                {
                    placeholderDefaultValues[placeholder.Index] = placeholder.Children;
                }
                else
                {
                    incompletePlaceholders.Add(placeholder);
                }
            }

            return true;
        });

        void FillInIncompletePlaceholder(Placeholder placeholder, HashSet<int> stack)
        {
            if (placeholderDefaultValues.GetValueOrDefault(placeholder.Index) is not { } defaultValues)
            {
                return;
            }

            var clone = new Placeholder(placeholder.Index) { Transform = placeholder.Transform };
            foreach (SnippetMarker child in defaultValues)
            {
                SnippetMarker newChild = child.Clone();
                clone.AppendChild(newChild);

                // "Recurse" on children that are again placeholders.
                if (newChild is Placeholder nested && placeholderDefaultValues.ContainsKey(nested.Index) && !stack.Contains(nested.Index))
                {
                    stack.Add(nested.Index);
                    FillInIncompletePlaceholder(nested, stack);
                    stack.Remove(nested.Index);
                }
            }

            snippet.Replace(placeholder, [clone]);
        }

        var stack = new HashSet<int>();
        foreach (Placeholder placeholder in incompletePlaceholders)
        {
            FillInIncompletePlaceholder(placeholder, stack);
        }

        return snippet.Children.Skip(offset).ToList();
    }

    /// <summary>Appends <c>$0</c> when it is wanted and missing (Monaco: <c>ensureFinalTabstop</c>).</summary>
    public static void EnsureFinalTabstop(TextmateSnippet snippet, bool enforceFinalTabstop, bool insertFinalTabstop)
    {
        ArgumentNullException.ThrowIfNull(snippet);

        if ((enforceFinalTabstop || (insertFinalTabstop && snippet.Placeholders.Count > 0))
            && !snippet.Placeholders.Any(placeholder => placeholder.Index == 0))
        {
            // The snippet uses placeholders but has no final tabstop defined: insert at the end.
            snippet.AppendChild(new Placeholder(0));
        }
    }

    // Monaco: _accept(type). Consumes the token when it has the type.
    private bool Accept(TokenType type)
    {
        if (_token.Type == type)
        {
            _token = _scanner.Next();
            return true;
        }

        return false;
    }

    // Monaco: _accept(type, true). The text of the token, or false when it does not have the type.
    private bool TryAcceptText(TokenType type, out string value)
    {
        if (_token.Type == type)
        {
            value = _scanner.TokenText(_token);
            _token = _scanner.Next();
            return true;
        }

        value = string.Empty;
        return false;
    }

    // Monaco: _accept(undefined, true). Any token; the text is empty at the end.
    private string AcceptAnyText()
    {
        string value = _scanner.TokenText(_token);
        _token = _scanner.Next();
        return value;
    }

    // Monaco: _backTo. Always false, so that callers can `return BackTo(token)`.
    private bool BackTo(SnippetToken token)
    {
        _scanner.Pos = token.Pos + token.Len;
        _token = token;
        return false;
    }

    // Monaco: _until. The text up to `type` with `\$`, `\}` and `\\` unescaped, or null; an empty text is a failure for the callers.
    private string? Until(TokenType type)
    {
        SnippetToken start = _token;
        while (_token.Type != type)
        {
            if (_token.Type == TokenType.EOF)
            {
                return null;
            }

            if (_token.Type == TokenType.Backslash)
            {
                SnippetToken nextToken = _scanner.Next();
                if (nextToken.Type is not (TokenType.Dollar or TokenType.CurlyClose or TokenType.Backslash))
                {
                    return null;
                }
            }

            _token = _scanner.Next();
        }

        string value = Unescape(_scanner.Value[start.Pos.._token.Pos]);
        _token = _scanner.Next();
        return value;
    }

    // Monaco: replace(/\\(\$|}|\\)/g, '$1'), in one pass.
    private static string Unescape(string text)
    {
        var result = new System.Text.StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length && text[i + 1] is '$' or '}' or '\\')
            {
                i++;
            }

            result.Append(text[i]);
        }

        return result.ToString();
    }
}
