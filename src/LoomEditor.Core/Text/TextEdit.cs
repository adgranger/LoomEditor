// Ported from microsoft/vscode: src/vs/editor/common/core/editOperation.ts (ISingleEditOperation, EditOperation)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

/// <summary>
/// One text change: "replace the text in <see cref="Range"/> with <see cref="Text"/>".
/// Inserting and deleting are special cases (empty range, or empty text).
/// </summary>
/// <remarks>
/// <para>
/// Use the factory methods <see cref="Insert"/>, <see cref="Delete"/> and <see cref="Replace"/>:
/// they say what the edit does better than the constructor.
/// </para>
/// <para>
/// <see cref="Text"/> may contain any end-of-line sequence; the text model normalizes it when the edit is applied.
/// </para>
/// <para>Thread safety: immutable value, safe to share between threads.</para>
/// </remarks>
public readonly record struct TextEdit
{
    /// <summary>Creates an edit that replaces <paramref name="range"/> with <paramref name="text"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    public TextEdit(Range range, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Range = range;
        Text = text;
    }

    /// <summary>The range of text to replace. An empty range means "insert at this position".</summary>
    public Range Range { get; }

    /// <summary>The new text. An empty string means "delete the range".</summary>
    public string Text { get; }

    /// <summary>True if this edit only inserts text (the range is empty and the text is not).</summary>
    public bool IsInsert => Range.IsEmpty && Text.Length > 0;

    /// <summary>True if this edit only deletes text (the range is not empty and the text is).</summary>
    public bool IsDelete => !Range.IsEmpty && Text.Length == 0;

    /// <summary>True if this edit changes nothing (empty range and empty text).</summary>
    public bool IsNoOp => Range.IsEmpty && Text.Length == 0;

    /// <summary>Creates an edit that inserts <paramref name="text"/> at <paramref name="position"/>.</summary>
    /// <remarks>Monaco: <c>EditOperation.insert</c>.</remarks>
    public static TextEdit Insert(Position position, string text) => new(Range.Collapsed(position), text);

    /// <summary>Creates an edit that deletes the text in <paramref name="range"/>.</summary>
    /// <remarks>Monaco: <c>EditOperation.delete</c>.</remarks>
    public static TextEdit Delete(Range range) => new(range, string.Empty);

    /// <summary>Creates an edit that replaces the text in <paramref name="range"/> with <paramref name="text"/>.</summary>
    /// <remarks>Monaco: <c>EditOperation.replace</c>.</remarks>
    public static TextEdit Replace(Range range, string text) => new(range, text);

    /// <summary>Formats the edit for debugging, e.g. <c>[1,1 -> 1,4] => "abc"</c>.</summary>
    public override string ToString() => $"{Range} => \"{Text}\"";
}
