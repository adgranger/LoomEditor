using LoomEditor.Core.Markers;

namespace LoomEditor.Gallery;

/// <summary>The "Features" picker entries of phase 10 (fonts, zoom, whitespace, overtype, selection commands, snippets, problems).</summary>
/// <remarks>Threading: every member is pure.</remarks>
internal static class Phase10Samples
{
    /// <summary>The owner of the markers added by the "Add sample problems" button.</summary>
    public const string ProblemsOwner = "gallery-sample";

    /// <summary>The snippet of the "Insert snippet" button.</summary>
    public const string ForLoopSnippet = "for (let ${1:i} = 0; $1 < ${2:array}.length; $1++) {\n\tconst ${3:element} = $2[$1];\n\t$0\n}";

    /// <summary>The snippet of the "Insert snippet around selection" button.</summary>
    public const string LogSelectionSnippet = "console.log('${TM_SELECTED_TEXT}', ${TM_SELECTED_TEXT});$0";

    /// <summary>The samples, in picker order.</summary>
    public static IReadOnlyList<FeatureSample> All { get; } = (FeatureSample[])
    [
        new("Fonts and ligatures", "typescript", FontsText, 1, 1, "Switch Cascadia Code, Ligatures, Font weight, Letter spacing; the caret crosses `=>` in two steps"),
        new("Zoom", "csharp", ZoomText, 1, 1, "A−, A+, 100 %, Ctrl + wheel with the switch on"),
        new("Whitespace and control characters", "plaintext", WhitespaceText, 1, 1, "Render whitespace: None, Selection, Boundary, Trailing, All; Control characters switch"),
        new("Overtype and cursor styles", "plaintext", "0123456789\n0123456789\n0123456789", 1, 1, "Insert toggles overtype; cursor style box; type over the digits"),
        new("Selection commands", "csharp", SelectionText, 3, 9, "Ctrl+K Ctrl+B / Ctrl+K Ctrl+K, Ctrl+L, Ctrl+U, Shift+Alt+→ / ←, drag selected text (Ctrl = copy), Escape cancels a drag"),
        new("Snippets", "typescript", "\n\nfoo bar baz\n\n", 1, 1, "Insert snippet: Tab / Shift+Tab / Escape; select a word then \"Insert snippet around selection\" (${TM_SELECTED_TEXT}); accept the completion snippet with the fake language server demo"),
        new("Problems", "typescript", ProblemsText, 1, 1, "\"Add sample problems\", then F8 / Shift+F8 cycle, Escape ends, Narrator reads the message"),
    ];

    /// <summary>Two errors, two warnings and one info at fixed positions of the "Problems" sample.</summary>
    public static IReadOnlyList<MarkerData> SampleProblems { get; } = (MarkerData[])
    [
        new(MarkerSeverity.Error, "Cannot find name 'totl'.", 7, 12, 7, 16),
        new(MarkerSeverity.Warning, "'unused' is declared but never used.", 2, 9, 2, 15),
        new(MarkerSeverity.Info, "Consider using 'const'.", 3, 5, 3, 8),
        new(MarkerSeverity.Error, "Expected ';'.", 5, 19, 5, 19),
        new(MarkerSeverity.Warning, "Unreachable code detected.", 9, 5, 9, 24),
    ];

    private const string FontsText =
        "const add = (a: number, b: number) => a + b;\n"
        + "\n"
        + "=> != -> <= >= == === !== |> <|> www\n"
        + "\n"
        + "function compare(a: number, b: number) {\n"
        + "    if (a !== b && a >= b) {\n"
        + "        return a - b;\n"
        + "    }\n"
        + "    return a <= b ? -1 : 0;\n"
        + "}";

    private const string ZoomText =
        "public sealed class Greeter\n"
        + "{\n"
        + "    private readonly string _name;\n"
        + "\n"
        + "    public Greeter(string name) => _name = name;\n"
        + "\n"
        + "    public string Greet() => $\"Hello, {_name}!\";\n"
        + "}";

    // Tabs, double and triple spaces, trailing spaces, a whitespace-only line and control characters (U+0001, U+0007, U+007F, U+FEFF).
    private const string WhitespaceText =
        "\tone tab, then\ttext\n"
        + "a  b   c\n"
        + "trailing spaces   \n"
        + "    \n"
        + "a\u0001b\u0007c\u007Fd﻿e";

    private const string SelectionText =
        "public class UserAccount\n"
        + "{\n"
        + "    public string getUserName(string first_name, string lastName)\n"
        + "    {\n"
        + "        var fullName = (first_name + \" \" + lastName).Trim();\n"
        + "        return fullName.Substring(0, Math.Min(10, fullName.Length));\n"
        + "    }\n"
        + "}";

    private const string ProblemsText =
        "function sum(values: number[]) {\n"
        + "    let unused = 0;\n"
        + "    let total = 0;\n"
        + "    for (const value of values) {\n"
        + "    total += value\n"
        + "    }\n"
        + "    return totl\n"
        + "    // after the return\n"
        + "    console.log(total);\n"
        + "}";
}
