namespace LoomEditor.Themes;

/// <summary>
/// One token color rule of a theme, in VS Code's order (VS Code: <c>ITextMateThemingRule</c>, without the font
/// size, font family and line height).
/// </summary>
/// <param name="Scopes">The TextMate scope selectors the rule applies to; empty for the default rule (rule 0: the default foreground and background).</param>
/// <param name="Foreground">The foreground color, or <see langword="null"/> when the rule does not set one.</param>
/// <param name="Background">The background color, or <see langword="null"/> when the rule does not set one.</param>
/// <param name="FontStyle">The font style as written in the theme (space separated <c>italic</c>, <c>bold</c>, <c>underline</c>, <c>strikethrough</c>), or <see langword="null"/>.</param>
/// <remarks>Threading: immutable.</remarks>
public sealed record TokenColorRule(IReadOnlyList<string> Scopes, ThemeColor? Foreground, ThemeColor? Background, string? FontStyle);
