// Ported from microsoft/vscode: src/vs/editor/common/viewModel/modelLineProjection.ts (IdentityModelLineProjection, HiddenModelLineProjection)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

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
}
