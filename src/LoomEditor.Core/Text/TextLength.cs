// Ported from microsoft/vscode: src/vs/editor/common/core/text/textLength.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;

namespace LoomEditor.Core.Text;

/// <summary>
/// A length expressed as a number of lines and a number of columns (Monaco: <c>TextLength</c>).
/// </summary>
/// <remarks>
/// <para>Only <c>zero</c>, the constructor, <c>isZero</c> and <c>toString</c> are ported; the rest is not needed yet.</para>
/// <para>Threading: immutable value, safe to share between threads.</para>
/// </remarks>
internal readonly record struct TextLength(int LineCount, int ColumnCount)
{
    /// <summary>The zero length.</summary>
    public static readonly TextLength Zero = new(0, 0);

    /// <summary>True when both counts are zero.</summary>
    public bool IsZero => LineCount == 0 && ColumnCount == 0;

    /// <summary><c>addToPosition</c>: the position reached by moving over this length from <paramref name="position"/>.</summary>
    public Position AddToPosition(Position position) =>
        LineCount == 0
            ? new Position(position.LineNumber, position.Column + ColumnCount)
            : new Position(position.LineNumber + LineCount, ColumnCount + 1);

    /// <summary><c>toRange</c>: the range covered by this length starting at (1, 1).</summary>
    public Range ToRange() => new(1, 1, LineCount + 1, ColumnCount + 1);

    /// <summary><c>toLineRange</c>.</summary>
    public LineRange ToLineRange() => new(1, LineCount + 1);

    /// <inheritdoc />
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"{LineCount},{ColumnCount}");
    }
}
