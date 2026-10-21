// Ported from microsoft/vscode: src/vs/editor/contrib/folding/test/browser/indentRangeProvider.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.
using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Features.Folding;

public class IndentRangeProviderTests
{
    private readonly record struct ExpectedIndentRange(int StartLineNumber, int EndLineNumber, int ParentIndex);

    // The `marker` argument of Monaco's `r` is not used by its assertions: it is dropped.
    private static ExpectedIndentRange R(int startLineNumber, int endLineNumber, int parentIndex, bool marker = false) =>
        new(startLineNumber, endLineNumber, parentIndex);

    private static void AssertRanges(string[] lines, ExpectedIndentRange[] expected, bool offside, FoldingMarkers? markers = null)
    {
        var model = new TextModel(string.Join('\n', lines));
        FoldingRegions actual = IndentRanges.ComputeRanges(model, offside, markers);

        var actualRanges = new List<ExpectedIndentRange>();
        for (int i = 0; i < actual.Length; i++)
        {
            actualRanges.Add(R(actual.GetStartLineNumber(i), actual.GetEndLineNumber(i), actual.GetParentIndex(i)));
        }

        Assert.Equal(expected, actualRanges);
    }

    private static readonly FoldingMarkers Markers = new(new Regex(@"^\s*#region\b"), new Regex(@"^\s*#endregion\b"));

    // Monaco: 'Indentation Folding' › 'Fold one level'
    [Fact]
    public void FoldOneLevel()
    {
        string[] range = ["A", "  A", "  A", "  A"];
        AssertRanges(range, [R(1, 4, -1)], true);
        AssertRanges(range, [R(1, 4, -1)], false);
    }

    // Monaco: 'Indentation Folding' › 'Fold two levels'
    [Fact]
    public void FoldTwoLevels()
    {
        string[] range = ["A", "  A", "  A", "    A", "    A"];
        AssertRanges(range, [R(1, 5, -1), R(3, 5, 0)], true);
        AssertRanges(range, [R(1, 5, -1), R(3, 5, 0)], false);
    }

    // Monaco: 'Indentation Folding' › 'Fold three levels'
    [Fact]
    public void FoldThreeLevels()
    {
        string[] range = ["A", "  A", "    A", "      A", "A"];
        AssertRanges(range, [R(1, 4, -1), R(2, 4, 0), R(3, 4, 1)], true);
        AssertRanges(range, [R(1, 4, -1), R(2, 4, 0), R(3, 4, 1)], false);
    }

    // Monaco: 'Indentation Folding' › 'Fold decreasing indent'
    [Fact]
    public void FoldDecreasingIndent()
    {
        string[] range = ["    A", "  A", "A"];
        AssertRanges(range, [], true);
        AssertRanges(range, [], false);
    }

    // Monaco: 'Indentation Folding' › 'Fold Java'
    [Fact]
    public void FoldJava()
    {
        AssertRanges(
            [
                "class A {",
                "  void foo() {",
                "    console.log();",
                "    console.log();",
                "  }",
                "",
                "  void bar() {",
                "    console.log();",
                "  }",
                "}",
                "interface B {",
                "  void bar();",
                "}",
            ],
            [R(1, 9, -1), R(2, 4, 0), R(7, 8, 0), R(11, 12, -1)],
            false);
    }

    // Monaco: 'Indentation Folding' › 'Fold Javadoc'
    [Fact]
    public void FoldJavadoc()
    {
        AssertRanges(["/**", " * Comment", " */", "class A {", "  void foo() {", "  }", "}"], [R(1, 3, -1), R(4, 6, -1)], false);
    }

    // Monaco: 'Indentation Folding' › 'Fold Whitespace Java'
    [Fact]
    public void FoldWhitespaceJava()
    {
        AssertRanges(
            ["class A {", "", "  void foo() {", "     ", "     return 0;", "  }", "      ", "}"],
            [R(1, 7, -1), R(3, 5, 0)],
            false);
    }

    // Monaco: 'Indentation Folding' › 'Fold Whitespace Python'
    [Fact]
    public void FoldWhitespacePython()
    {
        AssertRanges(
            ["def a:", "  pass", "   ", "  def b:", "    pass", "  ", "      ", "def c: # since there was a deintent here"],
            [R(1, 5, -1), R(4, 5, 0)],
            true);
    }

