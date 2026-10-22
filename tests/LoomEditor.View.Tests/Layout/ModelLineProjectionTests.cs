// Ported from microsoft/vscode: src/vs/editor/common/viewModel/modelLineProjection.ts (IdentityModelLineProjection, HiddenModelLineProjection)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;
using static LoomEditor.View.Tests.Layout.TestHelpers.ProjectionTestHelpers;

namespace LoomEditor.View.Tests.Layout;

public class ModelLineProjectionTests
{
    private static TextModel CreateModel() => new("first line\n\tsecond", options: null);

    [Fact]
    public void IdentityProjection_ReturnsModelLine()
    {
        TextModel model = CreateModel();
        IModelLineProjection projection = ModelLineProjection.Create(isVisible: true);

        Assert.True(projection.IsVisible);
        Assert.Equal(1, projection.GetViewLineCount());
        Assert.Equal("\tsecond", projection.GetViewLineContent(model, 2, 0));
        Assert.Equal(7, projection.GetViewLineLength(model, 2, 0));
        Assert.Equal(1, projection.GetViewLineMinColumn(model, 2, 0));
        Assert.Equal(8, projection.GetViewLineMaxColumn(model, 2, 0));
        Assert.False(projection.GetViewLineContinuesWithWrappedLine(0));

        ViewLineData data = projection.GetViewLineData(model, 2, 0);

        Assert.Equal("\tsecond", data.Content);
        Assert.False(data.ContinuesWithWrappedLine);
        Assert.Equal(1, data.MinColumn);
        Assert.Equal(8, data.MaxColumn);
        Assert.Equal(0, data.StartVisibleColumn);
        Assert.True(data.Tokens.Equals(model.Tokenization.GetLineTokens(2).Inflate()));
    }

    [Fact]
    public void IdentityProjection_GetViewLinesData_FillsOnlyTheNeededLines()
    {
        TextModel model = CreateModel();
        ViewLineData?[] result = new ViewLineData?[3];

        ModelLineProjection.Create(true).GetViewLinesData(model, 1, 0, 1, 0, [false, true, false], result);
        ModelLineProjection.Create(true).GetViewLinesData(model, 2, 0, 1, 1, [false, true, false], result);

        Assert.Null(result[0]);
        Assert.Equal("\tsecond", result[1]!.Content);
        Assert.Null(result[2]);
    }

    [Fact]
    public void HiddenProjection_HasNoViewLine()
    {
        TextModel model = CreateModel();
        IModelLineProjection projection = ModelLineProjection.Create(isVisible: false);

        Assert.False(projection.IsVisible);
        Assert.Equal(0, projection.GetViewLineCount());
        Assert.Throws<InvalidOperationException>(() => projection.GetViewLineContent(model, 1, 0));
        Assert.Throws<InvalidOperationException>(() => projection.GetViewPositionOfModelPosition(1, 1));
    }

    [Fact]
    public void SetVisible_SwitchesBetweenTheTwoSingletons()
    {
        IModelLineProjection identity = ModelLineProjection.Create(true);
        IModelLineProjection hidden = ModelLineProjection.Create(false);

        Assert.Same(IdentityModelLineProjection.Instance, identity);
        Assert.Same(HiddenModelLineProjection.Instance, hidden);
        Assert.Same(identity, identity.SetVisible(true));
        Assert.Same(hidden, identity.SetVisible(false));
        Assert.Same(hidden, hidden.SetVisible(false));
        Assert.Same(identity, hidden.SetVisible(true));
    }

    [Fact]
    public void IdentityProjection_GetViewPositionOfModelPosition_KeepsTheColumn()
    {
        IModelLineProjection projection = ModelLineProjection.Create(true);

        Assert.Equal(new Position(7, 4), projection.GetViewPositionOfModelPosition(7, 4));
        Assert.Equal(7, projection.GetViewLineNumberOfModelPosition(7, 4));
        Assert.Equal(4, projection.GetModelColumnOfViewPosition(0, 4));
        Assert.Equal(new Position(2, 3), projection.NormalizePosition(0, new Position(2, 3), PositionAffinity.Right));
    }

