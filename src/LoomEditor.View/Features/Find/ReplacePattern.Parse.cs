// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/replacePattern.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;

namespace LoomEditor.View.Features.Find;

// Parsing a replace string (Monaco: parseReplaceString, ReplacePieceBuilder).
internal sealed partial class ReplacePattern
{
    /// <summary>Parses a replace string (Monaco: <c>parseReplaceString</c>).</summary>
    public static ReplacePattern Parse(string replaceString)
    {
        if (string.IsNullOrEmpty(replaceString))
        {
            return new ReplacePattern(null);
        }

        List<char> caseOps = [];
        var result = new ReplacePieceBuilder(replaceString);

        for (var i = 0; i < replaceString.Length; i++)
        {
            char ch = replaceString[i];

            if (ch == '\\')
            {
                // move to next char
                i++;
                if (i >= replaceString.Length)
                {
                    // string ends with a \
                    break;
                }

                char next = replaceString[i];
                switch (next)
                {
                    case '\\':
                        // \\ => inserts a "\"
                        result.EmitUnchanged(i - 1);
                        result.EmitStatic("\\", i + 1);
                        break;
                    case 'n':
                        // \n => inserts a LF
                        result.EmitUnchanged(i - 1);
                        result.EmitStatic(((char)10).ToString(), i + 1);
                        break;
                    case 't':
                        // \t => inserts a TAB
                        result.EmitUnchanged(i - 1);
                        result.EmitStatic(((char)9).ToString(), i + 1);
                        break;
                    // Case modification of string replacements, patterned after Boost, but only applied
                    // to the replacement text, not subsequent content.
                    case 'u': // \u => upper-cases one character.
                    case 'U': // \U => upper-cases ALL following characters.
                    case 'l': // \l => lower-cases one character.
                    case 'L': // \L => lower-cases ALL following characters.
                        result.EmitUnchanged(i - 1);
                        result.EmitStatic("", i + 1);
                        caseOps.Add(next);
                        break;
                    default:
                        break;
                }

                continue;
            }

            if (ch != '$')
            {
                continue;
            }

            // move to next char
            i++;
            if (i >= replaceString.Length)
            {
                // string ends with a $
                break;
            }

            char nextCh = replaceString[i];

            if (nextCh == '$')
            {
                // $$ => inserts a "$"
                result.EmitUnchanged(i - 1);
                result.EmitStatic("$", i + 1);
                continue;
            }

            if (nextCh is '0' or '&')
            {
                // $& and $0 => inserts the matched substring.
                result.EmitUnchanged(i - 1);
                result.EmitMatchIndex(0, i + 1, caseOps);
                caseOps.Clear();
                continue;
            }

            if (nextCh is >= '1' and <= '9')
            {
                // $n
                var matchIndex = nextCh - '0';

                // peek next char to probe for $nn
                if (i + 1 < replaceString.Length && replaceString[i + 1] is >= '0' and <= '9')
                {
                    // $nn: move to next char
                    i++;
                    matchIndex = (matchIndex * 10) + (replaceString[i] - '0');

                    result.EmitUnchanged(i - 2);
                    result.EmitMatchIndex(matchIndex, i + 1, caseOps);
                    caseOps.Clear();
                    continue;
                }

                result.EmitUnchanged(i - 1);
                result.EmitMatchIndex(matchIndex, i + 1, caseOps);
                caseOps.Clear();
            }
        }

        return result.Finalize();
    }

    private sealed class ReplacePieceBuilder(string source)
    {
        private readonly List<ReplacePiece> _result = [];
        private readonly StringBuilder _currentStaticPiece = new();
        private int _lastCharIndex;

        public void EmitUnchanged(int toCharIndex)
        {
            _currentStaticPiece.Append(source, _lastCharIndex, toCharIndex - _lastCharIndex);
            _lastCharIndex = toCharIndex;
        }

        public void EmitStatic(string value, int toCharIndex)
        {
            _currentStaticPiece.Append(value);
            _lastCharIndex = toCharIndex;
        }

        public void EmitMatchIndex(int index, int toCharIndex, IReadOnlyList<char> caseOps)
        {
            FlushStaticPiece();
            _result.Add(ReplacePiece.CreateCaseOps(index, caseOps));
            _lastCharIndex = toCharIndex;
        }

        public ReplacePattern Finalize()
        {
            EmitUnchanged(source.Length);
            FlushStaticPiece();
            return new ReplacePattern(_result);
        }

        private void FlushStaticPiece()
        {
            if (_currentStaticPiece.Length != 0)
            {
                _result.Add(ReplacePiece.CreateStaticValue(_currentStaticPiece.ToString()));
                _currentStaticPiece.Clear();
            }
        }
    }
}
