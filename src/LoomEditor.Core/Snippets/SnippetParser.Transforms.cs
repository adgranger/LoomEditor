// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetParser.ts (SnippetParser._parseTransform, _parseFormatString)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;

namespace LoomEditor.Core.Snippets;

internal sealed partial class SnippetParser
{
    private const string JavaScriptFlags = "dgimsuvy";

    private bool ParseTransform(TransformableMarker parent)
    {
        // ...<regex>/<format>/<options>}
        var transform = new Transform();
        string regexValue = string.Empty;
        string regexOptions = string.Empty;

        // (1) /regex
        while (true)
        {
            if (Accept(TokenType.Forwardslash))
            {
                break;
            }

            if (TryAcceptText(TokenType.Backslash, out string escaped))
            {
                if (TryAcceptText(TokenType.Forwardslash, out string slash))
                {
                    escaped = slash;
                }

                regexValue += escaped;
                continue;
            }

            if (_token.Type != TokenType.EOF)
            {
                regexValue += AcceptAnyText();
                continue;
            }

            return false;
        }

        // (2) /format
        while (true)
        {
            if (Accept(TokenType.Forwardslash))
            {
                break;
            }

            if (TryAcceptText(TokenType.Backslash, out string escaped))
            {
                if (TryAcceptText(TokenType.Backslash, out string other) || TryAcceptText(TokenType.Forwardslash, out other))
                {
                    escaped = other;
                }

                transform.AppendChild(new TextMarker(escaped));
                continue;
            }

            if (ParseFormatString(transform) || ParseAnything(transform))
            {
                continue;
            }

            return false;
        }

        // (3) /option
        while (true)
        {
            if (Accept(TokenType.CurlyClose))
            {
                break;
            }

            if (_token.Type != TokenType.EOF)
            {
                regexOptions += AcceptAnyText();
                continue;
            }

            return false;
        }

        // Monaco: new RegExp(regexValue, regexOptions) throws on an invalid pattern or flags.
        if (!AreValidFlags(regexOptions))
        {
            return false;
        }

        try
        {
            transform.SetRegexp(regexValue, regexOptions);
        }
        catch (ArgumentException)
        {
            // Invalid regexp.
            return false;
        }

        parent.Transform = transform;
        return true;
    }

    private static bool AreValidFlags(string flags) =>
        flags.All(flag => JavaScriptFlags.Contains(flag, StringComparison.Ordinal))
        && flags.Distinct().Count() == flags.Length
        && !(flags.Contains('u', StringComparison.Ordinal) && flags.Contains('v', StringComparison.Ordinal));

    private bool ParseFormatString(Transform parent)
    {
        SnippetToken token = _token;
        if (!Accept(TokenType.Dollar))
        {
            return false;
        }

        bool complex = Accept(TokenType.CurlyOpen);

        if (!TryAcceptText(TokenType.Int, out string indexText)
            || !int.TryParse(indexText, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
        {
            BackTo(token);
            return false;
        }

        if (!complex)
        {
            // $1
            parent.AppendChild(new FormatString(index));
            return true;
        }

        if (Accept(TokenType.CurlyClose))
        {
            // ${1}
            parent.AppendChild(new FormatString(index));
            return true;
        }

        if (!Accept(TokenType.Colon))
        {
            BackTo(token);
            return false;
        }

        if (Accept(TokenType.Forwardslash))
        {
            // ${1:/upcase}
            if (!TryAcceptText(TokenType.VariableName, out string shorthand) || !Accept(TokenType.CurlyClose))
            {
                BackTo(token);
                return false;
            }

            parent.AppendChild(new FormatString(index, shorthand));
            return true;
        }

        if (Accept(TokenType.Plus))
        {
            // ${1:+<if>}
            string? ifValue = Until(TokenType.CurlyClose);
            if (!string.IsNullOrEmpty(ifValue))
            {
                parent.AppendChild(new FormatString(index, null, ifValue, null));
                return true;
            }
        }
        else if (Accept(TokenType.Dash))
        {
            // ${2:-<else>}
            string? elseValue = Until(TokenType.CurlyClose);
            if (!string.IsNullOrEmpty(elseValue))
            {
                parent.AppendChild(new FormatString(index, null, null, elseValue));
                return true;
            }
        }
        else if (Accept(TokenType.QuestionMark))
        {
            // ${2:?<if>:<else>}
            string? ifValue = Until(TokenType.Colon);
            if (!string.IsNullOrEmpty(ifValue))
            {
                string? elseValue = Until(TokenType.CurlyClose);
                if (!string.IsNullOrEmpty(elseValue))
                {
                    parent.AppendChild(new FormatString(index, null, ifValue, elseValue));
                    return true;
                }
            }
        }
        else
        {
            // ${1:<else>}
            string? elseValue = Until(TokenType.CurlyClose);
            if (!string.IsNullOrEmpty(elseValue))
            {
                parent.AppendChild(new FormatString(index, null, null, elseValue));
                return true;
            }
        }

        BackTo(token);
        return false;
    }
}
