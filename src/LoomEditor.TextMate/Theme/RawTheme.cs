// Ported from microsoft/vscode-textmate: src/theme.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Theme;

/// <summary><c>IRawTheme</c>: a TextMate theme.</summary>
/// <param name="Settings"><c>settings</c>: the theme rules.</param>
/// <param name="Name"><c>name</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record RawTheme(IReadOnlyList<RawThemeSetting>? Settings, string? Name = null);

/// <summary><c>IRawThemeSetting</c>: a single theme setting.</summary>
/// <param name="Name"><c>name</c>.</param>
/// <param name="ScopeText"><c>scope</c> when it is a string (a <c>ScopePattern</c>: comma-separated scope paths).</param>
/// <param name="Scope"><c>scope</c> when it is an array of scope patterns.</param>
/// <param name="Settings"><c>settings</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record RawThemeSetting(string? Name, string? ScopeText, IReadOnlyList<string>? Scope, RawThemeSettings? Settings);

/// <summary>
/// The <c>settings</c> of an <c>IRawThemeSetting</c>. <c>fontFamily</c>, <c>fontSize</c> and <c>lineHeight</c> are
/// not ported (ADR 0008, decision 3).
/// </summary>
/// <param name="FontStyle"><c>fontStyle</c>: space-separated <c>italic</c>, <c>bold</c>, <c>underline</c>, <c>strikethrough</c>.</param>
/// <param name="Foreground"><c>foreground</c>: a hex color.</param>
/// <param name="Background"><c>background</c>: a hex color.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed record RawThemeSettings(string? FontStyle, string? Foreground, string? Background);
