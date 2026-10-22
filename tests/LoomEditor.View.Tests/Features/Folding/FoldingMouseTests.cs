using LoomEditor.Core.Text;
using LoomEditor.View.Controller;
using LoomEditor.View.Features;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Tests.Features.TestHelpers;

namespace LoomEditor.View.Tests.Features.Folding;

// New tests: the mouse handlers of folding.ts have no upstream test.
public class FoldingMouseTests
{
    private static readonly TimeSpan FirstDelay = TimeSpan.FromMilliseconds(300);

    private readonly ManualEditorScheduler _scheduler = new();

    private (EditorContext Context, FoldingModel Model) Create(string text)
    {
        var context = new EditorContext(_scheduler);
        context.SetText(text);
        var feature = new FoldingFeature();
        feature.Attach(context);
        _scheduler.AdvanceBy(FirstDelay);
        return (context, feature.FoldingModel!);
    }

    private static EditorMouseEvent Mouse(double x, bool left = true, bool middle = false, bool alt = false, bool shift = false) =>
        new(x, 5, left, middle, alt, false, shift, false, 1, 0);

    private static MouseTarget GutterTarget(EditorContext context, int line) =>
        new(MouseTargetType.GutterLineDecorations, new Position(line, 1), 1, new Range(line, 1, line, context.Model.GetLineMaxColumn(line)), false, null);

    private static MouseTarget TextTarget(int line, int column) =>
        new(MouseTargetType.ContentText, new Position(line, column), column, Range.Collapsed(new Position(line, column)), false, null);

    private static double IconX(EditorContext context) => context.ViewModel.LayoutInfo.DecorationsLeft + 8;

    private static void Click(EditorContext context, EditorMouseEvent mouse, MouseTarget down, MouseTarget? up = null)
    {
        context.UserInputEvents.EmitMouseDown(new EditorMouseEventArgs(mouse, down));
        context.UserInputEvents.EmitMouseUp(new EditorMouseEventArgs(mouse, up ?? down));
    }

    private static bool[] Collapsed(FoldingModel model) =>
        [.. Enumerable.Range(0, model.Regions.Length).Select(i => model.Regions.IsCollapsed(i))];

    [Fact]
    public void ClickOnExpandedChevron_FoldsTheRegion()
    {
        (EditorContext context, FoldingModel model) = Create("a\n  b\n  c\nd");

        Click(context, Mouse(IconX(context)), GutterTarget(context, 1));

        Assert.Equal([true], Collapsed(model));
    }

    [Fact]
    public void ClickOnCollapsedChevron_UnfoldsIt()
    {
        (EditorContext context, FoldingModel model) = Create("a\n  b\n  c\nd");
        Click(context, Mouse(IconX(context)), GutterTarget(context, 1));

        Click(context, Mouse(IconX(context)), GutterTarget(context, 1));

        Assert.Equal([false], Collapsed(model));
    }

    [Fact]
    public void ClickInTheFirstFourDipsOfTheDecorationsArea_DoesNothing()
    {
        (EditorContext context, FoldingModel model) = Create("a\n  b\n  c\nd");

        Click(context, Mouse(context.ViewModel.LayoutInfo.DecorationsLeft + 3), GutterTarget(context, 1));

        Assert.Equal([false], Collapsed(model));
    }

    [Fact]
    public void ShiftClickOnChevron_FoldsChildrenFirst_ThenTheRegion()
    {
        (EditorContext context, FoldingModel model) = Create("a\n  b\n    c\n    d\n  e\nf");
        Assert.Equal(2, model.Regions.Length);

        Click(context, Mouse(IconX(context), shift: true), GutterTarget(context, 1));
        Assert.Equal([false, true], Collapsed(model));

        Click(context, Mouse(IconX(context), shift: true), GutterTarget(context, 1));
        Assert.Equal([true, true], Collapsed(model));
    }

    [Fact]
    public void AltClickOnChevron_TogglesTheSurroundingRegions()
    {
        (EditorContext context, FoldingModel model) = Create("a\n b\nc\n d\ne\n f");
        Assert.Equal(3, model.Regions.Length);

        Click(context, Mouse(IconX(context), alt: true), GutterTarget(context, 1));
        Assert.Equal([false, true, true], Collapsed(model));

        // the surrounding regions are folded now: unfold them
        Click(context, Mouse(IconX(context), alt: true), GutterTarget(context, 1));
        Assert.Equal([false, false, false], Collapsed(model));
    }

    [Fact]
    public void ClickOnEllipsis_UnfoldsTheRegion()
    {
        (EditorContext context, FoldingModel model) = Create("a\n  b\n  c\nd");
        Click(context, Mouse(IconX(context)), GutterTarget(context, 1));
        Assert.Equal([true], Collapsed(model));

        // The text of the folded line is "a": its max column is 2.
        Click(context, Mouse(100), TextTarget(1, 2));

        Assert.Equal([false], Collapsed(model));
    }

    [Fact]
    public void ClickAfterTheEllipsis_DoesNothing()
    {
        (EditorContext context, FoldingModel model) = Create("a\n  b\n  c\nd");
        Click(context, Mouse(IconX(context)), GutterTarget(context, 1));
        var empty = new MouseTarget(MouseTargetType.ContentEmpty, new Position(1, 2), 2, null, false, 30);

        Click(context, Mouse(300), empty);

        Assert.Equal([true], Collapsed(model));
    }

    [Fact]
    public void MouseUpOnAnotherLine_DoesNothing()
    {
        (EditorContext context, FoldingModel model) = Create("a\n  b\n  c\nd");

        Click(context, Mouse(IconX(context)), GutterTarget(context, 1), GutterTarget(context, 4));

        Assert.Equal([false], Collapsed(model));
    }
}