    // Monaco: 'SplitLine'
    // The interface default affinity is Left, Monaco's class default is None: tests pass None.
    // Behavior difference: the view line number base is 1 (Monaco uses 0, which Position cannot hold), so every expected line is Monaco's + 1.
    [Fact]
    public void SplitLine()
    {
        TextModel model1 = new("My First LineMy Second LineAnd another one", options: null);
        IModelLineProjection line1 = CreateSplitLine([13, 14, 15], [13, 13 + 14, 13 + 14 + 15], 0);

        Assert.Equal(3, line1.GetViewLineCount());
        Assert.Equal("My First Line", line1.GetViewLineContent(model1, 1, 0));
        Assert.Equal("My Second Line", line1.GetViewLineContent(model1, 1, 1));
        Assert.Equal("And another one", line1.GetViewLineContent(model1, 1, 2));
        Assert.Equal(14, line1.GetViewLineMaxColumn(model1, 1, 0));
        Assert.Equal(15, line1.GetViewLineMaxColumn(model1, 1, 1));
        Assert.Equal(16, line1.GetViewLineMaxColumn(model1, 1, 2));
        Assert.Equal([true, true, false], Enumerable.Range(0, 3).Select(line1.GetViewLineContinuesWithWrappedLine));
        for (int col = 1; col <= 14; col++)
        {
            Assert.Equal(col, line1.GetModelColumnOfViewPosition(0, col));
        }

        for (int col = 1; col <= 15; col++)
        {
            Assert.Equal(13 + col, line1.GetModelColumnOfViewPosition(1, col));
        }

        for (int col = 1; col <= 16; col++)
        {
            Assert.Equal(13 + 14 + col, line1.GetModelColumnOfViewPosition(2, col));
        }

        for (int col = 1; col <= 13; col++)
        {
            Assert.Equal(new Position(1, col), line1.GetViewPositionOfModelPosition(1, col, PositionAffinity.None));
        }

        for (int col = 1 + 13; col <= 14 + 13; col++)
        {
            Assert.Equal(new Position(2, col - 13), line1.GetViewPositionOfModelPosition(1, col, PositionAffinity.None));
        }

        for (int col = 1 + 13 + 14; col <= 15 + 14 + 13; col++)
        {
            Assert.Equal(new Position(3, col - 13 - 14), line1.GetViewPositionOfModelPosition(1, col, PositionAffinity.None));
        }

        model1 = new TextModel("My First LineMy Second LineAnd another one", options: null);
        line1 = CreateSplitLine([13, 14, 15], [13, 13 + 14, 13 + 14 + 15], 4);

        Assert.Equal(3, line1.GetViewLineCount());
        Assert.Equal("My First Line", line1.GetViewLineContent(model1, 1, 0));
        Assert.Equal("    My Second Line", line1.GetViewLineContent(model1, 1, 1));
        Assert.Equal("    And another one", line1.GetViewLineContent(model1, 1, 2));
        Assert.Equal(14, line1.GetViewLineMaxColumn(model1, 1, 0));
        Assert.Equal(19, line1.GetViewLineMaxColumn(model1, 1, 1));
        Assert.Equal(20, line1.GetViewLineMaxColumn(model1, 1, 2));

        var actualViewColumnMapping = new List<int[]>();
        for (int lineIndex = 0; lineIndex < line1.GetViewLineCount(); lineIndex++)
        {
            var mapping = new List<int>();
            for (int col = 1; col <= line1.GetViewLineMaxColumn(model1, 1, lineIndex); col++)
            {
                mapping.Add(line1.GetModelColumnOfViewPosition(lineIndex, col));
            }

            actualViewColumnMapping.Add([.. mapping]);
        }

        Assert.Equal(
            [
                [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14],
                [14, 14, 14, 14, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28],
                [28, 28, 28, 28, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43],
            ],
            actualViewColumnMapping);

        for (int col = 1; col <= 13; col++)
        {
            Assert.Equal(new Position(1, col), line1.GetViewPositionOfModelPosition(1, col, PositionAffinity.None));
        }

        for (int col = 1 + 13; col <= 14 + 13; col++)
        {
            Assert.Equal(new Position(2, 4 + col - 13), line1.GetViewPositionOfModelPosition(1, col, PositionAffinity.None));
        }

        for (int col = 1 + 13 + 14; col <= 15 + 14 + 13; col++)
        {
            Assert.Equal(new Position(3, 4 + col - 13 - 14), line1.GetViewPositionOfModelPosition(1, col, PositionAffinity.None));
        }
    }

