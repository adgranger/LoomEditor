using LoomEditor.Core.Decorations;
using LoomEditor.Themes;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

// Monaco has no tests for these: all tests are new.
public class OverviewRulerDecorationsTests
{
    private static readonly EditorTheme Dark = EditorTheme.Load("{ \"colors\": { \"editorError.foreground\": \"#112233\" } }", "vs-dark", "Test");
    private static readonly EditorTheme Light = EditorTheme.Load("{ \"colors\": { \"editorError.foreground\": \"#445566\" } }", "vs", "Test");

    private static ModelDeltaDecoration Mark(int startLine, int endLine, string color, OverviewRulerLane lane = OverviewRulerLane.Center, string? darkColor = null) =>
        new(
            new Range(startLine, 1, endLine, 1),
            ModelDecorationOptions.CreateDynamic(new ModelDecorationOptionsData
            {
                Description = "test",
                OverviewRuler = new ModelDecorationOverviewRulerOptions(
                    DecorationColor.FromValue(color),
                    darkColor is null ? null : DecorationColor.FromValue(darkColor),
                    lane),
            }));

    private static void With(Action<TestCodeEditor> callback, params ModelDeltaDecoration[] decorations) =>
        TestCodeEditor.WithTestCodeEditor("a\nb\nc\nd\ne\nf", editor =>
        {
            editor.Model.DeltaDecorations(0, [], decorations);
            callback(editor);
        });

    [Fact]
    public void GetAllOverviewRulerDecorations_GroupsByColor()
    {
        With(
            editor =>
            {
                List<OverviewRulerDecorationsGroup> groups = editor.ViewModel.GetAllOverviewRulerDecorations(Dark);

                Assert.Equal(2, groups.Count);
                Assert.Equal(ThemeColor.Parse("#FF0000"), groups[0].Color);
                Assert.Equal(ThemeColor.Parse("#00FF00"), groups[1].Color);
            },
            Mark(1, 1, "#FF0000"),
            Mark(3, 3, "#00FF00"),
            Mark(5, 5, "#FF0000"));
    }

    [Fact]
    public void GetAllOverviewRulerDecorations_MergesAdjacentRangesOfTheSameLane()
    {
        With(
            editor => Assert.Equal([2, 1, 3, 2, 5, 5], editor.ViewModel.GetAllOverviewRulerDecorations(Dark).Single().Data),
            Mark(1, 2, "#FF0000"),
            Mark(3, 3, "#FF0000"),
            Mark(5, 5, "#FF0000"));
    }

    [Fact]
    public void GetAllOverviewRulerDecorations_DoesNotMergeDifferentLanes()
    {
        With(
            editor => Assert.Equal([2, 1, 1, 4, 2, 2], editor.ViewModel.GetAllOverviewRulerDecorations(Dark).Single().Data),
            Mark(1, 1, "#FF0000"),
            Mark(2, 2, "#FF0000", OverviewRulerLane.Right));
    }

    [Fact]
    public void GetAllOverviewRulerDecorations_UsesViewLineNumbers()
    {
        With(
            editor =>
            {
                editor.ViewModel.SetHiddenAreas([new Range(1, 1, 2, 1)]);

                // Model lines 4–5 are view lines 2–3 once model lines 1–2 are hidden.
                Assert.Equal([2, 2, 3], editor.ViewModel.GetAllOverviewRulerDecorations(Dark).Single().Data);
            },
            Mark(4, 5, "#FF0000"));
    }

    [Fact]
    public void DecorationColorResolver_DarkTheme_PrefersDarkColor()
    {
        var color = DecorationColor.FromValue("#FF0000");
        var darkColor = DecorationColor.FromValue("#00FF00");

        Assert.Equal(ThemeColor.Parse("#00FF00"), DecorationColorResolver.Resolve(color, darkColor, Dark));
        Assert.Equal(ThemeColor.Parse("#FF0000"), DecorationColorResolver.Resolve(color, darkColor, Light));
        Assert.Equal(ThemeColor.Parse("#FF0000"), DecorationColorResolver.Resolve(color, null, Dark));
    }

    [Fact]
    public void DecorationColorResolver_ThemeColorId_IsResolved()
    {
        var color = DecorationColor.FromThemeColor("editorError.foreground");

        Assert.Equal(ThemeColor.Parse("#112233"), DecorationColorResolver.Resolve(color, null, Dark));
        Assert.Equal(ThemeColor.Parse("#445566"), DecorationColorResolver.Resolve(color, null, Light));
        Assert.Null(DecorationColorResolver.Resolve(DecorationColor.FromThemeColor("no.such.color"), null, Dark));
        Assert.Null(DecorationColorResolver.Resolve(null, null, Dark));
    }

    [Fact]
    public void CompareByRenderingProps_OrdersByZIndexThenColor()
    {
        var low = new OverviewRulerDecorationsGroup(ThemeColor.Parse("#FFFFFF"), 0, []);
        var high = new OverviewRulerDecorationsGroup(ThemeColor.Parse("#000000"), 1, []);
        var darker = new OverviewRulerDecorationsGroup(ThemeColor.Parse("#000000"), 0, []);

        Assert.True(OverviewRulerDecorationsGroup.CompareByRenderingProps(low, high) < 0);
        Assert.True(OverviewRulerDecorationsGroup.CompareByRenderingProps(low, darker) > 0);
        Assert.Equal(0, OverviewRulerDecorationsGroup.CompareByRenderingProps(low, low));
    }
}
