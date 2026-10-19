// Ported from microsoft/vscode-textmate: src/theme.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Theme;

/// <summary><c>FontStyle</c> of vscode-textmate (not Monaco's <c>LoomEditor.Core.Tokens.FontStyle</c>).</summary>
/// <remarks>Threading: a value type.</remarks>
[Flags]
internal enum FontStyle
{
    /// <summary>Not set by the rule (inherits).</summary>
    NotSet = -1,

    /// <summary>No style.</summary>
    None = 0,

    /// <summary>Italic.</summary>
    Italic = 1,

    /// <summary>Bold.</summary>
    Bold = 2,

    /// <summary>Underline.</summary>
    Underline = 4,

    /// <summary>Strikethrough.</summary>
    Strikethrough = 8,
}

/// <summary><c>fontStyleToString</c>.</summary>
/// <remarks>Threading: stateless.</remarks>
internal static class FontStyleExtensions
{
    /// <summary><c>fontStyleToString</c>: <c>not set</c>, <c>none</c>, or the flags separated by spaces.</summary>
    public static string FontStyleToString(this FontStyle fontStyle)
    {
        if (fontStyle == FontStyle.NotSet)
        {
            return "not set";
        }

        string style = string.Empty;
        if ((fontStyle & FontStyle.Italic) != 0)
        {
            style += "italic ";
        }

        if ((fontStyle & FontStyle.Bold) != 0)
        {
            style += "bold ";
        }

        if ((fontStyle & FontStyle.Underline) != 0)
        {
            style += "underline ";
        }

        if ((fontStyle & FontStyle.Strikethrough) != 0)
        {
            style += "strikethrough ";
        }

        if (style.Length == 0)
        {
            style = "none";
        }

        return style.Trim();
    }
}
