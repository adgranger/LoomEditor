using System.Globalization;
using System.Text;
using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.TextMate.Grammars;
using LoomEditor.Themes;
using LoomEditor.View.Decorations;
using LoomEditor.View.Features;
using LoomEditor.View.Layout;
using TextRange = LoomEditor.Core.Text.Range;

namespace LoomEditor.Gallery;

/// <summary>The outcome of one check of <see cref="HostApiSelfCheck"/>.</summary>
/// <param name="Name">The name of the check.</param>
/// <param name="Passed">Whether it passed.</param>
/// <param name="Detail">What was measured, or why it failed.</param>
internal sealed record HostApiCheckResult(string Name, bool Passed, string Detail);

/// <summary>
/// Proves that the phase 13 host API logic (language by file name, host decorations, custom styles, sample data) works in the real app, the Native AOT build
/// included: <c>LoomEditor.Gallery.exe --hostapi-selfcheck=&lt;file&gt;</c> writes a PASS / FAIL report and exits 0 or 1. The WinUI members are not covered.
/// </summary>
/// <remarks>Threading: UI thread only (the checks create an editor context); no WinUI type.</remarks>
internal static class HostApiSelfCheck
{
    /// <summary>The names of the checks, in run order.</summary>
    public static IReadOnlyList<string> Names { get; } =
        (string[])["language", "decoration-options", "decorations-collection", "custom-style", "blame-data"];

    /// <summary>Runs every check; an exception is a failed check, never a crash.</summary>
    public static IReadOnlyList<HostApiCheckResult> Run()
    {
        List<HostApiCheckResult> results = [];

        void Check(string name, Func<string> check)
        {
            try
            {
                results.Add(new HostApiCheckResult(name, true, check()));
            }
            catch (Exception e)
            {
                results.Add(new HostApiCheckResult(name, false, e is CheckFailedException ? e.Message : $"{e.GetType().Name}: {e.Message}"));
            }
        }

        Check("language", Language);
        Check("decoration-options", DecorationOptions);
        Check("decorations-collection", DecorationsCollectionCheck);
        Check("custom-style", CustomStyle);
        Check("blame-data", BlameData);
        return results;
    }

    /// <summary>The report: <c>LoomEditor host API self-check</c>, one <c>PASS|FAIL name — detail</c> line per check, then <c>n of m checks passed</c>.</summary>
    public static string FormatReport(IReadOnlyList<HostApiCheckResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        StringBuilder report = new("LoomEditor host API self-check\n");
        foreach (HostApiCheckResult result in results)
        {
            report.Append(result.Passed ? "PASS" : "FAIL").Append(' ').Append(result.Name).Append(" — ").Append(result.Detail).Append('\n');
        }

        report.Append(CultureInfo.InvariantCulture, $"{results.Count(r => r.Passed)} of {results.Count} checks passed\n");
        return report.ToString();
    }

    /// <summary>Runs the checks, writes the report (UTF-8) and returns the number of failures.</summary>
    public static int RunToFile(string reportFile)
    {
        ArgumentNullException.ThrowIfNull(reportFile);

        IReadOnlyList<HostApiCheckResult> results = Run();
        File.WriteAllText(reportFile, FormatReport(results), new UTF8Encoding(false));
        return results.Count(r => !r.Passed);
    }

    private static void Require(bool condition, string failure)
    {
        if (!condition)
        {
            throw new CheckFailedException(failure);
        }
    }

    private static string Language()
    {
        string?[] actual =
        [
            BuiltInLanguages.GetLanguageIdByFileName("Program.cs"),
            BuiltInLanguages.GetLanguageIdByFileName("src/tsconfig.json"),
            BuiltInLanguages.GetLanguageIdByFileName(HostApiSamples.FileName),
            BuiltInLanguages.GetLanguageIdByFileName("Makefile"),
        ];
        string?[] expected = ["csharp", "jsonc", "csharp", null];
        for (int i = 0; i < expected.Length; i++)
        {
            Require(actual[i] == expected[i], $"file name #{i} gave '{actual[i] ?? "null"}', expected '{expected[i] ?? "null"}'");
        }

        return "Program.cs → csharp, tsconfig.json → jsonc, Conflicts.cs → csharp, Makefile → null";
    }

