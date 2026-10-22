// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelLines.ts (ViewModelLinesFromProjectedModel: indent guides)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Guides;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Layout.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

public class ViewModelLinesGuidesTests
{
    private static readonly BracketGuideOptions AllVertical = new(true, HorizontalGuidesState.Disabled, false);
    private static readonly BracketGuideOptions AllHorizontal = new(true, HorizontalGuidesState.Enabled, false);

    private static TextModel CreateBracketModel(string text)
    {
        var codec = new LanguageIdCodec();
        codec.Register("brackets");
        var configurations = new LanguageConfigurationRegistry();
        configurations.Register("brackets", new LanguageConfiguration { Brackets = [new("{", "}"), new("(", ")")] });
        var model = new TextModel(text, DefaultEndOfLine.LF, null, new TokenizationRegistry(), codec, null, configurations);
        model.Tokenization.SetLanguageId("brackets");
        model.UpdateOptions(new TextModelUpdateOptions(TabSize: 4, IndentSize: 4));
        return model;
    }

    private static readonly MonospaceLineBreaksComputerFactory Monospace = new("", "");

    private static ViewModelLinesFromProjectedModel Create(TextModel model, int wrappingColumn, WrappingIndent indent = WrappingIndent.None) =>
        new(1, model, Monospace, FontInfo.Default, 4, wrappingColumn, indent, WordBreak.Normal, false, false);

    [Fact]
    public void GetViewLinesIndentGuides_WrappedLine_RepeatsTheGuidesOnContinuations()
    {
        TextModel model = new("{\n\taaaaaaaaaabbbbbbbbbb\n}", options: null);
        using ViewModelLinesFromProjectedModel lines = Create(model, 10, WrappingIndent.Same);

        int viewLineCount = lines.GetViewLineCount();
        Assert.True(viewLineCount > 3);
        int[] expected = [0, .. Enumerable.Repeat(1, viewLineCount - 2), 0];
        Assert.Equal(expected, lines.GetViewLinesIndentGuides(1, viewLineCount));
    }

    [Fact]
    public void GetViewLinesIndentGuides_HiddenLines_AreSkipped()
    {
        TextModel model = new("{\n\ta\n\tb\n\tc\n}", options: null);
        using ViewModelLinesFromProjectedModel lines = Create(model, -1);
        lines.SetHiddenAreas([new Range(2, 1, 3, 1)]);

        Assert.Equal(3, lines.GetViewLineCount());
        Assert.Equal([0, 1, 0], lines.GetViewLinesIndentGuides(1, 3));
    }

    [Fact]
    public void GetActiveIndentGuide_ReturnsViewLineNumbers()
    {
        TextModel model = new("aaaaaaaaaabbbbbbbbbb\n{\n\ta\n\tb\n}", options: null);
        using ViewModelLinesFromProjectedModel lines = Create(model, 10);

        Assert.Equal(6, lines.GetViewLineCount());
        ActiveIndentGuideInfo expectedModel = model.Guides.GetActiveIndentGuide(3, 1, 5);
        ActiveIndentGuideInfo actual = lines.GetActiveIndentGuide(4, 1, 6);

        Assert.Equal(expectedModel.StartLineNumber + 1, actual.StartLineNumber);
        Assert.Equal(expectedModel.EndLineNumber + 1, actual.EndLineNumber);
        Assert.Equal(expectedModel.Indent, actual.Indent);
    }

    [Fact]
    public void GetViewLinesBracketGuides_WrappedLine_KeepsTheGuidesOfTheRightSegment()
    {
        TextModel model = CreateBracketModel("{aaaaaaaa\n  b\n}");
        using ViewModelLinesFromProjectedModel lines = Create(model, 4);

        Assert.Equal(5, lines.GetViewLineCount());
        List<IndentGuide>[] guides = lines.GetViewLinesBracketGuides(1, 5, null, AllVertical);

        // The pair opens in the first segment: its guide shows on the continuation segments only.
        Assert.Equal([0, 1, 1, 1, 0], guides.Select(g => g.Count));
    }

    [Fact]
    public void GetViewLinesBracketGuides_ColumnGuide_IsTranslatedToTheViewColumn()
    {
        TextModel model = CreateBracketModel("{ aaaaaaaa }");
        using ViewModelLinesFromProjectedModel lines = Create(model, 6);

        Assert.Equal(2, lines.GetViewLineCount());
        IndentGuide modelGuide = Assert.Single(model.Guides.GetLinesBracketGuides(1, 1, null, AllHorizontal)[0]);
        Assert.Equal(2, modelGuide.Column);

        List<IndentGuide>[] guides = lines.GetViewLinesBracketGuides(1, 2, null, AllHorizontal);

        // First segment: the column stays, the line ends at the end of the segment's text.
        IndentGuide first = Assert.Single(guides[0]);
        Assert.Equal(2, first.Column);
        Assert.Equal(lines.GetViewLineMaxColumn(1), first.HorizontalLine!.EndColumn);
        // Continuation: the column moves to the segment start, the end is the view column of the closing bracket.
        IndentGuide second = Assert.Single(guides[1]);
        Assert.Equal(lines.GetViewLineMinColumn(2), second.Column);
        Assert.Equal(lines.ConvertModelPositionToViewPosition(1, modelGuide.HorizontalLine!.EndColumn).Column, second.HorizontalLine!.EndColumn);
    }

    [Fact]
    public void ViewModel_GuideQueries_DelegateToTheLines()
    {
        TextModel model = CreateBracketModel("{\n  a\n}");
        using ViewModel viewModel = new(model, FontInfo.Default, new EditorLayoutOptions(), new ViewLayoutOptions(20), new FakeFrameScheduler().Schedule, new FakeTimeProvider(), 0);

        Assert.Equal([0, 1, 0], viewModel.GetLinesIndentGuides(1, 3));
        Assert.Equal(model.Guides.GetActiveIndentGuide(2, 1, 3).Indent, viewModel.GetActiveIndentGuide(2, 1, 3).Indent);
        List<IndentGuide>[] guides = viewModel.GetBracketGuidesInRangeByLine(1, 3, null, AllVertical);
        Assert.Equal([0, 1, 0], guides.Select(g => g.Count));
    }
}
