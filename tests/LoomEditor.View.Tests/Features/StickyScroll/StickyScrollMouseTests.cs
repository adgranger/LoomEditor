using LoomEditor.Core.Text;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Features.StickyScroll;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Features.TestHelpers;

namespace LoomEditor.View.Tests.Features.StickyScroll;

// New tests: stickyScrollController.ts click listener has no upstream test.
public class StickyScrollMouseTests
{
    // 1: class A {   2:   void f() {   3-32: body of f   33:   }   34-61: g   62: }   63: last
    private static readonly string Text = string.Join('\n',
        ["class A {", "  void f() {", .. Enumerable.Range(1, 30).Select(i => $"    f{i}();"), "  }", "  void g() {",
         .. Enumerable.Range(1, 26).Select(i => $"    g{i}();"), "  }", "}", "last"]);

    private readonly ManualEditorScheduler _scheduler = new();

    private (EditorContext Context, StickyScrollFeature Feature, int LineHeight) Create()
    {
        var context = new EditorContext(_scheduler);
        context.SetText(Text);
        int lineHeight = context.ViewModel.FontInfo.LineHeight;
        context.ViewModel.SetViewportSize(600, 20 * lineHeight);
        var folding = new FoldingFeature();
        folding.Attach(context);
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1));
        var feature = new StickyScrollFeature();
        feature.Attach(context);
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1));
        context.ViewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(null, 9 * lineHeight), ScrollType.Immediate);
        return (context, feature, lineHeight);
    }

    [Fact]
    public void Click_OnContent_RevealsAndSelectsThePosition()
    {
        (EditorContext context, StickyScrollFeature feature, _) = Create();

        feature.HandleClick(1, StickyScrollHitPart.Content, 8, shift: false);

        Assert.Equal(new Position(2, 8), context.Selection.Position);
        Assert.True(context.Selection.IsEmpty);
    }

    [Fact]
    public void Click_OnTheLineNumber_SelectsColumnOne()
    {
        (EditorContext context, StickyScrollFeature feature, _) = Create();

        feature.HandleClick(1, StickyScrollHitPart.LineNumberGutter, null, shift: false);

        Assert.Equal(new Position(2, 1), context.Selection.Position);
    }

    [Fact]
    public void ShiftClick_RevealsTheEndOfTheScope()
    {
        (EditorContext context, StickyScrollFeature feature, _) = Create();

        feature.HandleClick(1, StickyScrollHitPart.Content, 3, shift: true);

        Assert.Equal(new Position(feature.WidgetState.EndLineNumbers[1], 1), context.Selection.Position);
    }

    [Fact]
    public void CtrlClick_DoesNothing()
    {
        (EditorContext context, StickyScrollFeature feature, _) = Create();
        Selection before = context.Selection;

        feature.HandleClick(1, StickyScrollHitPart.Content, 8, shift: false, otherModifier: true);

        Assert.Equal(before, context.Selection);
    }

    [Fact]
    public void Click_OnChevron_TogglesTheFoldAndAdjustsScrollTop()
    {
        (EditorContext context, StickyScrollFeature feature, _) = Create();
        FoldingModel folding = feature.FoldingModel!;
        Assert.False(folding.GetRegionAtLine(2)!.IsCollapsed);

        feature.HandleClick(1, StickyScrollHitPart.FoldingIcon, null, shift: false);

        Assert.True(folding.GetRegionAtLine(2)!.IsCollapsed);
        // The collapsed region ends on view line 2, which is the second sticky line: its top, minus one line, plus one.
        Assert.Equal(1, context.ViewModel.ViewLayout.GetCurrentScrollTop());
    }
}
