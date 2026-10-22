using LoomEditor.Core.Decorations;
using LoomEditor.View.Features.WordHighlighter;

namespace LoomEditor.View.Tests.Features.WordHighlighter;

// No upstream test file: new tests for the overview ruler parts of highlightDecorations.ts.
public class HighlightDecorationsTests
{
    [Theory]
    [InlineData(-1, "editorOverviewRuler.wordHighlightForeground")]
    [InlineData((int)DocumentHighlightKind.Read, "editorOverviewRuler.wordHighlightForeground")]
    [InlineData((int)DocumentHighlightKind.Text, "editorOverviewRuler.wordHighlightTextForeground")]
    [InlineData((int)DocumentHighlightKind.Write, "editorOverviewRuler.wordHighlightStrongForeground")]
    public void WordHighlight_Options_HaveTheRulerColor(int kind, string colorId)
    {
        ModelDecorationOverviewRulerOptions ruler = HighlightDecorations.GetHighlightDecorationOptions(kind < 0 ? null : (DocumentHighlightKind)kind).OverviewRuler!;

        Assert.Equal(colorId, ruler.Color!.ThemeColorId);
        Assert.Equal(colorId, ruler.DarkColor!.ThemeColorId);
        Assert.Equal(OverviewRulerLane.Center, ruler.Position);
    }
}
