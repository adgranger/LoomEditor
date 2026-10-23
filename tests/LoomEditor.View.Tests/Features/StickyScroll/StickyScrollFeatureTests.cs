using LoomEditor.View.Features;
using LoomEditor.View.Features.StickyScroll;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.Features.TestHelpers;

namespace LoomEditor.View.Tests.Features.StickyScroll;

// New tests: the scenarios of Monaco's 'issue #157180' and 'issue #159271' (stickyScroll.test.ts), read with the indentation
// model instead of the outline model. The expected lines were recomputed: the indentation model sticks the line that opens
// each indented block, so the nested blocks below stick 'class A {' (line 1) and '  void f() {' (line 2).
public class StickyScrollFeatureTests
{
    // 1: class A {   2:   void f() {   3-32: body of f   33:   }   34:   void g() {   35-60: body of g   61:   }   62: }
    private static readonly string Text = string.Join('\n',
        ["class A {", "  void f() {", .. Enumerable.Range(1, 30).Select(i => $"    f{i}();"), "  }", "  void g() {",
         .. Enumerable.Range(1, 26).Select(i => $"    g{i}();"), "  }", "}", "last"]);

    private readonly ManualEditorScheduler _scheduler = new();

    private (EditorContext Context, StickyScrollFeature Feature, int LineHeight) Create(EditorStickyScrollOptions? options = null)
    {
        var context = new EditorContext(_scheduler);
        context.SetText(Text);
        int lineHeight = context.ViewModel.FontInfo.LineHeight;
        EditorLayoutOptions layout = context.ViewModel.LayoutOptions with { StickyScroll = options ?? new EditorStickyScrollOptions() };
        context.ViewModel.SetOptions(layout, context.ViewModel.ViewLayoutOptions);
        context.ViewModel.SetViewportSize(600, 20 * lineHeight);
        var feature = new StickyScrollFeature();
        feature.Attach(context);
        _scheduler.AdvanceBy(TimeSpan.FromSeconds(1));
        return (context, feature, lineHeight);
    }

    private static void ScrollToLine(EditorContext context, int lineNumber, int lineHeight) =>
        context.ViewModel.ViewLayout.SetScrollPosition(new NewScrollPosition(null, (lineNumber - 1) * lineHeight), ScrollType.Immediate);

    [Fact]
    public void FindScrollWidgetState_AtTop_NoStickyLine()
    {
        (_, StickyScrollFeature feature, _) = Create();

        Assert.Empty(feature.FindScrollWidgetState().StartLineNumbers);
    }

    [Fact]
    public void FindScrollWidgetState_InsideABlock_StickTheBlockStart()
    {
        (EditorContext context, StickyScrollFeature feature, int lineHeight) = Create(new EditorStickyScrollOptions { MaxLineCount = 1 });

        ScrollToLine(context, 10, lineHeight);

        Assert.Equal([1], feature.FindScrollWidgetState().StartLineNumbers);
    }

    [Fact]
    public void FindScrollWidgetState_NestedBlocks_StickBoth()
    {
        (EditorContext context, StickyScrollFeature feature, int lineHeight) = Create();

        ScrollToLine(context, 10, lineHeight);

        Assert.Equal([1, 2], feature.FindScrollWidgetState().StartLineNumbers);
        Assert.Equal(0, feature.FindScrollWidgetState().LastLineRelativePosition);
    }

    [Fact]
    public void FindScrollWidgetState_LastLinePushedUp_HasANegativeRelativePosition()
    {
        (EditorContext context, StickyScrollFeature feature, int lineHeight) = Create();

        // Line 32 is the last line of f: the sticky line 2 is pushed up by the end of its block.
        ScrollToLine(context, 32, lineHeight);

        StickyScrollWidgetState state = feature.FindScrollWidgetState();
        Assert.Equal([1, 2], state.StartLineNumbers);
        Assert.True(state.LastLineRelativePosition < 0);
    }

    [Fact]
    public void FindScrollWidgetState_MaxLineCount_IsRespected()
    {
        (EditorContext context, StickyScrollFeature feature, int lineHeight) = Create(new EditorStickyScrollOptions { MaxLineCount = 1 });

        ScrollToLine(context, 10, lineHeight);

        Assert.Single(feature.FindScrollWidgetState().StartLineNumbers);
    }

    [Fact]
    public void Scroll_PublishesTheRenderData()
    {
        (EditorContext context, StickyScrollFeature feature, int lineHeight) = Create();
        Assert.Null(context.ViewModel.StickyScroll);

        ScrollToLine(context, 10, lineHeight);

        Assert.Equal([1, 2], context.ViewModel.StickyScroll!.LineNumbers);
        Assert.Equal(feature.WidgetState.StartLineNumbers, context.ViewModel.StickyScroll.LineNumbers);
    }

    [Fact]
    public void Disabled_PublishesNull()
    {
        (EditorContext context, StickyScrollFeature feature, int lineHeight) = Create();
        ScrollToLine(context, 10, lineHeight);
        Assert.NotNull(context.ViewModel.StickyScroll);

        context.ViewModel.SetOptions(
            context.ViewModel.LayoutOptions with { StickyScroll = new EditorStickyScrollOptions { Enabled = false } },
            context.ViewModel.ViewLayoutOptions);
        ScrollToLine(context, 12, lineHeight);

        Assert.Null(context.ViewModel.StickyScroll);
        Assert.Empty(feature.WidgetState.StartLineNumbers);
    }
}
