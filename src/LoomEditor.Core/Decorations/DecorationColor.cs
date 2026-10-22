// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Decorations;

/// <summary>
/// A color a decoration asks for: a CSS hex color or the id of a theme color (Monaco: <c>string | ThemeColor</c>).
/// Exactly one of the two is set; Core does not resolve it (the view does).
/// </summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
/// <param name="Value">A hex color like <c>#RRGGBB</c> or <c>#RRGGBBAA</c>.</param>
/// <param name="ThemeColorId">The id of a theme color (Monaco: <c>themeColorFromId</c>).</param>
internal sealed record DecorationColor(string? Value, string? ThemeColorId)
{
    /// <summary>A hex color like <c>#RRGGBB(AA)</c>.</summary>
    public static DecorationColor FromValue(string css)
    {
        ArgumentException.ThrowIfNullOrEmpty(css);
        return new DecorationColor(css, null);
    }

    /// <summary>A theme color by id (Monaco: <c>themeColorFromId</c>).</summary>
    public static DecorationColor FromThemeColor(string id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        return new DecorationColor(null, id);
    }
}