    // Monaco: 'Indentation Folding' › 'Fold Tabs'
    [Fact]
    public void FoldTabs()
    {
        AssertRanges(
            ["class A {", "\t\t", "\tvoid foo() {", "\t \t//hello", "\t    return 0;", "  \t}", "      ", "}"],
            [R(1, 7, -1), R(3, 5, 0)],
            false);
    }

    // Monaco: 'Folding with regions' › 'Inside region, indented'
    [Fact]
    public void InsideRegionIndented()
    {
        AssertRanges(
            ["class A {", "  #region", "  void foo() {", "     ", "     return 0;", "  }", "  #endregion", "}"],
            [R(1, 7, -1), R(2, 7, 0, true), R(3, 5, 1)],
            false,
            Markers);
    }

    // Monaco: 'Folding with regions' › 'Inside region, not indented'
    [Fact]
    public void InsideRegionNotIndented()
    {
        AssertRanges(
            ["var x;", "#region", "void foo() {", "     ", "     return 0;", "  }", "#endregion", ""],
            [R(2, 7, -1, true), R(3, 6, 0)],
            false,
            Markers);
    }

    // Monaco: 'Folding with regions' › 'Empty Regions'
    [Fact]
    public void EmptyRegions()
    {
        AssertRanges(
            ["var x;", "#region", "#endregion", "#region", "", "#endregion", "var y;"],
            [R(2, 3, -1, true), R(4, 6, -1, true)],
            false,
            Markers);
    }

    // Monaco: 'Folding with regions' › 'Nested Regions'
    [Fact]
    public void NestedRegions()
    {
        AssertRanges(
            ["var x;", "#region", "#region", "", "#endregion", "#endregion", "var y;"],
            [R(2, 6, -1, true), R(3, 5, 0, true)],
            false,
            Markers);
    }

    // Monaco: 'Folding with regions' › 'Nested Regions 2'
    [Fact]
    public void NestedRegions2()
    {
        AssertRanges(
            ["class A {", "  #region", "", "  #region", "", "  #endregion", "  // comment", "  #endregion", "}"],
            [R(1, 8, -1), R(2, 8, 0, true), R(4, 6, 1, true)],
            false,
            Markers);
    }

    // Monaco: 'Folding with regions' › 'Incomplete Regions'
    [Fact]
    public void IncompleteRegions()
    {
        AssertRanges(["class A {", "#region", "  // comment", "}"], [R(2, 3, -1)], false, Markers);
    }

    // Monaco: 'Folding with regions' › 'Incomplete Regions 2'
    [Fact]
    public void IncompleteRegions2()
    {
        AssertRanges(
            ["", "#region", "#region", "#region", "  // comment", "#endregion", "#endregion", " // hello"],
            [R(3, 7, -1, true), R(4, 6, 0, true)],
            false,
            Markers);
    }

    // Monaco: 'Folding with regions' › 'Indented region before'
    [Fact]
    public void IndentedRegionBefore()
    {
        AssertRanges(
            ["if (x)", "  return;", "", "#region", "  // comment", "#endregion"],
            [R(1, 3, -1), R(4, 6, -1, true)],
            false,
            Markers);
    }

    // Monaco: 'Folding with regions' › 'Indented region before 2'
    [Fact]
    public void IndentedRegionBefore2()
    {
        AssertRanges(
            ["if (x)", "  log();", "", "    #region", "      // comment", "    #endregion"],
            [R(1, 6, -1), R(2, 6, 0), R(4, 6, 1, true)],
            false,
            Markers);
    }

    // Monaco: 'Folding with regions' › 'Indented region in-between'
    [Fact]
    public void IndentedRegionInBetween()
    {
        AssertRanges(
            ["#region", "  // comment", "  if (x)", "    return;", "", "#endregion"],
            [R(1, 6, -1, true), R(3, 5, 0)],
            false,
            Markers);
    }

    // Monaco: 'Folding with regions' › 'Indented region after'
    [Fact]
    public void IndentedRegionAfter()
    {
        AssertRanges(
            ["#region", "  // comment", "", "#endregion", "  if (x)", "    return;"],
            [R(1, 4, -1, true), R(5, 6, -1)],
            false,
            Markers);
    }

