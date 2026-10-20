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

    /// <inheritdoc />
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"{LineCount},{ColumnCount}");
    }
}