    private static string DecorationOptions()
    {
        var options = new EditorDecorationOptions
        {
            ClassName = "selfcheck",
            IsWholeLine = true,
            OverviewRulerColor = new ThemeColor(0x80, 0x12, 0x34, 0x56),
        };
        ModelDecorationOptions model = options.ToModelOptions();
        Require(model.ClassName == "selfcheck" && model.IsWholeLine, "class name or whole line lost");
        Require(model.Stickiness == TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges, $"stickiness {model.Stickiness}");
        Require(model.OverviewRuler?.Color?.Value == "#12345680", $"ruler color {model.OverviewRuler?.Color?.Value}");
        Require(ReferenceEquals(model, options.ToModelOptions()), "a second call built another instance");
        return "class, whole line, NeverGrowsWhenTypingAtEdges and ruler #12345680 convert; the instance is cached";
    }

    private static string DecorationsCollectionCheck()
    {
        var editor = new EditorContext();
        editor.SetText(HostApiSamples.Text);
        var collection = new DecorationsCollection(editor.CreateDecorationsCollection());
        var options = new EditorDecorationOptions { ClassName = "line-insert", IsWholeLine = true };
        collection.Set((EditorDecoration[])[new EditorDecoration(new TextRange(10, 1, 12, 1), options), new EditorDecoration(new TextRange(14, 1, 16, 1), options)]);
        Require(collection.Count == 2, $"count {collection.Count}");

        editor.Model.ApplyEdits((TextEdit[])[TextEdit.Insert(new Position(1, 1), "new\n")]);
        IReadOnlyList<TextRange> ranges = collection.GetRanges();
        Require(ranges.Count == 2 && ranges[0].StartLineNumber == 11 && ranges[1].StartLineNumber == 15, $"ranges after an insert at the top: {string.Join(' ', ranges)}");

        collection.Clear();
        Require(collection.Count == 0, "Clear left decorations");
        return "two conflict ranges set, both moved down by an inserted line, cleared";
    }

    private static string CustomStyle()
    {
        const string name = "selfcheck-style";
        ThemeColor light = ThemeColor.FromRgb(1, 2, 3);
        ThemeColor dark = ThemeColor.FromRgb(4, 5, 6);
        CustomDecorationStyles.Register(name, new CustomDecorationStyle { Background = light, DarkBackground = dark });
        try
        {
            DecorationStyle style = DecorationStyles.Get(name) ?? throw new CheckFailedException("the style is not resolved");
            Require(style.BackgroundResolver!(EditorTheme.Light) == light, "light background");
            Require(style.BackgroundResolver!(EditorTheme.Dark) == dark, "dark background");

            bool builtInRejected = false;
            try
            {
                CustomDecorationStyles.Register("line-insert", new CustomDecorationStyle());
            }
            catch (ArgumentException)
            {
                builtInRejected = true;
            }

            Require(builtInRejected, "a built-in name was accepted");
        }
        finally
        {
            Require(CustomDecorationStyles.Unregister(name), "the first Unregister returned false");
        }

        Require(!CustomDecorationStyles.Unregister(name), "the second Unregister returned true");
        return "light and dark backgrounds resolve; a built-in name throws; Unregister returns true then false";
    }

    private static string BlameData()
    {
        int lines = HostApiSamples.Text.Count(c => c == '\n') + 1;
        Require(lines >= 60, $"the sample has {lines} lines");
        for (int line = 1; line <= lines; line++)
        {
            (string commit, _, _) = HostApiSamples.Blame(line);
            Require(commit.Length > 0, $"no commit for line {line}");
            if (line > 1)
            {
                bool changed = commit != HostApiSamples.Blame(line - 1).Commit;
                Require(changed == ((line - 1) % 7 == 0), $"commit change at line {line}: {changed}");
            }
        }

        return $"{lines} lines have a commit; it changes every 7 lines";
    }

    private sealed class CheckFailedException(string message) : Exception(message);
}
