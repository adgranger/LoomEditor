// Ported from microsoft/vscode: src/vs/base/common/strings.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

/// <summary>Walks a string code point by code point, in both directions (Monaco: <c>CodePointIterator</c>).</summary>
/// <remarks>Threading: mutable, not thread-safe; use from one thread.</remarks>
internal sealed class CodePointIterator
{
    private readonly string _str;
    private readonly int _len;

    public CodePointIterator(string str, int offset = 0)
    {
        _str = str;
        _len = str.Length;
        Offset = offset;
    }

    /// <summary>Current UTF-16 offset.</summary>
    public int Offset { get; private set; }

    /// <summary>Moves the iterator to <paramref name="offset"/>.</summary>
    public void SetOffset(int offset) => Offset = offset;

    /// <summary>Returns the code point before the offset and moves back over it.</summary>
    public int PrevCodePoint()
    {
        var codePoint = Strings.GetPrevCodePoint(_str, Offset);
        Offset -= Strings.CodePointLength(codePoint);
        return codePoint;
    }

    /// <summary>Returns the code point at the offset and moves forward over it.</summary>
    public int NextCodePoint()
    {
        var codePoint = Strings.GetNextCodePoint(_str, _len, Offset);
        Offset += Strings.CodePointLength(codePoint);
        return codePoint;
    }

    /// <summary>True when the offset is at or past the end of the string.</summary>
    public bool Eol() => Offset >= _len;
}
