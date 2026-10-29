using System.Globalization;
using System.Text;
using LoomEditor.Core.Markers;
using LoomEditor.Core.Snippets;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features;
using LoomEditor.View.Features.GotoError;
using LoomEditor.View.Features.Snippet;
using LoomEditor.View.Features.SmartSelect;
using LoomEditor.View.Layout;

namespace LoomEditor.Gallery;

/// <summary>The outcome of one check of <see cref="TextSelfCheck"/>.</summary>
/// <param name="Name">The name of the check.</param>
/// <param name="Passed">Whether it passed.</param>
/// <param name="Detail">What was measured, or why it failed.</param>
internal sealed record TextCheckResult(string Name, bool Passed, string Detail);

/// <summary>
/// Proves that the phase 10 logic (snippets, whitespace, control characters, font features, zoom, overtype, smart select, problems) works in
/// the real app, the Native AOT build included: <c>LoomEditor.Gallery.exe --text-selfcheck=&lt;file&gt;</c> writes a PASS / FAIL report and
/// exits 0 or 1. The main reason is <c>snippet-transform</c>: a <c>Regex</c> built at run time.
/// </summary>
/// <remarks>Threading: UI thread only (the checks create editor contexts); no WinUI type.</remarks>
internal static class TextSelfCheck
{
    private const string ForLoop = "for (${1:i} = 0; $1 < ${2:n}; $1++) {\n\t$0\n}";

    /// <summary>The names of the checks, in run order.</summary>
    public static IReadOnlyList<string> Names { get; } =
        (string[])["snippet-parse", "snippet-transform", "snippet-insert", "whitespace", "control-characters", "font-features", "zoom", "overtype", "smart-select", "marker-list"];

    /// <summary>Runs every check; an exception is a failed check, never a crash.</summary>
    public static IReadOnlyList<TextCheckResult> Run()
    {
        List<TextCheckResult> results = [];

        void Check(string name, Func<string> check)
        {
            try
            {
                results.Add(new TextCheckResult(name, true, check()));
            }
            catch (Exception e)
            {
                results.Add(new TextCheckResult(name, false, e is CheckFailedException ? e.Message : $"{e.GetType().Name}: {e.Message}"));
            }
        }

        Check("snippet-parse", SnippetParse);
        Check("snippet-transform", SnippetTransform);
        Check("snippet-insert", SnippetInsert);
        Check("whitespace", Whitespace);
        Check("control-characters", ControlCharactersCheck);
        Check("font-features", FontFeatures);
        Check("zoom", Zoom);
        Check("overtype", Overtype);
        Check("smart-select", SmartSelect);
        Check("marker-list", MarkerListCheck);
        return results;
    }

