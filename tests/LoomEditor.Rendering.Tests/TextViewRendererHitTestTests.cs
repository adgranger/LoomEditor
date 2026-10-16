using LoomEditor.Core.Text;
using LoomEditor.Rendering.Tests.TestHelpers;
using LoomEditor.View.Controller;

namespace LoomEditor.Rendering.Tests;

// Monaco has no test for hit testing on a canvas: all tests are new.
public class TextViewRendererHitTestTests
{
    private const double CharacterWidth = FakeTextMeasurer.CharacterWidth;

    [Theory]
    [InlineData("abc", 0)]
    [InlineData("abc", 3)]
    [InlineData("a\tb", 3)]
    [InlineData("a\tb", 1)]
    public void HitTest_AtACharacterX_ReturnsThatOffset(string text, int index)
    {
        var layout = new FakeLineLayout(text, CharacterWidth, 4);

        Assert.Equal(index, layout.HitTest(layout.GetCharacterX(index)));
    }

    [Fact]
    public void HitTest_BetweenTwoBoundaries_ReturnsTheNearestOne()
    {
        var layout = new FakeLineLayout("abcd", CharacterWidth, 4);

        Assert.Equal(1, layout.HitTest((CharacterWidth * 1) + (CharacterWidth * 0.3)));
        Assert.Equal(2, layout.HitTest((CharacterWidth * 1) + (CharacterWidth * 0.7)));
        Assert.Equal(4, layout.HitTest(10_000));
        Assert.Equal(0, layout.HitTest(-5));
    }

    [Fact]
    public void HitTest_SurrogatePair_NeverReturnsAnOffsetInsideIt()
    {
        var layout = new FakeLineLayout("a😀b", CharacterWidth, 4);

        for (double x = 0; x < 6 * CharacterWidth; x += 0.5)
        {
            int offset = layout.HitTest(x);
            Assert.NotEqual(2, offset);
        }
    }

    [Fact]
    public void GetPositionAtCoordinate_AgreesWithGetCharacterX()
    {
        using var harness = new ViewHarness("hello\nw\torld");
        ILineHitTester tester = harness.Renderer.CreateLineHitTester(harness.ViewModel);
        var layout = new FakeLineLayout("w\torld", CharacterWidth, 4);

        for (int index = 0; index <= 6; index++)
        {
            Position? position = tester.GetPositionAtCoordinate(2, layout.GetCharacterX(index));

            Assert.Equal(new Position(2, index + 1), position);
        }

        Assert.Null(tester.GetPositionAtCoordinate(3, 0));
    }

    [Fact]
    public void GetLineWidth_IsTheWidthOfTheLayout()
    {
        using var harness = new ViewHarness("hello\nw\torld");
        ILineHitTester tester = harness.Renderer.CreateLineHitTester(harness.ViewModel);

        Assert.Equal(5 * CharacterWidth, tester.GetLineWidth(1));
        Assert.Equal(new FakeLineLayout("w\torld", CharacterWidth, 4).Width, tester.GetLineWidth(2));
    }
}
