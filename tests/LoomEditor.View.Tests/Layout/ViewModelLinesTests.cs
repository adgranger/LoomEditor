// Ported from microsoft/vscode: src/vs/editor/test/browser/viewModel/modelLineProjection.test.ts (suite 'SplitLinesCollection', no wrapping)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

public class ViewModelLinesTests
{
    private const string LanguageId = "modelModeTest1";

    private static readonly string[] Lines =
    [
        "class Nice {",
        "\tfunction hi() {",
        "\t\tconsole.log(\"Hello world\");",
        "\t}",
        "\tfunction hello() {",
        "\t\tconsole.log(\"Hello world, this is a somewhat longer line\");",
        "\t}",
        "}",
    ];

    private static readonly IReadOnlyList<(int StartIndex, int Value)[]> Tokens =
    [
        [(0, 1), (5, 2), (6, 3), (10, 4)],
        [(0, 5), (1, 6), (9, 7), (10, 8), (12, 9)],
        [(0, 10), (2, 11), (9, 12), (10, 13), (13, 14), (14, 15), (27, 16)],
        [(0, 17)],
        [(0, 18), (1, 19), (9, 20), (10, 21), (15, 22)],
        [(0, 23), (2, 24), (9, 25), (10, 26), (13, 27), (14, 28), (59, 29)],
        [(0, 30)],
        [(0, 31)],
    ];

    // Monaco comments out a `break` to test all the combinations of needed lines; here it is a switch (a property: no constant folding).
    private static bool TestAllCombinations => false;

    private sealed record ExpectedLine(string Content, int MinColumn, int MaxColumn, (int EndIndex, int Value)[] Tokens);

    private static readonly ExpectedLine[] Expected =
    [
        new("class Nice {", 1, 13, [(5, 1), (6, 2), (10, 3), (12, 4)]),
        new("\tfunction hi() {", 1, 17, [(1, 5), (9, 6), (10, 7), (12, 8), (16, 9)]),
        new("\t\tconsole.log(\"Hello world\");", 1, 30, [(2, 10), (9, 11), (10, 12), (13, 13), (14, 14), (27, 15), (29, 16)]),
        new("\t}", 1, 3, [(2, 17)]),
        new("\tfunction hello() {", 1, 20, [(1, 18), (9, 19), (10, 20), (15, 21), (19, 22)]),
        new("\t\tconsole.log(\"Hello world, this is a somewhat longer line\");", 1, 62, [(2, 23), (9, 24), (10, 25), (13, 26), (14, 27), (59, 28), (61, 29)]),
        new("\t}", 1, 3, [(2, 30)]),
        new("}", 1, 2, [(1, 31)]),
    ];

    // Monaco: the suite's setup (the fixed-token tokenizer, forceTokenization) and withSplitLinesCollection with wordWrap 'off'
    private static void WithSplitLinesCollection(Action<TextModel, ViewModelLinesFromProjectedModel> callback)
    {
        using var services = new TestEditorServices();
        services.RegisterLanguage(LanguageId);
        services.Add(services.TokenizationRegistry.Register(LanguageId, new FixedTokensTokenizationSupport(Tokens)));
        TextModel model = services.CreateTextModel(string.Join('\n', Lines), LanguageId);

        // force tokenization
        model.Tokenization.ForceTokenization(model.LineCount);

        using var linesCollection = new ViewModelLinesFromProjectedModel(1, model, model.Options.TabSize);
        callback(model, linesCollection);
    }

    private static void AssertViewLineTokens(LoomEditor.Core.Tokens.IViewLineTokens actual, (int EndIndex, int Value)[] expected)
    {
        List<(int EndIndex, int Value)> tokens = [];
        for (int i = 0; i < actual.GetCount(); i++)
        {
            tokens.Add((actual.GetEndOffset(i), actual.GetForeground(i)));
        }

        Assert.Equal(expected, tokens);
    }

    private static void AssertMinimapLineRenderingData(ViewLineData? actual, ExpectedLine? expected)
    {
        if (actual is null && expected is null)
        {
            return;
        }

        Assert.NotNull(actual);
        Assert.NotNull(expected);
        Assert.Equal(expected.Content, actual.Content);
        Assert.Equal(expected.MinColumn, actual.MinColumn);
        Assert.Equal(expected.MaxColumn, actual.MaxColumn);
        AssertViewLineTokens(actual.Tokens, expected.Tokens);
    }

    private static void AssertAllMinimapLinesRenderingData(ViewModelLinesFromProjectedModel splitLinesCollection, ExpectedLine[] all)
    {
        int lineCount = all.Length;
        for (int line = 1; line <= lineCount; line++)
        {
            Assert.Equal(splitLinesCollection.GetViewLineContent(line), splitLinesCollection.GetViewLineData(line).Content);
        }

        for (int start = 1; start <= lineCount; start++)
        {
            for (int end = start; end <= lineCount; end++)
            {
                int count = end - start + 1;
                for (int desired = (1 << count) - 1; desired >= 0; desired--)
                {
                    bool[] needed = new bool[count];
                    ExpectedLine?[] expected = new ExpectedLine?[count];
                    for (int i = 0; i < count; i++)
                    {
                        needed[i] = (desired & (1 << i)) != 0;
                        expected[i] = needed[i] ? all[start - 1 + i] : null;
                    }

                    ViewLineData?[] actual = splitLinesCollection.GetViewLinesData(start, end, needed);

                    Assert.Equal(expected.Length, actual.Length);
                    for (int i = 0; i < expected.Length; i++)
                    {
                        AssertMinimapLineRenderingData(actual[i], expected[i]);
                    }

                    if (!TestAllCombinations)
                    {
                        break;
                    }
                }
            }
        }
    }