    // Monaco: 'Folding with regions' › 'With off-side'
    [Fact]
    public void WithOffSide()
    {
        AssertRanges(["#region", "  ", "", "#endregion", ""], [R(1, 4, -1, true)], true, Markers);
    }

    // Monaco: 'Folding with regions' › 'Nested with off-side'
    [Fact]
    public void NestedWithOffSide()
    {
        AssertRanges(
            ["#region", "  ", "#region", "", "#endregion", "", "#endregion", ""],
            [R(1, 7, -1, true), R(3, 5, 0, true)],
            true,
            Markers);
    }

    // Monaco: 'Folding with regions' › 'Issue 35981'
    [Fact]
    public void Issue35981()
    {
        AssertRanges(
            [
                "function thisFoldsToEndOfPage() {",
                "  const variable = []",
                "    // #region",
                "    .reduce((a, b) => a,[]);",
                "}",
                "",
                "function thisFoldsProperly() {",
                "  const foo = \"bar\"",
                "}",
            ],
            [R(1, 4, -1), R(2, 4, 0), R(7, 8, -1)],
            false,
            Markers);
    }

    // Monaco: 'Folding with regions' › 'Misspelled Markers'
    [Fact]
    public void MisspelledMarkers()
    {
        AssertRanges(
            ["#Region", "#endregion", "#regionsandmore", "#endregion", "#region", "#end region", "#region", "#endregionff"],
            [],
            true,
            Markers);
    }

    // Monaco: 'Folding with regions' › 'Issue 79359'
    [Fact]
    public void Issue79359()
    {
        AssertRanges(
            ["#region", "", "class A", "  foo", "", "class A", "  foo", "", "#endregion"],
            [R(1, 9, -1, true), R(3, 4, 0), R(6, 7, 0)],
            true,
            Markers);
    }

    // Monaco: 'Folding with regions' › 'Markers with stateful flags'
    // JavaScript's `g` flag has no .NET equivalent and is dropped; `i` is RegexOptions.IgnoreCase, so the "different options"
    // path is taken exactly where Monaco's flags differ.
    [Fact]
    public void MarkersWithStatefulFlags()
    {
        AssertRanges(
            ["#region", "content", "#endregion"],
            [R(1, 3, -1, true)],
            false,
            new FoldingMarkers(new Regex(@"^\s*#region\b"), new Regex(@"^\s*#endregion\b")));

        AssertRanges(
            ["#REGION", "content", "#endregion"],
            [R(1, 3, -1, true)],
            false,
            new FoldingMarkers(new Regex(@"^\s*#region\b", RegexOptions.IgnoreCase), new Regex(@"^\s*#endregion\b")));

        AssertRanges(
            ["#REGION", "content", "#ENDREGION"],
            [],
            false,
            new FoldingMarkers(new Regex(@"^\s*#region\b", RegexOptions.IgnoreCase), new Regex(@"^\s*#endregion\b")));

        AssertRanges(
            ["#REGION", "content", "#ENDREGION"],
            [],
            false,
            new FoldingMarkers(new Regex(@"^\s*#region\b"), new Regex(@"^\s*#endregion\b", RegexOptions.IgnoreCase)));
    }

    [Fact]
    public void IndentRangeProvider_UsesTheLanguageFoldingRules()
    {
        using var services = new TestEditorServices();
        services.RegisterLanguage("regions");
        services.Add(services.LanguageConfigurationRegistry.Register("regions", new LanguageConfiguration { Folding = new FoldingRules(false, Markers) }));
        TextModel model = services.CreateTextModel("#region\ncontent\n#endregion", "regions");
        using var provider = new IndentRangeProvider(model, services.LanguageConfigurationRegistry, new TestLimitReporter());

        FoldingRegions? regions = provider.Compute();

        Assert.NotNull(regions);
        Assert.Equal("indent", provider.Id);
        Assert.Equal(1, regions.Length);
        Assert.Equal(1, regions.GetStartLineNumber(0));
        Assert.Equal(3, regions.GetEndLineNumber(0));
    }

    private sealed class TestLimitReporter : IFoldingLimitReporter
    {
        public int Limit => 5000;

        public void Update(int computed, int? limited)
        {
        }
    }
}