// Ported from microsoft/vscode: src/vs/workbench/services/themes/common/themeCompatibility.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.Json.Nodes;

namespace LoomEditor.Themes;

// The old theme format: a `settings` array whose scope-less entries hold the editor colors under short names.
internal static partial class VsCodeThemeLoader
{
    // settingToColorIdMapping / addSettingMapping (without the terminal.ansi* mappings, which belong to the workbench).
    private static readonly Dictionary<string, string[]> SettingToColorIds = new(StringComparer.Ordinal)
    {
        ["background"] = ["editor.background"],
        ["foreground"] = ["editor.foreground"],
        ["selection"] = ["editor.selectionBackground"],
        ["inactiveSelection"] = ["editor.inactiveSelectionBackground"],
        ["selectionHighlightColor"] = ["editor.selectionHighlightBackground"],
        ["findMatchHighlight"] = ["editor.findMatchHighlightBackground", "peekViewResult.matchHighlightBackground"],
        ["currentFindMatchHighlight"] = ["editor.findMatchBackground"],
        ["hoverHighlight"] = ["editor.hoverHighlightBackground"],
        ["wordHighlight"] = ["editor.wordHighlightBackground"],
        ["wordHighlightStrong"] = ["editor.wordHighlightStrongBackground"],
        ["findRangeHighlight"] = ["editor.findRangeHighlightBackground"],
        ["referenceHighlight"] = ["peekViewEditor.matchHighlightBackground"],
        ["lineHighlight"] = ["editor.lineHighlightBackground"],
        ["rangeHighlight"] = ["editor.rangeHighlightBackground"],
        ["caret"] = ["editorCursor.foreground"],
        ["invisibles"] = ["editorWhitespace.foreground"],
        ["guide"] = ["editorIndentGuide.background1"],
        ["activeGuide"] = ["editorIndentGuide.activeBackground1"],
    };

    // convertSettings: every entry is a token rule; the settings of a scope-less entry also set colors.
    private static void ConvertSettings(JsonArray oldSettings, Dictionary<string, ThemeColor> colors, List<ThemeRule> rules)
    {
        foreach (JsonNode? item in oldSettings)
        {
            if (item is not JsonObject rule)
            {
                continue;
            }

            if (ReadRule(rule) is { } themeRule)
            {
                rules.Add(themeRule);
            }

            bool hasScope = rule["scope"] is { } scope && AsString(scope) != string.Empty;
            if (hasScope || rule["settings"] is not JsonObject settings)
            {
                continue;
            }

            foreach ((string key, JsonNode? value) in settings)
            {
                if (SettingToColorIds.TryGetValue(key, out string[]? colorIds) && AsString(value) is { } colorHex)
                {
                    ThemeColor color = ColorNormalization.FromHex(colorHex);
                    foreach (string colorId in colorIds)
                    {
                        colors[colorId] = color;
                    }
                }
            }
        }
    }
}
