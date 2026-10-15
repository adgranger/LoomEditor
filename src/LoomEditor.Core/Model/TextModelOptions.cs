// Ported from microsoft/vscode: src/vs/editor/common/model.ts (TextModelResolvedOptions)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

/// <summary>The indentation and line-ending options of a <see cref="TextModel"/>.</summary>
/// <remarks>
/// <para>
/// Monaco: <c>TextModelResolvedOptions</c>, without <c>bracketPairColorizationOptions</c>. Monaco's
/// <c>indentSize: 'tabSize'</c> is a <see langword="null"/> indent size here (see <see cref="IndentSizeIsTabSize"/>).
/// </para>
/// <para>Threading: immutable; thread-safe.</para>
/// </remarks>
public sealed class TextModelOptions : IEquatable<TextModelOptions>
{
    /// <summary>Creates options; sizes below 1 are raised to 1, like Monaco.</summary>
    /// <param name="tabSize">The width of a tab stop, in spaces.</param>
    /// <param name="indentSize">The number of spaces of one indentation level, or <see langword="null"/> to follow <paramref name="tabSize"/>.</param>
    /// <param name="insertSpaces">True to insert spaces when Tab is pressed.</param>
    /// <param name="trimAutoWhitespace">True to remove the whitespace that was inserted automatically when it is left unused.</param>
    /// <param name="defaultEol">The EOL to use when the text has no line break.</param>
    public TextModelOptions(
        int tabSize = 4,
        int? indentSize = null,
        bool insertSpaces = true,
        bool trimAutoWhitespace = true,
        DefaultEndOfLine defaultEol = DefaultEndOfLine.LF)
    {
        TabSize = Math.Max(1, tabSize);
        IndentSizeIsTabSize = indentSize is null;
        IndentSize = Math.Max(1, indentSize ?? TabSize);
        InsertSpaces = insertSpaces;
        TrimAutoWhitespace = trimAutoWhitespace;
        DefaultEol = defaultEol;
    }

    /// <summary>Monaco's creation defaults: tab size 4, indent size 4 (not "follow the tab size"), spaces, trim auto whitespace, LF.</summary>
    public static TextModelOptions Default { get; } = new(tabSize: 4, indentSize: 4);

    /// <summary>The width of a tab stop, in spaces; at least 1.</summary>
    public int TabSize { get; }

    /// <summary>The number of spaces of one indentation level; at least 1.</summary>
    public int IndentSize { get; }

    /// <summary>True when <see cref="IndentSize"/> follows <see cref="TabSize"/> (Monaco: <c>originalIndentSize === 'tabSize'</c>).</summary>
    public bool IndentSizeIsTabSize { get; }

    /// <summary>True to insert spaces when Tab is pressed.</summary>
    public bool InsertSpaces { get; }

    /// <summary>True to remove the whitespace that was inserted automatically when it is left unused.</summary>
    public bool TrimAutoWhitespace { get; }

    /// <summary>The EOL to use when the text has no line break.</summary>
    public DefaultEndOfLine DefaultEol { get; }

    /// <summary>True when every option is the same (Monaco: <c>equals</c>).</summary>
    public bool Equals(TextModelOptions? other) =>
        other is not null
        && TabSize == other.TabSize
        && IndentSizeIsTabSize == other.IndentSizeIsTabSize
        && IndentSize == other.IndentSize
        && InsertSpaces == other.InsertSpaces
        && DefaultEol == other.DefaultEol
        && TrimAutoWhitespace == other.TrimAutoWhitespace;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as TextModelOptions);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(TabSize, IndentSizeIsTabSize, IndentSize, InsertSpaces, DefaultEol, TrimAutoWhitespace);

    // Monaco: createChangeEvent
    internal ModelOptionsChangedEventArgs CreateChangeEvent(TextModelOptions newOptions) => new(
        tabSize: TabSize != newOptions.TabSize,
        indentSize: IndentSize != newOptions.IndentSize,
        insertSpaces: InsertSpaces != newOptions.InsertSpaces,
        trimAutoWhitespace: TrimAutoWhitespace != newOptions.TrimAutoWhitespace);
}