    // Monaco: 'getViewLinesData - no wrapping'
    [Fact]
    public void GetViewLinesData_NoWrapping() => WithSplitLinesCollection((_, splitLinesCollection) =>
    {
        Assert.Equal(8, splitLinesCollection.GetViewLineCount());
        for (int line = 1; line <= 8; line++)
        {
            Assert.True(splitLinesCollection.ModelPositionIsVisible(line, 1));
        }

        AssertAllMinimapLinesRenderingData(splitLinesCollection, Expected);

        splitLinesCollection.SetHiddenAreas([new Range(2, 1, 4, 1)]);
        Assert.Equal(5, splitLinesCollection.GetViewLineCount());
        bool[] visible = [true, false, false, false, true, true, true, true];
        for (int line = 1; line <= 8; line++)
        {
            Assert.Equal(visible[line - 1], splitLinesCollection.ModelPositionIsVisible(line, 1));
        }

        AssertAllMinimapLinesRenderingData(splitLinesCollection, [Expected[0], Expected[4], Expected[5], Expected[6], Expected[7]]);
    });

    [Fact]
    public void SetHiddenAreas_SameRangesTwice_ReturnsFalse() => WithSplitLinesCollection((_, lines) =>
    {
        Assert.True(lines.SetHiddenAreas([new Range(2, 1, 4, 1)]));
        Assert.False(lines.SetHiddenAreas([new Range(2, 3, 4, 2)]));
        Assert.Equal([new Range(2, 1, 4, 1)], lines.GetHiddenAreas());
    });

    [Fact]
    public void SetHiddenAreas_EverythingHidden_RevealsEverything() => WithSplitLinesCollection((_, lines) =>
    {
        // Monaco: "Cannot have everything be hidden => reveal everything!"
        Assert.True(lines.SetHiddenAreas([new Range(1, 1, 8, 1)]));

        Assert.Equal(8, lines.GetViewLineCount());
        Assert.Empty(lines.GetHiddenAreas());
    });

    [Fact]
    public void OnModelLinesInserted_AfterAHiddenLine_InsertsHiddenLines() => WithSplitLinesCollection((_, lines) =>
    {
        lines.SetHiddenAreas([new Range(2, 1, 4, 1)]);

        ViewLineRange? inserted = lines.OnModelLinesInserted(100, 5, 6, new ModelLineProjectionData?[2]);

        Assert.Equal(new ViewLineRange(2, 1), inserted);
        Assert.Equal(5, lines.GetViewLineCount());
        Assert.False(lines.ModelPositionIsVisible(5, 1));
        Assert.False(lines.ModelPositionIsVisible(6, 1));
        Assert.True(lines.ModelPositionIsVisible(7, 1));
    });

    [Fact]
    public void OnModelLinesDeleted_StaleVersionId_IsIgnored() => WithSplitLinesCollection((model, lines) =>
    {
        Assert.Null(lines.OnModelLinesDeleted(model.VersionId, 2, 3));
        Assert.Null(lines.OnModelLinesDeleted(null, 2, 3));
        Assert.Null(lines.OnModelLinesDeleted(0, 2, 3));
        Assert.Equal(8, lines.GetViewLineCount());

        Assert.Equal(new ViewLineRange(2, 3), lines.OnModelLinesDeleted(model.VersionId + 1, 2, 3));
        Assert.Equal(6, lines.GetViewLineCount());
    });

    [Fact]
    public void AcceptVersionId_AllLinesHidden_ShowsTheFirstLine() => WithSplitLinesCollection((model, lines) =>
    {
        lines.SetHiddenAreas([new Range(2, 1, 8, 1)]);
        lines.OnModelLinesDeleted(model.VersionId + 1, 1, 1);
        Assert.Equal(0, lines.GetViewLineCount());

        lines.AcceptVersionId(model.VersionId + 1);

        Assert.Equal(1, lines.GetViewLineCount());
        Assert.True(lines.ModelPositionIsVisible(1, 1));
    });

    [Fact]
    public void NormalizeLineRanges_MergesOverlappingAndTouchingRanges()
    {
        List<Range> result = LineRanges.Normalize(
            [new Range(5, 3, 6, 2), new Range(1, 4, 2, 1), new Range(3, 1, 3, 9), new Range(8, 1, 9, 1), new Range(6, 1, 7, 7)]);

        Assert.Equal([new Range(1, 1, 3, 1), new Range(5, 1, 9, 1)], result);
        Assert.Empty(LineRanges.Normalize([]));
    }
}
