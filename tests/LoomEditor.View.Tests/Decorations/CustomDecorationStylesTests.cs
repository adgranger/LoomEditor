using LoomEditor.Themes;
using LoomEditor.View.Decorations;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Decorations;

// The host table is global and xUnit runs classes in parallel: every test uses its own class name and unregisters it.
public class CustomDecorationStylesTests
{
    private static readonly ThemeColor Light = ThemeColor.FromRgb(1, 2, 3);
    private static readonly ThemeColor Dark = ThemeColor.FromRgb(4, 5, 6);

    private static EditorTheme Theme(string uiTheme) => EditorTheme.Load("{ \"colors\": {} }", uiTheme, "Test");

    [Fact]
    public void Register_BackgroundWithDarkColor_ResolvesPerThemeType()
    {
        const string name = "test-styles-pick";
        CustomDecorationStyles.Register(name, new CustomDecorationStyle { Background = Light, DarkBackground = Dark });
        try
        {
            DecorationStyle style = DecorationStyles.Get(name)!;

            Assert.Equal(Light, style.BackgroundResolver!(Theme("vs")));
            Assert.Equal(Dark, style.BackgroundResolver!(Theme("vs-dark")));
            Assert.Equal(Dark, style.BackgroundResolver!(Theme("hc-black")));
            Assert.Null(style.BorderResolver);
            Assert.Null(style.ForegroundResolver);
        }
        finally
        {
            CustomDecorationStyles.Unregister(name);
        }
    }

    [Fact]
    public void Register_NoDarkColor_FallsBackToTheLightColorInADarkTheme()
    {
        const string name = "test-styles-fallback";
        CustomDecorationStyles.Register(name, new CustomDecorationStyle { Foreground = Light });
        try
        {
            Assert.Equal(Light, DecorationStyles.Get(name)!.ForegroundResolver!(Theme("vs-dark")));
        }
        finally
        {
            CustomDecorationStyles.Unregister(name);
        }
    }

    [Fact]
    public void Register_BorderWidths_AreCopiedToTheInternalStyle()
    {
        const string name = "test-styles-widths";
        CustomDecorationStyles.Register(name, new CustomDecorationStyle { Border = Light, BorderWidth = 2, LeftBorderWidth = 3 });
        try
        {
            DecorationStyle style = DecorationStyles.Get(name)!;

            Assert.Equal(2f, style.BorderWidth);
            Assert.Equal(3f, style.LeftBorderWidth);
        }
        finally
        {
            CustomDecorationStyles.Unregister(name);
        }
    }

    [Fact]
    public void Get_ClassList_FindsTheHostClass()
    {
        const string name = "test-styles-list";
        CustomDecorationStyles.Register(name, new CustomDecorationStyle { Background = Light });
        try
        {
            Assert.NotNull(DecorationStyles.Get("foo " + name));
        }
        finally
        {
            CustomDecorationStyles.Unregister(name);
        }
    }

    [Fact]
    public void Register_Twice_ReplacesTheStyle()
    {
        const string name = "test-styles-replace";
        CustomDecorationStyles.Register(name, new CustomDecorationStyle { Background = Light });
        CustomDecorationStyles.Register(name, new CustomDecorationStyle { Background = Dark });
        try
        {
            Assert.Equal(Dark, DecorationStyles.Get(name)!.BackgroundResolver!(Theme("vs")));
        }
        finally
        {
            CustomDecorationStyles.Unregister(name);
        }
    }

    [Fact]
    public void Unregister_Registered_ReturnsTrueOnceThenFalse()
    {
        const string name = "test-styles-unregister";
        CustomDecorationStyles.Register(name, new CustomDecorationStyle { Background = Light });

        Assert.True(CustomDecorationStyles.Unregister(name));
        Assert.Null(DecorationStyles.Get(name));
        Assert.False(CustomDecorationStyles.Unregister(name));
    }

    [Fact]
    public void Register_BuiltInName_ThrowsAndKeepsTheBuiltInStyle()
    {
        DecorationStyle? builtIn = DecorationStyles.Get("rangeHighlight");

        Assert.Throws<ArgumentException>("className", () => CustomDecorationStyles.Register("rangeHighlight", new CustomDecorationStyle()));

        Assert.Same(builtIn, DecorationStyles.Get("rangeHighlight"));
        Assert.False(CustomDecorationStyles.Unregister("rangeHighlight"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a b")]
    [InlineData("é")]
    public void Register_InvalidName_Throws(string name)
    {
        Assert.Throws<ArgumentException>("className", () => CustomDecorationStyles.Register(name, new CustomDecorationStyle()));
    }

    [Fact]
    public void Register_NegativeOrNonFiniteWidth_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CustomDecorationStyles.Register("test-styles-negative", new CustomDecorationStyle { BorderWidth = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => CustomDecorationStyles.Register("test-styles-nan", new CustomDecorationStyle { LeftBorderWidth = double.NaN }));
    }

    [Fact]
    public void Register_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => CustomDecorationStyles.Register(null!, new CustomDecorationStyle()));
        Assert.Throws<ArgumentNullException>(() => CustomDecorationStyles.Register("test-styles-null", null!));
        Assert.Throws<ArgumentNullException>(() => CustomDecorationStyles.Unregister(null!));
    }
}
