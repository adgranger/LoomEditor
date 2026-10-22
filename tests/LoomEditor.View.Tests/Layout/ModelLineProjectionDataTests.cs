using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

public class ModelLineProjectionDataTests
{
    private static ModelLineProjectionData Wrapped(int indent = 0) => new(null, null, [5, 9], [], indent);

    private static ModelLineProjectionData WithInjection() =>
        new([2], [ModelDecorationInjectedTextOptions.From(new InjectedTextOptions { Content = "XYZ" })], [20], [], 0);

    [Fact]
    public void GetOutputLineCount_IsTheNumberOfBreakOffsets() => Assert.Equal(2, Wrapped().GetOutputLineCount());

    [Fact]
    public void GetLineLength_WrappedLine_AddsTheIndent()
    {
        ModelLineProjectionData data = Wrapped(2);

        Assert.Equal(5, data.GetLineLength(0));
        Assert.Equal(6, data.GetLineLength(1));
        Assert.Equal(6, data.GetMaxOutputOffset(1));
    }

    [Fact]
    public void GetMinOutputOffset_FirstLineIsZero_OthersAreTheIndent()
    {
        ModelLineProjectionData data = Wrapped(2);

        Assert.Equal(0, data.GetMinOutputOffset(0));
        Assert.Equal(2, data.GetMinOutputOffset(1));
    }

    [Fact]
    public void TranslateToInputOffset_InsideTheIndent_ClampsToTheLineStart() =>
        Assert.Equal(5, Wrapped(3).TranslateToInputOffset(1, 1));

    [Fact]
    public void TranslateToInputOffset_InsideInjectedText_ReturnsTheInjectionOffset() =>
        Assert.Equal(2, WithInjection().TranslateToInputOffset(0, 4));

    [Fact]
    public void TranslateToInputOffset_AfterInjectedText_SubtractsItsLength() =>
        Assert.Equal(5, WithInjection().TranslateToInputOffset(0, 8));

    [Fact]
    public void TranslateToOutputPosition_AtABreak_LeftStaysRightMoves()
    {
        ModelLineProjectionData data = Wrapped(2);

        Assert.Equal(new OutputPosition(0, 5), data.TranslateToOutputPosition(5, PositionAffinity.Left));
        Assert.Equal(new OutputPosition(1, 2), data.TranslateToOutputPosition(5, PositionAffinity.None));
        Assert.Equal(new OutputPosition(1, 2), data.TranslateToOutputPosition(5, PositionAffinity.Right));
    }

    [Fact]
    public void TranslateToOutputPosition_AtAnInjection_RightSkipsTheInjectedText()
    {
        ModelLineProjectionData data = WithInjection();

        Assert.Equal(2, data.TranslateToOutputPosition(2, PositionAffinity.None).OutputOffset);
        Assert.Equal(5, data.TranslateToOutputPosition(2, PositionAffinity.Right).OutputOffset);
    }

    [Fact]
    public void OutputPosition_ToPositionAndToString()
    {
        var position = new OutputPosition(2, 7);

        Assert.Equal(new Position(12, 8), position.ToPosition(10));
        Assert.Equal("2:7", position.ToString());
    }
}
