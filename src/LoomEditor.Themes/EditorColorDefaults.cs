// Ported from microsoft/vscode: src/vs/platform/theme/common/colorUtils.ts (resolveDefaultColor, resolveColorValue, executeTransform)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Themes;

/// <summary>
/// The default value of every editor color that <see cref="EditorTheme"/> exposes, and the colors they refer to (VS
/// Code: the <c>registerColor</c> calls of <c>editorColors.ts</c>, <c>editorColorRegistry.ts</c>, <c>baseColors.ts</c>,
/// <c>highlightDecorations.ts</c> and <c>foldingDecorations.ts</c>, as a static table instead of a registry). A default
/// is <see langword="null"/> (no color), a <c>#hex</c> color, the id of another color, or a transform
/// (<c>transparent</c>, <c>lessProminent</c>); the other transforms of <c>colorUtils.ts</c> are not needed by these ids.
/// </summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class EditorColorDefaults
{
    // id -> (dark, light, hcDark, hcLight). One line per registerColor, with the upstream line in a comment.
    // Color.black / Color.white are #000000 / #FFFFFF. A registerColor with a single value is the same for all four types.
    private static readonly Dictionary<string, (ColorValue? Dark, ColorValue? Light, ColorValue? HcDark, ColorValue? HcLight)> Table = new(StringComparer.Ordinal)
    {
        // editorColors.ts:19
        ["editor.background"] = ("#1E1E1E", "#ffffff", "#000000", "#FFFFFF"),

        // editorColors.ts:23
        ["editor.foreground"] = ("#BBBBBB", "#333333", "#FFFFFF", "foreground"),

        // editorColors.ts:121
        ["editor.selectionBackground"] = ("#264F78", "#ADD6FF", "#f3f518", "#0F4A85"),

        // editorColors.ts:125 (no default for the dark and light themes)
        ["editor.selectionForeground"] = (null, null, "#000000", "#FFFFFF"),

        // editorColors.ts:133 (activeContrastBorder is the id contrastActiveBorder)
        ["editor.selectionHighlightBackground"] = (
            ColorValue.LessProminent("editor.selectionBackground", "editor.background", 0.3, 0.6),
            ColorValue.LessProminent("editor.selectionBackground", "editor.background", 0.3, 0.6),
            null,
            null),

        // editorColors.ts:137
        ["editor.selectionHighlightBorder"] = (null, null, "contrastActiveBorder", "contrastActiveBorder"),

        // editorColors.ts:141
        ["editor.compositionBorder"] = ("#ffffff", "#000000", "#ffffff", "#000000"),

        // editorColors.ts:148
        ["editor.findMatchBackground"] = ("#515C6A", "#A8AC94", null, null),

        // editorColors.ts:152 (registered with a null default: the same for all theme types)
        ["editor.findMatchForeground"] = (null, null, null, null),

        // editorColors.ts:156
        ["editor.findMatchHighlightBackground"] = ("#EA5C0055", "#EA5C0055", null, null),

        // editorColors.ts:160 (registered with a null default)
        ["editor.findMatchHighlightForeground"] = (null, null, null, null),

        // editorColors.ts:164
        ["editor.findRangeHighlightBackground"] = ("#3a3d4166", "#b4b4b44d", null, null),

        // editorColors.ts:168
        ["editor.findMatchBorder"] = (null, null, "contrastActiveBorder", "contrastActiveBorder"),

        // editorColors.ts:172
        ["editor.findMatchHighlightBorder"] = (null, null, "contrastActiveBorder", "contrastActiveBorder"),

        // editorColors.ts:176
        ["editor.findRangeHighlightBorder"] = (
            null,
            null,
            ColorValue.Transparent("contrastActiveBorder", 0.4),
            ColorValue.Transparent("contrastActiveBorder", 0.4)),

        // editorColorRegistry.ts:14 (registered with a null default: the same for all theme types)
        ["editor.lineHighlightBackground"] = (null, null, null, null),

        // editorColorRegistry.ts:16
        ["editor.lineHighlightBorder"] = ("#282828", "#eeeeee", "#f38518", "contrastBorder"),

        // editorColors.ts:248-262 (rgba(124,124,124,0.3) and rgba(10,50,100,0.2 / 0.5))
        ["editor.snippetTabstopHighlightBackground"] = ("#7C7C7C4D", "#0A326433", "#7C7C7C4D", "#0A326433"),
        ["editor.snippetTabstopHighlightBorder"] = (null, null, null, null),
        ["editor.snippetFinalTabstopHighlightBackground"] = (null, null, null, null),
        ["editor.snippetFinalTabstopHighlightBorder"] = ("#525252", "#0A326480", "#525252", "#292929"),

        // editorColorRegistry.ts:17
        ["editor.rangeHighlightBackground"] = ("#ffffff0b", "#fdff0033", null, null),

        // editorColorRegistry.ts:18
        ["editor.rangeHighlightBorder"] = (null, null, "contrastActiveBorder", "contrastActiveBorder"),

        // editorColorRegistry.ts:22
        ["editorCursor.foreground"] = ("#AEAFAD", "#000000", "#FFFFFF", "#0F4A85"),

        // editorColorRegistry.ts:24
        ["editorMultiCursor.primary.foreground"] = ("editorCursor.foreground", "editorCursor.foreground", "editorCursor.foreground", "editorCursor.foreground"),

        // editorColorRegistry.ts:26
        ["editorMultiCursor.secondary.foreground"] = ("editorCursor.foreground", "editorCursor.foreground", "editorCursor.foreground", "editorCursor.foreground"),

        // editorColors.ts:436
        ["editorOverviewRuler.findMatchForeground"] = ("#d186167e", "#d186167e", "#AB5A00", "#AB5A00"),

        // editorColors.ts:440
        ["editorOverviewRuler.selectionHighlightForeground"] = ("#A0A0A0CC", "#A0A0A0CC", "#A0A0A0CC", "#A0A0A0CC"),

        // highlightDecorations.ts:20
        ["editorOverviewRuler.wordHighlightForeground"] = ("#A0A0A0CC", "#A0A0A0CC", "#A0A0A0CC", "#A0A0A0CC"),

        // highlightDecorations.ts:21
        ["editorOverviewRuler.wordHighlightStrongForeground"] = ("#C0A0C0CC", "#C0A0C0CC", "#C0A0C0CC", "#C0A0C0CC"),

        // highlightDecorations.ts:22
        ["editorOverviewRuler.wordHighlightTextForeground"] = (
            "editorOverviewRuler.selectionHighlightForeground", "editorOverviewRuler.selectionHighlightForeground",
            "editorOverviewRuler.selectionHighlightForeground", "editorOverviewRuler.selectionHighlightForeground"),

        // bracketMatching.ts:27
        ["editorOverviewRuler.bracketMatchForeground"] = ("#A0A0A0", "#A0A0A0", "#A0A0A0", "#A0A0A0"),

        // editorColors.ts:105 (editorHint.foreground)
        ["editorHint.foreground"] = (ColorValue.Transparent("#eeeeee", 0.7), "#6c6c6c", null, null),

        // editorColors.ts:109 (editorHint.border)
        ["editorHint.border"] = (null, null, ColorValue.Transparent("#eeeeee", 0.8), "#292929"),

        // editorColorRegistry.ts:75
        ["editorOverviewRuler.errorForeground"] = (ColorValue.Transparent("#FF1212", 0.7), ColorValue.Transparent("#FF1212", 0.7), "#FF3232", "#B5200D"),

        // editorColorRegistry.ts:76
        ["editorOverviewRuler.warningForeground"] = ("editorWarning.foreground", "editorWarning.foreground", "editorWarning.border", "editorWarning.border"),

        // editorColorRegistry.ts:77
        ["editorOverviewRuler.infoForeground"] = ("editorInfo.foreground", "editorInfo.foreground", "editorInfo.border", "editorInfo.border"),

        // editorColorRegistry.ts:61
        ["editorOverviewRuler.border"] = ("#7f7f7f4d", "#7f7f7f4d", "#7f7f7f4d", "#666666"),

        // editorColorRegistry.ts:62
        ["editorOverviewRuler.background"] = (null, null, null, null),

        // editorColorRegistry.ts:28 (the default of the deprecated indent guide colors)
        ["editorWhitespace.foreground"] = ("#e3e4e229", "#33333333", "#e3e4e229", "#CCCCCC"),

        // editorColorRegistry.ts:30
        ["editorLineNumber.foreground"] = ("#858585", "#237893", "#FFFFFF", "#292929"),

        // editorColorRegistry.ts:32 (deprecated id)
        ["editorIndentGuide.background"] = ("editorWhitespace.foreground", "editorWhitespace.foreground", "editorWhitespace.foreground", "editorWhitespace.foreground"),

        // editorColorRegistry.ts:33 (deprecated id)
        ["editorIndentGuide.activeBackground"] = ("editorWhitespace.foreground", "editorWhitespace.foreground", "editorWhitespace.foreground", "editorWhitespace.foreground"),

        // editorColorRegistry.ts:35
        ["editorIndentGuide.background1"] = ("editorIndentGuide.background", "editorIndentGuide.background", "editorIndentGuide.background", "editorIndentGuide.background"),

        // editorColorRegistry.ts:36
        ["editorIndentGuide.background2"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:37
        ["editorIndentGuide.background3"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:38
        ["editorIndentGuide.background4"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:39
        ["editorIndentGuide.background5"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:40
        ["editorIndentGuide.background6"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:42
        ["editorIndentGuide.activeBackground1"] = ("editorIndentGuide.activeBackground", "editorIndentGuide.activeBackground", "editorIndentGuide.activeBackground", "editorIndentGuide.activeBackground"),

        // editorColorRegistry.ts:43
        ["editorIndentGuide.activeBackground2"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:44
        ["editorIndentGuide.activeBackground3"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:45
        ["editorIndentGuide.activeBackground4"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:46
        ["editorIndentGuide.activeBackground5"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:47
        ["editorIndentGuide.activeBackground6"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:49 (deprecated id)
        ["editorActiveLineNumber.foreground"] = ("#c6c6c6", "#0B216F", "contrastActiveBorder", "contrastActiveBorder"),

        // editorColorRegistry.ts:50 (the default is the deprecated color, for every theme type)
        ["editorLineNumber.activeForeground"] = ("editorActiveLineNumber.foreground", "editorActiveLineNumber.foreground", "editorActiveLineNumber.foreground", "editorActiveLineNumber.foreground"),

        // editorColorRegistry.ts:57
        ["editorBracketMatch.background"] = ("#0064001a", "#0064001a", "#0064001a", "#0000"),

        // editorColorRegistry.ts:58
        ["editorBracketMatch.border"] = ("#888", "#B9B9B9", "contrastBorder", "contrastBorder"),

        // editorColorRegistry.ts:59 (registered with a null default)
        ["editorBracketMatch.foreground"] = (null, null, null, null),

        // editorColorRegistry.ts:79
        ["editorBracketHighlight.foreground1"] = ("#FFD700", "#0431FAFF", "#FFD700", "#0431FAFF"),

        // editorColorRegistry.ts:80
        ["editorBracketHighlight.foreground2"] = ("#DA70D6", "#319331FF", "#DA70D6", "#319331FF"),

        // editorColorRegistry.ts:81
        ["editorBracketHighlight.foreground3"] = ("#179FFF", "#7B3814FF", "#87CEFA", "#7B3814FF"),

        // editorColorRegistry.ts:82
        ["editorBracketHighlight.foreground4"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:83
        ["editorBracketHighlight.foreground5"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:84
        ["editorBracketHighlight.foreground6"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:86 (new Color(new RGBA(255, 18, 18, 0.8)) is #FF1212CC; RGBA(255, 50, 50, 1) is #FF3232)
        ["editorBracketHighlight.unexpectedBracket.foreground"] = ("#FF1212CC", "#FF1212CC", "#FF3232", "#B5200D"),

        // editorColorRegistry.ts:88
        ["editorBracketPairGuide.background1"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:89
        ["editorBracketPairGuide.background2"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:90
        ["editorBracketPairGuide.background3"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:91
        ["editorBracketPairGuide.background4"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:92
        ["editorBracketPairGuide.background5"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:93
        ["editorBracketPairGuide.background6"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:95
        ["editorBracketPairGuide.activeBackground1"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:96
        ["editorBracketPairGuide.activeBackground2"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:97
        ["editorBracketPairGuide.activeBackground3"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:98
        ["editorBracketPairGuide.activeBackground4"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:99
        ["editorBracketPairGuide.activeBackground5"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // editorColorRegistry.ts:100
        ["editorBracketPairGuide.activeBackground6"] = ("#00000000", "#00000000", "#00000000", "#00000000"),

        // highlightDecorations.ts:14
        ["editor.wordHighlightBackground"] = ("#575757B8", "#57575740", null, null),

        // highlightDecorations.ts:15
        ["editor.wordHighlightStrongBackground"] = ("#004972B8", "#0e639c40", null, null),

        // highlightDecorations.ts:16
        ["editor.wordHighlightTextBackground"] = (
            "editor.wordHighlightBackground", "editor.wordHighlightBackground", "editor.wordHighlightBackground", "editor.wordHighlightBackground"),

        // highlightDecorations.ts:17
        ["editor.wordHighlightBorder"] = (null, null, "contrastActiveBorder", "contrastActiveBorder"),

        // highlightDecorations.ts:18
        ["editor.wordHighlightStrongBorder"] = (null, null, "contrastActiveBorder", "contrastActiveBorder"),

        // highlightDecorations.ts:19
        ["editor.wordHighlightTextBorder"] = (
            "editor.wordHighlightBorder", "editor.wordHighlightBorder", "editor.wordHighlightBorder", "editor.wordHighlightBorder"),

        // foldingDecorations.ts:17
        ["editor.foldBackground"] = (
            ColorValue.Transparent("editor.selectionBackground", 0.3),
            ColorValue.Transparent("editor.selectionBackground", 0.3),
            null,
            null),

        // foldingDecorations.ts:18
        ["editor.foldPlaceholderForeground"] = ("#808080", "#808080", null, null),

        // foldingDecorations.ts:19
        ["editorGutter.foldingControlForeground"] = ("icon.foreground", "icon.foreground", "icon.foreground", "icon.foreground"),

        // baseColors.ts:13
        ["foreground"] = ("#CCCCCC", "#616161", "#FFFFFF", "#292929"),

        // baseColors.ts:33
        ["icon.foreground"] = ("#C5C5C5", "#424242", "#FFFFFF", "#292929"),

        // LoomEditor's own id: Monaco hard-codes the color of the selection anchor in anchorSelect.css.
        ["loom.selectionAnchor"] = ("#007ACC", "#007ACC", "#007ACC", "#007ACC"),

        // editorColors.ts:264–335 (diff editor). defaultInsertColor = rgba(155,185,85,0.2), defaultRemoveColor = rgba(255,0,0,0.2).
        ["diffEditor.insertedTextBackground"] = ("#9ccc2c33", "#9ccc2c40", null, null),
        ["diffEditor.removedTextBackground"] = ("#ff000033", "#ff000033", null, null),
        ["diffEditor.insertedLineBackground"] = ("#9bb95533", "#9bb95533", null, null),
        ["diffEditor.removedLineBackground"] = ("#ff000033", "#ff000033", null, null),
        ["diffEditorGutter.insertedLineBackground"] = (null, null, null, null),
        ["diffEditorGutter.removedLineBackground"] = (null, null, null, null),
        ["diffEditorOverview.insertedForeground"] = (null, null, null, null),
        ["diffEditorOverview.removedForeground"] = (null, null, null, null),
        ["diffEditor.insertedTextBorder"] = (null, null, "#33ff2eff", "#374E06"),
        ["diffEditor.removedTextBorder"] = (null, null, "#FF008F", "#AD0707"),
        ["diffEditor.border"] = (null, null, "contrastBorder", "contrastBorder"),
        ["diffEditor.diagonalFill"] = ("#cccccc33", "#22222233", null, null),
        ["diffEditor.unchangedRegionBackground"] = ("sideBar.background", "sideBar.background", "sideBar.background", "sideBar.background"),
        ["diffEditor.unchangedRegionForeground"] = ("foreground", "foreground", "foreground", "foreground"),
        ["diffEditor.unchangedCodeBackground"] = ("#74747429", "#b8b8b829", null, null),

        // theme.ts:625
        ["sideBar.background"] = ("#252526", "#F3F3F3", "#000000", "#FFFFFF"),

        // baseColors.ts:37
        ["focusBorder"] = ("#007FD4", "#0090F1", "#F38518", "#006BBD"),

        // baseColors.ts:41
        ["contrastBorder"] = (null, null, "#6FC3DF", "#0F4A85"),

        // baseColors.ts:45
        ["contrastActiveBorder"] = (null, null, "focusBorder", "focusBorder"),

        // baseColors.ts:25
        ["errorForeground"] = ("#F48771", "#A1260D", "#F48771", "#B5200D"),

        // editorColors.ts:49
        ["editorWidget.background"] = ("#252526", "#F3F3F3", "#0C141F", "#FFFFFF"),

        // editorColors.ts:53
        ["editorWidget.foreground"] = ("foreground", "foreground", "foreground", "foreground"),

        // editorColors.ts:57
        ["editorWidget.border"] = ("#CCCCCC33", "#61616133", "contrastBorder", "contrastBorder"),

        // editorColors.ts:187-198 (the hover widget colors are the editor widget colors)
        ["editorHoverWidget.background"] = ("editorWidget.background", "editorWidget.background", "editorWidget.background", "editorWidget.background"),
        ["editorHoverWidget.foreground"] = ("editorWidget.foreground", "editorWidget.foreground", "editorWidget.foreground", "editorWidget.foreground"),
        ["editorHoverWidget.border"] = ("editorWidget.border", "editorWidget.border", "editorWidget.border", "editorWidget.border"),

        // baseColors.ts / listColors.ts / quickpickColors.ts / suggestWidget.ts: the completion list colors and the chains they need
        // listColors.ts:33
        ["list.activeSelectionBackground"] = ("#04395E", "#0060C0", null, ColorValue.Transparent("#0F4A85", 0.1)),
        // listColors.ts:37
        ["list.activeSelectionForeground"] = ("#FFFFFF", "#FFFFFF", null, null),
        // listColors.ts:41
        ["list.activeSelectionIconForeground"] = (null, null, null, null),
        // listColors.ts:81
        ["list.highlightForeground"] = ("#2AAAFF", "#0066BF", "focusBorder", "focusBorder"),
        // listColors.ts:85 (light: ifDefinedThenElse(list.activeSelectionBackground, ...) is always defined here)
        ["list.focusHighlightForeground"] = ("list.highlightForeground", "list.highlightForeground", "list.highlightForeground", "list.highlightForeground"),
        // quickpickColors.ts:41
        ["quickInputList.focusForeground"] = ("list.activeSelectionForeground", "list.activeSelectionForeground", "list.activeSelectionForeground", "list.activeSelectionForeground"),
        // quickpickColors.ts:45
        ["quickInputList.focusIconForeground"] = ("list.activeSelectionIconForeground", "list.activeSelectionIconForeground", "list.activeSelectionIconForeground", "list.activeSelectionIconForeground"),
        // quickpickColors.ts:49 (oneOf(deprecated quickInput.list.focusBackground, list.activeSelectionBackground))
        ["quickInputList.focusBackground"] = ("list.activeSelectionBackground", "list.activeSelectionBackground", null, null),
        // suggestWidget.ts:43
        ["editorSuggestWidget.background"] = ("editorWidget.background", "editorWidget.background", "editorWidget.background", "editorWidget.background"),
        // suggestWidget.ts:44
        ["editorSuggestWidget.border"] = ("editorWidget.border", "editorWidget.border", "editorWidget.border", "editorWidget.border"),
        // suggestWidget.ts:45
        ["editorSuggestWidget.foreground"] = ("editor.foreground", "editor.foreground", "editor.foreground", "editor.foreground"),
        // suggestWidget.ts:46
        ["editorSuggestWidget.selectedForeground"] = ("quickInputList.focusForeground", "quickInputList.focusForeground", "editorSuggestWidget.background", "editorSuggestWidget.background"),
        // suggestWidget.ts:48
        ["editorSuggestWidget.selectedBackground"] = ("quickInputList.focusBackground", "quickInputList.focusBackground", "editorSuggestWidget.foreground", "editorSuggestWidget.foreground"),
        // suggestWidget.ts:50
        ["editorSuggestWidget.highlightForeground"] = ("list.highlightForeground", "list.highlightForeground", "list.highlightForeground", "list.highlightForeground"),
        // suggestWidget.ts:51
        ["editorSuggestWidget.focusHighlightForeground"] = ("list.focusHighlightForeground", "list.focusHighlightForeground", "editorSuggestWidget.selectedForeground", "editorSuggestWidget.selectedForeground"),

        // baseColors.ts:56
        ["textLink.foreground"] = ("#3794FF", "#006AB1", "#21A6FF", "#0F4A85"),

        // baseColors.ts:95
        ["textCodeBlock.background"] = ("#0a0a0a66", "#dcdcdc66", "#000000", "#F2F2F2"),

        // inputColors.ts:20
        ["input.background"] = ("#3C3C3C", "#FFFFFF", "#000000", "#FFFFFF"),

        // inputColors.ts:24
        ["input.foreground"] = ("foreground", "foreground", "foreground", "foreground"),

        // inputColors.ts:28
        ["input.border"] = (null, null, "contrastBorder", "contrastBorder"),

        // inputColors.ts:32
        ["inputOption.activeBorder"] = ("#007ACC", "#007ACC", "contrastBorder", "contrastBorder"),

        // inputColors.ts:40
        ["inputOption.activeBackground"] = ("#007FD466", "#0090F133", "#00000000", "#00000000"),

        // inputColors.ts:44
        ["inputOption.activeForeground"] = ("#FFFFFF", "#000000", "foreground", "foreground"),

        // editorColors.ts:74 (editorError.foreground), :83 (editorWarning.foreground), :87, :96 (editorInfo.foreground), :100
        ["editorError.foreground"] = ("#F14C4C", "#E51400", "#F48771", "#B5200D"),
        ["editorWarning.foreground"] = ("#CCA700", "#BF8803", "#FFD370", "#895503"),
        ["editorWarning.border"] = (null, null, ColorValue.Transparent("#FFCC00", 0.8), ColorValue.Transparent("#FFCC00", 0.8)),
        ["editorInfo.foreground"] = ("#59a4f9", "#0063d3", "#59a4f9", "#0063d3"),
        ["editorInfo.border"] = (null, null, ColorValue.Transparent("#59a4f9", 0.8), "#292929"),

        // colorRegistry.ts: scrollbarShadow (the minimap shadow)
        ["scrollbar.shadow"] = ("#000000", "#DDDDDD", null, null),

        // editorColors.ts:28-46 (hcLight hover: #0F4A85 at 0.1)
        ["editorStickyScroll.background"] = ("editor.background", "editor.background", "editor.background", "editor.background"),
        ["editorStickyScrollGutter.background"] = ("editor.background", "editor.background", "editor.background", "editor.background"),
        ["editorStickyScrollHover.background"] = ("#2A2D2E", "#F0F0F0", null, ColorValue.Transparent("#0F4A85", 0.1)),
        ["editorStickyScroll.border"] = (null, null, "contrastBorder", "contrastBorder"),
        ["editorStickyScroll.shadow"] = ("scrollbar.shadow", "scrollbar.shadow", "scrollbar.shadow", "scrollbar.shadow"),

        // miscColors.ts:56, :60, :64
        ["scrollbarSlider.background"] = (
            ColorValue.Transparent("#797979", 0.4), ColorValue.Transparent("#646464", 0.4),
            ColorValue.Transparent("contrastBorder", 0.6), ColorValue.Transparent("contrastBorder", 0.4)),
        ["scrollbarSlider.hoverBackground"] = (
            ColorValue.Transparent("#646464", 0.7), ColorValue.Transparent("#646464", 0.7),
            ColorValue.Transparent("contrastBorder", 0.8), ColorValue.Transparent("contrastBorder", 0.8)),
        ["scrollbarSlider.activeBackground"] = (
            ColorValue.Transparent("#BFBFBF", 0.4), ColorValue.Transparent("#000000", 0.6), "contrastBorder", "contrastBorder"),

        // minimapColors.ts:17-60 (editorFindMatchHighlight = editor.findMatchHighlightBackground,
        // editorSelectionHighlight = editor.selectionHighlightBackground)
        ["minimap.findMatchHighlight"] = (
            "editor.findMatchHighlightBackground", "editor.findMatchHighlightBackground",
            "editor.findMatchHighlightBackground", "editor.findMatchHighlightBackground"),
        ["minimap.selectionOccurrenceHighlight"] = (
            "editor.selectionHighlightBackground", "editor.selectionHighlightBackground",
            "editor.selectionHighlightBackground", "editor.selectionHighlightBackground"),
        ["minimap.selectionHighlight"] = (
            "editor.selectionBackground", "editor.selectionBackground", "editor.selectionBackground", "editor.selectionBackground"),
        ["minimap.infoHighlight"] = ("editorInfo.foreground", "editorInfo.foreground", "editorInfo.border", "editorInfo.border"),
        ["minimap.warningHighlight"] = ("editorWarning.foreground", "editorWarning.foreground", "editorWarning.border", "editorWarning.border"),
        ["minimap.errorHighlight"] = ("#FF1212B3", "#FF1212B3", "#FF3232", "#B5200D"),
        ["minimap.background"] = (null, null, null, null),
        ["minimap.foregroundOpacity"] = ("#000000FF", "#000000FF", "#000000FF", "#000000FF"),
        ["minimapSlider.background"] = (
            ColorValue.Transparent("scrollbarSlider.background", 0.5), ColorValue.Transparent("scrollbarSlider.background", 0.5),
            ColorValue.Transparent("scrollbarSlider.background", 0.5), ColorValue.Transparent("scrollbarSlider.background", 0.5)),
        ["minimapSlider.hoverBackground"] = (
            ColorValue.Transparent("scrollbarSlider.hoverBackground", 0.5), ColorValue.Transparent("scrollbarSlider.hoverBackground", 0.5),
            ColorValue.Transparent("scrollbarSlider.hoverBackground", 0.5), ColorValue.Transparent("scrollbarSlider.hoverBackground", 0.5)),
        ["minimapSlider.activeBackground"] = (
            ColorValue.Transparent("scrollbarSlider.activeBackground", 0.5), ColorValue.Transparent("scrollbarSlider.activeBackground", 0.5),
            ColorValue.Transparent("scrollbarSlider.activeBackground", 0.5), ColorValue.Transparent("scrollbarSlider.activeBackground", 0.5)),
    };

    /// <summary>Whether <paramref name="id"/> has a default (even a <see langword="null"/> one).</summary>
    public static bool IsKnown(string id) => Table.ContainsKey(id);

    /// <summary>
    /// <c>theme.getColor(id, useDefault: true)</c>: the color the theme sets for <paramref name="id"/>, else its default
    /// for <paramref name="type"/>. A default that names another color is resolved the same way, so a theme value of
    /// the referenced color wins over its default (<c>resolveColorValue</c>).
    /// </summary>
    /// <param name="id">A color id, for example <c>editor.background</c>.</param>
    /// <param name="type">The type of the theme.</param>
    /// <param name="themeColors">The colors the theme sets: <see langword="null"/> when it does not set an id.</param>
    /// <returns>The color, or <see langword="null"/> when there is none (unknown id, <see langword="null"/> default).</returns>
    public static ThemeColor? Resolve(string id, ThemeType type, Func<string, ThemeColor?> themeColors)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(themeColors);
        return themeColors(id) ?? ResolveDefault(id, type, themeColors);
    }

    // resolveDefaultColor
    private static ThemeColor? ResolveDefault(string id, ThemeType type, Func<string, ThemeColor?> themeColors)
    {
        if (!Table.TryGetValue(id, out (ColorValue? Dark, ColorValue? Light, ColorValue? HcDark, ColorValue? HcLight) defaults))
        {
            return null;
        }

        ColorValue? colorValue = type switch
        {
            ThemeType.Light => defaults.Light,
            ThemeType.HighContrastDark => defaults.HcDark,
            ThemeType.HighContrastLight => defaults.HcLight,
            _ => defaults.Dark,
        };

        return ResolveColorValue(colorValue, type, themeColors);
    }

    // resolveColorValue
    private static ThemeColor? ResolveColorValue(ColorValue? colorValue, ThemeType type, Func<string, ThemeColor?> themeColors)
    {
        switch (colorValue)
        {
            case null:
                return null;
            case ColorValue.Reference reference:
                return reference.Value[0] == '#' ? ThemeColor.Parse(reference.Value) : Resolve(reference.Value, type, themeColors);
            default:
                return ExecuteTransform(colorValue, type, themeColors);
        }
    }

    // executeTransform (Transparent and LessProminent only)
    private static ThemeColor? ExecuteTransform(ColorValue transform, ThemeType type, Func<string, ThemeColor?> themeColors)
    {
        switch (transform)
        {
            case ColorValue.TransparentValue transparent:
                return ResolveColorValue(transparent.Value, type, themeColors) is { } value
                    ? Color.FromThemeColor(value).Transparent(transparent.Factor).ToThemeColor()
                    : null;

            case ColorValue.LessProminentValue lessProminent:
            {
                if (ResolveColorValue(lessProminent.Value, type, themeColors) is not { } themeFrom)
                {
                    return null;
                }

                Color from = Color.FromThemeColor(themeFrom);
                if (ResolveColorValue(lessProminent.Background, type, themeColors) is not { } themeBackground)
                {
                    return from.Transparent(lessProminent.Factor * lessProminent.Transparency).ToThemeColor();
                }

                Color background = Color.FromThemeColor(themeBackground);
                return (from.IsDarkerThan(background)
                    ? Color.GetLighterColor(from, background, lessProminent.Factor).Transparent(lessProminent.Transparency)
                    : Color.GetDarkerColor(from, background, lessProminent.Factor).Transparent(lessProminent.Transparency)).ToThemeColor();
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(transform), transform, "Unknown color transform.");
        }
    }
}
