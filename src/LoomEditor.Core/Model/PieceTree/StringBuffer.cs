// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

/// <summary>
/// A chunk of text and the offsets at which its lines start.
/// </summary>
/// <remarks>
/// <para>Monaco: <c>StringBuffer</c>.</para>
/// <para>
/// Difference from Monaco: the text is held in a growable <c>char[]</c> instead of a <c>string</c>. Monaco appends
/// to the change buffer with <c>buffer += text</c>, which in .NET copies the whole string every time (quadratic
/// over many edits). <see cref="Append"/> is amortized O(text length).
/// </para>
/// <para>Threading: not thread-safe; callers must serialize access.</para>
/// </remarks>
internal sealed class StringBuffer
{
    private char[] _chars;

    /// <summary>Creates a buffer holding <paramref name="text"/>.</summary>
    /// <param name="text">The initial content.</param>
    /// <param name="lineStarts">Offsets at which the lines of <paramref name="text"/> start.</param>
    public StringBuffer(string text, List<int> lineStarts)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(lineStarts);
        _chars = text.ToCharArray();
        Length = text.Length;
        LineStarts = lineStarts;
    }

    /// <summary>Number of characters in the buffer.</summary>
    public int Length { get; private set; }

    /// <summary>Offsets at which each line starts. Mutable: the change buffer appends to it or replaces it.</summary>
    public List<int> LineStarts { get; set; }

    /// <summary>Gets the character at <paramref name="index"/>.</summary>
    public char this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);
            return _chars[index];
        }
    }

    /// <summary>Appends <paramref name="text"/> at the end of the buffer. Does not touch <see cref="LineStarts"/>.</summary>
    public void Append(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0)
        {
            return;
        }

        int required = Length + text.Length;
        if (required > _chars.Length)
        {
            Array.Resize(ref _chars, Math.Max(required, _chars.Length * 2));
        }

        text.CopyTo(0, _chars, Length, text.Length);
        Length = required;
    }

    /// <summary>Returns <paramref name="length"/> characters starting at <paramref name="start"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The range is not inside the buffer.</exception>
    public string Substring(int start, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        if (start > Length - length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "The range is outside the buffer.");
        }

        return new string(_chars, start, length);
    }

    /// <inheritdoc />
    public override string ToString() => new(_chars, 0, Length);
}
