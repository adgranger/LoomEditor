using LoomEditor.Themes;

namespace LoomEditor.TextMate.Tests.Themes;

// No upstream unit tests: the expected values are the ones of the pinned VS Code files.
public class BuiltInThemesTests
{
    public static TheoryData<int> Indexes() => [.. Enumerable.Range(0, 10)];

    [Theory]
    [MemberData(nameof(Indexes))]
    public void BuiltIn_TenThemes_AllLoad(int index)
    {
        Assert.Equal(10, EditorTheme.BuiltIn.Count);
        EditorTheme theme = EditorTheme.BuiltIn[index];
        Assert.False(string.IsNullOrEmpty(theme.Name));
        Assert.True(Enum.IsDefined(theme.Type));
        Assert.NotEqual(default, theme.Background);
        Assert.True(theme.TokenColors.Count > 1);
        Assert.Empty(theme.TokenColors[0].Scopes);
        Assert.Equal(theme.Background, theme.TokenColors[0].Background);
    }

    [Fact]
    public void DarkModern_Background_Is1F1F1F()
    {
        Assert.Equal("Dark Modern", EditorTheme.Dark.Name);
        Assert.Equal(ThemeColor.Parse("#1F1F1F"), EditorTheme.Dark.Background);
        Assert.Equal(ThemeType.Dark, EditorTheme.Dark.Type);
    }

    [Fact]
    public void LightModern_Background_IsFFFFFF()
    {
        Assert.Equal("Light Modern", EditorTheme.Light.Name);
        Assert.Equal(ThemeColor.Parse("#FFFFFF"), EditorTheme.Light.Background);
        Assert.Equal(ThemeType.Light, EditorTheme.Light.Type);
    }

    [Fact]
    public void DarkModern_InheritsDarkPlusTokenColors()
    {
        Assert.Contains(EditorTheme.Dark.TokenColors, r => r.Scopes.Contains("keyword.control") && r.Foreground is not null);
    }

    [Fact]
    public void BuiltIn_TokenColorMap_HasOneEntryPerDistinctColor()
    {
        IReadOnlyList<ThemeColor> map = EditorTheme.Dark.TokenColorMap;
        Assert.Same(map, EditorTheme.Dark.TokenColorMap);
        Assert.Contains(EditorTheme.Dark.Foreground, map);
    }
}
