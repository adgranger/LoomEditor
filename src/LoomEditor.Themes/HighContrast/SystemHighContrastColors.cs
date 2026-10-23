namespace LoomEditor.Themes.HighContrast;

/// <summary>
/// The eight colors of the Windows high contrast scheme that the editor theme is built from. No Monaco counterpart.
/// </summary>
/// <remarks>Threading: immutable value, safe to share between threads.</remarks>
/// <param name="Window">The background (<c>COLOR_WINDOW</c>).</param>
/// <param name="WindowText">The text (<c>COLOR_WINDOWTEXT</c>).</param>
/// <param name="Highlight">The background of selected items (<c>COLOR_HIGHLIGHT</c>).</param>
/// <param name="HighlightText">The text of selected items (<c>COLOR_HIGHLIGHTTEXT</c>).</param>
/// <param name="GrayText">The disabled text (<c>COLOR_GRAYTEXT</c>).</param>
/// <param name="Hotlight">The hyperlinks (<c>COLOR_HOTLIGHT</c>).</param>
/// <param name="ButtonFace">The button surfaces (<c>COLOR_BTNFACE</c>).</param>
/// <param name="ButtonText">The button text (<c>COLOR_BTNTEXT</c>).</param>
internal readonly record struct SystemHighContrastColors(
    ThemeColor Window,
    ThemeColor WindowText,
    ThemeColor Highlight,
    ThemeColor HighlightText,
    ThemeColor GrayText,
    ThemeColor Hotlight,
    ThemeColor ButtonFace,
    ThemeColor ButtonText);
