// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelLines.ts (ViewModelLinesFromProjectedModel: indent guides)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Guides;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

public class ViewModelLinesGuidesTests
{
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
}