    [Fact]
    public void Create_WithData_ReturnsTheWrappedProjection()
    {
        ModelLineProjectionData data = CreateLineBreakData([3, 3], [3, 6], 0);

        IModelLineProjection projection = ModelLineProjection.Create(data, true);

        Assert.IsType<WrappedModelLineProjection>(projection);
        Assert.Same(data, projection.GetProjectionData());
        Assert.True(projection.IsVisible);
    }

    [Fact]
    public void Create_WithNullData_ReturnsIdentityOrHidden()
    {
        Assert.Same(IdentityModelLineProjection.Instance, ModelLineProjection.Create(null, true));
        Assert.Same(HiddenModelLineProjection.Instance, ModelLineProjection.Create(null, false));
        Assert.Null(IdentityModelLineProjection.Instance.GetProjectionData());
        Assert.Null(IdentityModelLineProjection.Instance.GetInjectedTextAt(0, 1));
    }

    [Fact]
    public void WrappedProjection_Hidden_ThrowsOnContent()
    {
        TextModel model = new("abcdef", options: null);
        IModelLineProjection projection = CreateSplitLine([3, 3], [3, 6], 0, isVisible: false);

        Assert.Throws<InvalidOperationException>(() => projection.GetViewLineContent(model, 1, 0));
        Assert.Throws<InvalidOperationException>(() => projection.GetViewLineMaxColumn(model, 1, 0));
        Assert.Throws<InvalidOperationException>(() => projection.GetModelColumnOfViewPosition(0, 1));
    }

    [Fact]
    public void WrappedProjection_Hidden_HasZeroViewLines()
    {
        IModelLineProjection projection = CreateSplitLine([3, 3], [3, 6], 0, isVisible: false);

        Assert.Equal(0, projection.GetViewLineCount());
        Assert.False(projection.GetViewLineContinuesWithWrappedLine(0));
        Assert.Equal(2, projection.SetVisible(true).GetViewLineCount());
    }

    [Fact]
    public void WrappedProjection_WithInjectedText_ContentIncludesIt()
    {
        TextModel model = new("abcdef", options: null);
        var options = ModelDecorationInjectedTextOptions.From(new InjectedTextOptions { Content = "XY" });
        var data = new ModelLineProjectionData([3], [options], [8], [8], 0);
        IModelLineProjection projection = ModelLineProjection.Create(data, true);

        Assert.Equal("abcXYdef", projection.GetViewLineContent(model, 1, 0));
        Assert.Equal(8, projection.GetViewLineLength(model, 1, 0));
    }

    [Fact]
    public void WrappedProjection_GetInjectedTextAt_ReturnsTheOptions()
    {
        var options = ModelDecorationInjectedTextOptions.From(new InjectedTextOptions { Content = "XY", InlineClassName = "c" });
        var data = new ModelLineProjectionData([3], [options], [8], [8], 0);
        IModelLineProjection projection = ModelLineProjection.Create(data, true);

        Assert.Same(options, projection.GetInjectedTextAt(0, 5)!.Options);
        Assert.Null(projection.GetInjectedTextAt(0, 2));
    }
}
