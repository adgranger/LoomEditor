// Not ported from Monaco: LoomEditor reference implementation, trivially correct, used by the randomized tests.

using System.Text;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

/// <summary>A <see cref="StringBuilder"/> with the same text API as the piece tree, implemented in the most obvious way.</summary>
internal sealed class NaiveTextBuffer
{
    private readonly StringBuilder _text;
    private List<(int Start, int Length)>? _lines;

    public NaiveTextBuffer(string initial = "")
    {
        _text = new StringBuilder(initial);
    }

    public int Length => _text.Length;

    public string GetText() => _text.ToString();

    public void Insert(int offset, string value)
    {
        _text.Insert(offset, value);
        _lines = null;
    }

    public void Delete(int offset, int count)
    {
        _text.Remove(offset, count);
        _lines = null;
    }

    /// <summary>The lines without their line breaks; a line break is <c>\r\n</c>, <c>\r</c> or <c>\n</c>.</summary>
    public List<string> GetLines()
    {
        var lines = new List<string>();
        foreach ((int start, int length) in Lines())
        {
            lines.Add(_text.ToString(start, length));
        }

        return lines;
    }

    /// <summary>Offset of the 1-based <paramref name="line"/> / <paramref name="column"/>.</summary>
    public int GetOffsetAt(int line, int column)
    {
        int lineStart = 0;
        int current = 1;
        foreach ((int start, _) in Lines())
        {
            if (current == line)
            {
                lineStart = start;
                break;
            }

            current++;
        }

        return lineStart + column - 1;
    }

    /// <summary>Position of <paramref name="offset"/>: the last line starting at or before it.</summary>
    public Position GetPositionAt(int offset)
    {
        int line = 0;
        int lineStart = 0;
        foreach ((int start, _) in Lines())
        {
            if (start > offset)
            {
                break;
            }

            lineStart = start;
            line++;
        }

        return new Position(line, offset - lineStart + 1);
    }

    private List<(int Start, int Length)> Lines() => _lines ??= EnumerateLines().ToList();

    private IEnumerable<(int Start, int Length)> EnumerateLines()
    {
        int start = 0;
        for (int i = 0; i < _text.Length; i++)
        {
            char c = _text[i];
            if (c != '\r' && c != '\n')
            {
                continue;
            }

            int eol = c == '\r' && i + 1 < _text.Length && _text[i + 1] == '\n' ? 2 : 1;
            yield return (start, i - start);
            i += eol - 1;
            start = i + 1;
        }

        yield return (start, _text.Length - start);
    }
}
