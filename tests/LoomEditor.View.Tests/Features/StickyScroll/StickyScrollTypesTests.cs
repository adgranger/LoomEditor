using LoomEditor.View.Features.StickyScroll;

namespace LoomEditor.View.Tests.Features.StickyScroll;

// New tests: these types are plain data in Monaco and have no upstream test.
public class StickyScrollTypesTests
{
    [Fact]
    public void Options_Defaults_MatchMonaco()
    {
        var options = new EditorStickyScrollOptions();

        Assert.True(options.Enabled);
        Assert.Equal(5, options.MaxLineCount);
        Assert.Equal(StickyScrollModel.OutlineModel, options.DefaultModel);
        Assert.True(options.ScrollWithEditor);
    }

    [Theory]
    [InlineData(-3, 1)]
    [InlineData(0, 1)]
    [InlineData(7, 7)]
    [InlineData(20, 20)]
    [InlineData(99, 20)]
    public void Options_Validated_ClampsMaxLineCount(int input, int expected)
    {
        var validated = new EditorStickyScrollOptions { MaxLineCount = input }.Validated();

        Assert.Equal(expected, validated.MaxLineCount);
    }

    [Fact]
    public void WidgetState_Equals_ComparesContent()
    {
        var state = new StickyScrollWidgetState([1, 5], [10, 8], 3, 5);

        Assert.True(state.Equals(new StickyScrollWidgetState([1, 5], [10, 8], 3, 5)));
        Assert.False(state.Equals(new StickyScrollWidgetState([1, 6], [10, 8], 3, 5)));
        Assert.False(state.Equals(new StickyScrollWidgetState([1, 5], [10, 9], 3, 5)));
        Assert.False(state.Equals(new StickyScrollWidgetState([1, 5], [10, 8], 4, 5)));
        Assert.False(state.Equals(new StickyScrollWidgetState([1, 5], [10, 8], 3, null)));
        Assert.False(state.Equals(null));
        Assert.True(StickyScrollWidgetState.Empty.Equals(StickyScrollWidgetState.Empty));
    }
}
