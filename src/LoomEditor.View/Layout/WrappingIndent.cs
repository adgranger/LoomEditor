// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>How wrapped continuation lines are indented (Monaco: <c>WrappingIndent</c>).</summary>
internal enum WrappingIndent
{
    /// <summary>No indentation: wrapped lines begin at column 1.</summary>
    None = 0,

    /// <summary>Same as the parent line.</summary>
    Same = 1,

    /// <summary>The parent line's indentation plus one.</summary>
    Indent = 2,

    /// <summary>The parent line's indentation plus two.</summary>
    DeepIndent = 3,
}

/// <summary>Parsing of <see cref="WrappingIndent"/> (Monaco: <c>WrappingIndentOption.validate</c>).</summary>
/// <remarks>Threading: stateless, safe from any thread.</remarks>
internal static class WrappingIndents
{
    /// <summary>Case-sensitive like Monaco; anything unknown (or <c>null</c>) is <see cref="WrappingIndent.Same"/>.</summary>
    /// <param name="value">One of <c>none</c>, <c>same</c>, <c>indent</c>, <c>deepIndent</c>.</param>
    /// <returns>The parsed value.</returns>
    public static WrappingIndent FromString(string? value) => value switch
    {
        "none" => WrappingIndent.None,
        "same" => WrappingIndent.Same,
        "indent" => WrappingIndent.Indent,
        "deepIndent" => WrappingIndent.DeepIndent,
        _ => WrappingIndent.Same,
    };
}
