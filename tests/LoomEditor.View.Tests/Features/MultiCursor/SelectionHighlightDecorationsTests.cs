using LoomEditor.Core.Decorations;
using LoomEditor.View.Features.MultiCursor;

namespace LoomEditor.View.Tests.Features.MultiCursor;

// No upstream test file: new tests for the overview ruler parts of highlightDecorations.ts.
public class SelectionHighlightDecorationsTests
{
    [Fact]
    public void SelectionHighlight_Options_HaveTheRulerColor()
    {
        ModelDecorationOverviewRulerOptions ruler = SelectionHighlightDecorations.GetSelectionHighlightDecorationOptions(false).OverviewRuler!;

        Assert.Equal("editorOverviewRuler.selectionHighlightForeground", ruler.Color!.ThemeColorId);
        Assert.Equal(OverviewRulerLane.Center, ruler.Position);
        Assert.Null(SelectionHighlightDecorations.GetSelectionHighlightDecorationOptions(true).OverviewRuler);
    }
}