    /// <summary>The report: <c>LoomEditor text self-check</c>, one <c>PASS|FAIL name — detail</c> line per check, then <c>n of m checks passed</c>.</summary>
    public static string FormatReport(IReadOnlyList<TextCheckResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        StringBuilder report = new("LoomEditor text self-check\n");
        foreach (TextCheckResult result in results)
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

        IReadOnlyList<TextCheckResult> results = Run();
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

    private static string SnippetParse()
    {
        TextmateSnippet snippet = new SnippetParser().Parse(ForLoop, true);
        int indices = snippet.Placeholders.Select(placeholder => placeholder.Index).Distinct().Count();

        Require(indices == 3, $"{indices} distinct placeholder indices, expected 3");
        Require(snippet.Placeholders.Any(placeholder => placeholder.IsFinalTabstop), "no final tabstop");
        return $"{indices} distinct indices and a final tabstop";
    }

    // A variable with a regex transform: the Regex is built at run time (the Native AOT check).
    private static string SnippetTransform()
    {
        TextmateSnippet snippet = new SnippetParser().Parse("${TM_FILENAME/(.*)\\..+$/$1/}", true);
        snippet.ResolveVariables(new CompositeVariableResolver(new ModelBasedVariableResolver(new FakeHost("C:\\src\\main.cs"))));

        string text = snippet.ToString();
        Require(text == "main", $"resolved to '{text}', expected 'main'");
        return $"TM_FILENAME of main.cs resolves to '{text}'";
    }

    private static string SnippetInsert()
    {
        using var context = new EditorContext();
        using var feature = new SnippetFeature();
        feature.Attach(context);
        context.SetText(string.Empty);
        context.SetSelections(new Selection[] { new Selection(1, 1, 1, 1) });
        SnippetController controller = context.SnippetController ?? throw new CheckFailedException("no snippet controller");

        controller.Insert("a${1:b}-${2:c}$0");
        Require(context.Model.GetValue() == "ab-c", $"text '{context.Model.GetValue()}', expected 'ab-c'");
        Require(context.Selection == new Selection(1, 2, 1, 3), $"first tab stop {context.Selection}");

        controller.Next();
        Require(context.Selection == new Selection(1, 4, 1, 5), $"second tab stop {context.Selection}");

        controller.Next();
        Require(context.Selection == Selection.Collapsed(new Position(1, 5)) && !controller.IsInSnippet, $"final tab stop {context.Selection}");
        return "text 'ab-c', Next visits the two placeholders then the final tab stop";
    }

    private static string Whitespace()
    {
        const string Line = "\ta  b ";
        IReadOnlyList<int> all = RenderWhitespaceComputer.Compute(Line, Line.Length, 0, RenderWhitespace.All, false, null);
        IReadOnlyList<int> boundary = RenderWhitespaceComputer.Compute(Line, Line.Length, 0, RenderWhitespace.Boundary, false, null);

        Require(string.Join(',', all) == "0,2,3,5", $"All gave [{string.Join(',', all)}], expected [0,2,3,5]");
        Require(boundary.Count > 0 && boundary.Count <= all.Count && boundary.All(all.Contains), $"Boundary gave [{string.Join(',', boundary)}]");
        return $"All [{string.Join(',', all)}], Boundary [{string.Join(',', boundary)}]";
    }

    private static string ControlCharactersCheck()
    {
        string substituted = ControlCharacters.Substitute("a\u0001b\u007Fc", true);

        Require(substituted == "a\u2401b\u2421c", "control pictures expected");
        Require(ControlCharacters.Substitute("a\u0001b", false) == "a\u0001b", "the characters must stay when the option is off");
        return "U+0001 and U+007F become their control pictures";
    }

    private static string FontFeatures()
    {
        IReadOnlyList<FontFeature> features = FontFeatureSettings.Parse("'liga' 1, \"calt\" 0");

        Require(features is [{ Tag: "liga", Value: 1 }, { Tag: "calt", Value: 0 }], "expected liga=1 and calt=0");
        return "'liga' 1 and \"calt\" 0 parsed";
    }

    private static string Zoom()
    {
        var zoom = new EditorZoomState();
        zoom.SetLevel(100);
        Require(zoom.Level == 20, $"max level {zoom.Level}, expected 20");
        zoom.SetLevel(-100);
        Require(zoom.Level == -5, $"min level {zoom.Level}, expected -5");
        return "clamped to [-5, 20]";
    }

    private static string Overtype()
    {
        using var context = new EditorContext();
        context.SetText("123");
        context.SetSelections(new Selection[] { new Selection(1, 1, 1, 1) });
        context.InputMode = EditorInputMode.Overtype;

        context.ViewModel.Type("a", "keyboard");

        Require(context.Model.GetValue() == "a23", $"text '{context.Model.GetValue()}', expected 'a23'");
        return "typing 'a' over '123' gives 'a23'";
    }

    private static string SmartSelect()
    {
        using var context = new EditorContext();
        context.SetText("foo(bar)");

        List<Range> ranges = SmartSelectRanges.Provide(context.Model, new Position[] { new(1, 6) }, new SmartSelectOptions())[0];

        Require(ranges.Count >= 2, $"{ranges.Count} range(s), expected at least 2");
        Require(ranges[0] == new Range(1, 5, 1, 8), $"first range {ranges[0]}, expected the word 'bar'");
        Require(ranges[1] == new Range(1, 4, 1, 9), $"second range {ranges[1]}, expected '(bar)'");
        return "the word 'bar', then '(bar)'";
    }

    private static string MarkerListCheck()
    {
        using var context = new EditorContext();
        context.SetText("a\nb\nc\nd");
        context.Markers.Change("selfcheck", new MarkerData[] {
            new MarkerData(MarkerSeverity.Error, "three", 3, 1, 3, 2),
            new MarkerData(MarkerSeverity.Error, "two", 2, 1, 2, 2),
            new MarkerData(MarkerSeverity.Error, "four", 4, 1, 4, 2),
        });
        using var list = new MarkerList(context.Markers, MarkerSortOrder.Position);

        bool moved = list.Move(true, context.Model, new Position(1, 1));

        Require(moved && list.Selected is { Index: 1, Total: 3 } selected && selected.Marker.Message == "two", "the first move must select 'two' (1 of 3)");
        return "next from the start selects 'two' (1 of 3)";
    }

    private sealed class CheckFailedException(string message) : Exception(message);

    private sealed class FakeHost(string filePath) : ISnippetVariableHost
    {
        public string? FilePath => filePath;

        public string? RelativeFilePath => null;

        public string? WorkspaceName => null;

        public string? WorkspaceFolder => null;

        public string? ReadClipboardText() => null;
    }
}
