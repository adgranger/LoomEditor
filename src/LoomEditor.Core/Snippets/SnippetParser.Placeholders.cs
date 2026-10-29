// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetParser.ts (SnippetParser._parse and the placeholder, choice and variable parts)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;

namespace LoomEditor.Core.Snippets;

// The order of the alternatives decides ambiguous inputs: it is Monaco's.
internal sealed partial class SnippetParser
{
    // Monaco: _parse.
    private bool ParseAny(SnippetMarker marker) =>
        ParseEscaped(marker)
        || ParseTabstopOrVariableName(marker)
        || ParseComplexPlaceholder(marker)
        || ParseComplexVariable(marker)
        || ParseAnything(marker);

    // \$, \\, \} -> just text.
    private bool ParseEscaped(SnippetMarker marker)
    {
        if (!TryAcceptText(TokenType.Backslash, out string value))
        {
            return false;
        }

        // Saw a backslash, append the escaped token or that backslash.
        if (TryAcceptText(TokenType.Dollar, out string escaped)
            || TryAcceptText(TokenType.CurlyClose, out escaped)
            || TryAcceptText(TokenType.Backslash, out escaped))
        {
            value = escaped;
        }

        marker.AppendChild(new TextMarker(value));
        return true;
    }

    // $foo -> variable, $1 -> tabstop.
    private bool ParseTabstopOrVariableName(SnippetMarker parent)
    {
        SnippetToken token = _token;
        string value = string.Empty;
        bool match = Accept(TokenType.Dollar)
            && (TryAcceptText(TokenType.VariableName, out value) || TryAcceptText(TokenType.Int, out value));
        if (!match)
        {
            return BackTo(token);
        }

        if (value.All(char.IsAsciiDigit))
        {
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
            {
                return BackTo(token);
            }

            parent.AppendChild(new Placeholder(index));
        }
        else
        {
            parent.AppendChild(new Variable(value));
        }

        return true;
    }

    // ${1:<children>}, ${1} -> placeholder.
    private bool ParseComplexPlaceholder(SnippetMarker parent)
    {
        SnippetToken token = _token;
        string indexText = string.Empty;
        bool match = Accept(TokenType.Dollar) && Accept(TokenType.CurlyOpen) && TryAcceptText(TokenType.Int, out indexText);
        if (!match || !int.TryParse(indexText, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
        {
            return BackTo(token);
        }

        var placeholder = new Placeholder(index);

        if (Accept(TokenType.Colon))
        {
            // ${1:<children>}
            while (true)
            {
                // ...} -> done
                if (Accept(TokenType.CurlyClose))
                {
                    parent.AppendChild(placeholder);
                    return true;
                }

                if (ParseAny(placeholder))
                {
                    continue;
                }

                // Fallback.
                parent.AppendChild(new TextMarker("${" + indexText + ":"));
                foreach (SnippetMarker child in placeholder.Children)
                {
                    parent.AppendChild(child);
                }

                return true;
            }
        }

        if (placeholder.Index > 0 && Accept(TokenType.Pipe))
        {
            // ${1|one,two,three|}
            var choice = new Choice();
            while (true)
            {
                if (ParseChoiceElement(choice))
                {
                    if (Accept(TokenType.Comma))
                    {
                        // opt, -> more
                        continue;
                    }

                    if (Accept(TokenType.Pipe))
                    {
                        placeholder.AppendChild(choice);
                        if (Accept(TokenType.CurlyClose))
                        {
                            // ..|} -> done
                            parent.AppendChild(placeholder);
                            return true;
                        }
                    }
                }

                return BackTo(token);
            }
        }

        if (Accept(TokenType.Forwardslash))
        {
            // ${1/<regex>/<format>/<options>}
            if (ParseTransform(placeholder))
            {
                parent.AppendChild(placeholder);
                return true;
            }

            return BackTo(token);
        }

        if (Accept(TokenType.CurlyClose))
        {
            // ${1}
            parent.AppendChild(placeholder);
            return true;
        }

        // ${1 <- missing curly or colon
        return BackTo(token);
    }

    private bool ParseChoiceElement(Choice parent)
    {
        SnippetToken token = _token;
        var values = new List<string>();

        while (true)
        {
            if (_token.Type is TokenType.Comma or TokenType.Pipe)
            {
                break;
            }

            string value;
            if (TryAcceptText(TokenType.Backslash, out value))
            {
                // \, \| or \\
                if (TryAcceptText(TokenType.Comma, out string escaped)
                    || TryAcceptText(TokenType.Pipe, out escaped)
                    || TryAcceptText(TokenType.Backslash, out escaped))
                {
                    value = escaped;
                }
            }
            else
            {
                value = AcceptAnyText();
            }

            if (value.Length == 0)
            {
                // EOF
                return BackTo(token);
            }

            values.Add(value);
        }

        if (values.Count == 0)
        {
            return BackTo(token);
        }

        parent.AppendChild(new TextMarker(string.Concat(values)));
        return true;
    }

    // ${foo:<children>}, ${foo} -> variable.
    private bool ParseComplexVariable(SnippetMarker parent)
    {
        SnippetToken token = _token;
        string name = string.Empty;
        bool match = Accept(TokenType.Dollar) && Accept(TokenType.CurlyOpen) && TryAcceptText(TokenType.VariableName, out name);
        if (!match)
        {
            return BackTo(token);
        }

        var variable = new Variable(name);

        if (Accept(TokenType.Colon))
        {
            // ${foo:<children>}
            while (true)
            {
                // ...} -> done
                if (Accept(TokenType.CurlyClose))
                {
                    parent.AppendChild(variable);
                    return true;
                }

                if (ParseAny(variable))
                {
                    continue;
                }

                // Fallback.
                parent.AppendChild(new TextMarker("${" + name + ":"));
                foreach (SnippetMarker child in variable.Children)
                {
                    parent.AppendChild(child);
                }

                return true;
            }
        }

        if (Accept(TokenType.Forwardslash))
        {
            // ${foo/<regex>/<format>/<options>}
            if (ParseTransform(variable))
            {
                parent.AppendChild(variable);
                return true;
            }

            return BackTo(token);
        }

        if (Accept(TokenType.CurlyClose))
        {
            // ${foo}
            parent.AppendChild(variable);
            return true;
        }

        // ${foo <- missing curly or colon
        return BackTo(token);
    }

    private bool ParseAnything(SnippetMarker marker)
    {
        if (_token.Type != TokenType.EOF)
        {
            marker.AppendChild(new TextMarker(_scanner.TokenText(_token)));
            _token = _scanner.Next();
            return true;
        }

        return false;
    }
}
