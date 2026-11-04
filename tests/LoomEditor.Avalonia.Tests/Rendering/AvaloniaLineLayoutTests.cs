using LoomEditor.Avalonia.Rendering;
using LoomEditor.Avalonia.Tests.TestHelpers;
using LoomEditor.Rendering;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Tests.Rendering;

// The twin of the Win2D line layout: no upstream test, all tests are new.
public class AvaloniaLineLayoutTests
{
    [Fact]
    public async Task Width_TrailingSpaces_Counted()
    {
        await HeadlessSession.RunAsync(() =>
        {
            using AvaloniaTextMeasurer measurer = TestFont.CreateMeasurer();
            using ILineLayout bare = measurer.CreateLineLayout("ab", LineStyle.Plain);
            using ILineLayout spaces = measurer.CreateLineLayout("ab  ", LineStyle.Plain);

            Assert.Equal(bare.Width + (2 * measurer.FontInfo.SpaceWidth), spaces.Width, 3);
        });
    }

    [Fact]
    public async Task Tab_AtColumn0_AdvancesOneTabStop()
    {
        await HeadlessSession.RunAsync(() =>
        {
            using AvaloniaTextMeasurer measurer = TestFont.CreateMeasurer(tabSize: 4);
            using ILineLayout layout = measurer.CreateLineLayout("\tx", LineStyle.Plain);

            Assert.Equal(4 * measurer.FontInfo.SpaceWidth, layout.GetCharacterX(1), 3);
        });
    }

    [Fact]
    public async Task Tab_AfterTwoChars_AdvancesToNextStop()
    {
        await HeadlessSession.RunAsync(() =>
        {
            using AvaloniaTextMeasurer measurer = TestFont.CreateMeasurer(tabSize: 4);
            using ILineLayout layout = measurer.CreateLineLayout("ab\tx", LineStyle.Plain);

            Assert.Equal(4 * measurer.FontInfo.SpaceWidth, layout.GetCharacterX(3), 3);
        });
    }

    [Theory]
    [InlineData("hello world")]
    [InlineData("日本語")]
    [InlineData("a👍b")]
    public async Task GetCharacterX_IsMonotonic(string text)
    {
        await HeadlessSession.RunAsync(() =>
        {
            using AvaloniaTextMeasurer measurer = TestFont.CreateMeasurer();
            using ILineLayout layout = measurer.CreateLineLayout(text, LineStyle.Plain);

            Assert.Equal(0, layout.GetCharacterX(0));
            for (int i = 1; i <= text.Length; i++)
            {
                Assert.True(layout.GetCharacterX(i) >= layout.GetCharacterX(i - 1), $"x({i}) < x({i - 1}) in \"{text}\"");
            }

            Assert.Equal(layout.Width, layout.GetCharacterX(text.Length));
            Assert.True(layout.Width > 0);
        });
    }

    [Fact]
    public async Task HitTest_LeftAndRightHalf_OfAChar()
    {
        await HeadlessSession.RunAsync(() =>
        {
            using AvaloniaTextMeasurer measurer = TestFont.CreateMeasurer();
            using ILineLayout layout = measurer.CreateLineLayout("abc", LineStyle.Plain);
            double left = layout.GetCharacterX(1);
            double right = layout.GetCharacterX(2);

            Assert.Equal(1, layout.HitTest(left + ((right - left) * 0.25)));
            Assert.Equal(2, layout.HitTest(left + ((right - left) * 0.75)));
            Assert.Equal(0, layout.HitTest(-10));
            Assert.Equal(3, layout.HitTest(layout.Width + 50));
        });
    }

    [Fact]
    public async Task HitTest_InsideLigature_PicksNearestOffset()
    {
        await HeadlessSession.RunAsync(() =>
        {
            using AvaloniaTextMeasurer measurer = TestFont.CreateMeasurer(fontFeatureSettings: EditorFontOptions.LigaturesOn);
            using ILineLayout layout = measurer.CreateLineLayout("a=>b", LineStyle.Plain);
            double start = layout.GetCharacterX(1);
            double end = layout.GetCharacterX(3);

            Assert.Equal(1, layout.HitTest(start + ((end - start) * 0.2)));
            Assert.Equal(2, layout.HitTest(start + ((end - start) * 0.5)));
            Assert.Equal(3, layout.HitTest(start + ((end - start) * 0.9)));
        });
    }

    [Fact]
    public async Task HitTest_Emoji_NeverSplitsSurrogates()
    {
        await HeadlessSession.RunAsync(() =>
        {
            using AvaloniaTextMeasurer measurer = TestFont.CreateMeasurer();
            const string text = "a👍b";
            using ILineLayout layout = measurer.CreateLineLayout(text, LineStyle.Plain);

            for (double x = -2; x <= layout.Width + 2; x += 0.5)
            {
                int index = layout.HitTest(x);
                Assert.InRange(index, 0, text.Length);
                Assert.NotEqual(2, index);
            }
        });
    }

    [Fact]
    public async Task EmptyLine_HasZeroWidth()
    {
        await HeadlessSession.RunAsync(() =>
        {
            using AvaloniaTextMeasurer measurer = TestFont.CreateMeasurer();
            using ILineLayout layout = measurer.CreateLineLayout(string.Empty, LineStyle.Plain);

            Assert.Equal(0, layout.Width);
            Assert.Equal(0, layout.GetCharacterX(0));
            Assert.Equal(0, layout.HitTest(42));
        });
    }
}
