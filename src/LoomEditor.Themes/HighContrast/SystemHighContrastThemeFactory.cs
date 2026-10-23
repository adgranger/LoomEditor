using System.Text;

namespace LoomEditor.Themes.HighContrast;

/// <summary>
/// Builds an <see cref="EditorTheme"/> from the colors of the Windows high contrast scheme. No Monaco counterpart
/// (ADR 0010 decision 10): the meaning of a token is carried by bold, italic and underline, not by its hue.
/// </summary>
/// <remarks>Threading: stateless, safe to call from any thread.</remarks>
internal static class SystemHighContrastThemeFactory
{
    /// <summary>The name of the theme built when the caller gives none.</summary>
    internal const string DefaultName = "Windows High Contrast";

    /// <summary>
    /// Creates the theme. The colors are made opaque (Win32 colors have no alpha); the theme is <c>hc-black</c> when
    /// the background is darker than the text, <c>hc-light</c> otherwise.
    /// </summary>
    internal static EditorTheme Create(SystemHighContrastColors colors, string name = DefaultName)
    {
        ArgumentNullException.ThrowIfNull(name);

        string w = Hex(colors.Window);
        string t = Hex(colors.WindowText);
        string h = Hex(colors.Highlight);
        string ht = Hex(colors.HighlightText);
        string g = Hex(colors.GrayText);
        string l = Hex(colors.Hotlight);
        string bf = Hex(colors.ButtonFace);
        string bt = Hex(colors.ButtonText);
        const string Clear = "#00000000";

        bool dark = Color.FromThemeColor(Opaque(colors.Window)).GetRelativeLuminance()
            < Color.FromThemeColor(Opaque(colors.WindowText)).GetRelativeLuminance();

        string escapedName = name.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        string[] entries =
        [
            Entry("editor.background", w), Entry("editor.foreground", t),
            Entry("editor.selectionBackground", h), Entry("editor.selectionForeground", ht),
            Entry("editor.inactiveSelectionBackground", h),
            Entry("editor.lineHighlightBorder", l),
            Entry("editorCursor.foreground", t),
            Entry("editorLineNumber.foreground", t), Entry("editorLineNumber.activeForeground", l),
            Entry("editorWhitespace.foreground", g),
            Entry("editorIndentGuide.background1", g), Entry("editorIndentGuide.activeBackground1", t),
            Entry("editorBracketMatch.background", Clear), Entry("editorBracketMatch.border", l),
            Entry("editor.findMatchBackground", Clear), Entry("editor.findMatchBorder", l),
            Entry("editor.findMatchHighlightBackground", Clear), Entry("editor.findMatchHighlightBorder", h),
            Entry("editor.wordHighlightBackground", Clear), Entry("editor.wordHighlightBorder", h),
            Entry("editorGutter.foldingControlForeground", t),
            Entry("editorWidget.background", bf), Entry("editorWidget.foreground", bt), Entry("editorWidget.border", bt),
            Entry("input.background", w), Entry("input.foreground", t), Entry("input.border", t),
            Entry("inputOption.activeBackground", h), Entry("inputOption.activeForeground", ht), Entry("inputOption.activeBorder", l),
            Entry("focusBorder", l), Entry("errorForeground", l),
        ];

        StringBuilder json = new();
        json.Append("{\"name\":\"").Append(escapedName).Append("\",\"colors\":{");
        json.AppendJoin(',', entries);
        json.Append("},\"tokenColors\":[");
        json.Append(Rule("[\"comment\",\"punctuation.definition.comment\"]", g, "italic")).Append(',');
        json.Append(Rule("[\"keyword\",\"storage\",\"storage.type\"]", t, "bold")).Append(',');
        json.Append(Rule("[\"markup.underline.link\",\"string.other.link\"]", l, "underline"));
        json.Append("]}");

        return EditorTheme.Load(json.ToString(), dark ? "hc-black" : "hc-light", name);
    }

    private static ThemeColor Opaque(ThemeColor c) => ThemeColor.FromRgb(c.R, c.G, c.B);

    private static string Hex(ThemeColor c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private static string Entry(string id, string color) => $"\"{id}\":\"{color}\"";

    private static string Rule(string scopes, string foreground, string fontStyle) =>
        $"{{\"scope\":{scopes},\"settings\":{{\"foreground\":\"{foreground}\",\"fontStyle\":\"{fontStyle}\"}}}}";
}
