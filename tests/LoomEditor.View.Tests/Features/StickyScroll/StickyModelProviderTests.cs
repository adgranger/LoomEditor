using LoomEditor.Core.Model;
using LoomEditor.View.Features.Folding;
using LoomEditor.View.Features.StickyScroll;

namespace LoomEditor.View.Tests.Features.StickyScroll;

public class StickyModelProviderTests
{
    private static StickyModel Compute(string text)
    {
        var model = new TextModel(text);
        using var provider = new StickyModelProvider(model, new RangesLimitReporter(() => 5000));
        StickyModel? sticky = provider.Update();
        Assert.NotNull(sticky);
        Assert.Equal(model.VersionId, sticky.Version);
        return sticky;
    }

    [Fact]
    public void FromFoldingRegions_NestedBlocks_BuildsTheTree()
    {
        StickyModel sticky = Compute("function f() {\n  if (a) {\n    b();\n  }\n}");

        StickyElement root = sticky.Element!;
        Assert.Null(root.Range);
        StickyElement function = Assert.Single(root.Children);
        Assert.Same(root, function.Parent);
        StickyElement inner = Assert.Single(function.Children);
        Assert.Same(function, inner.Parent);
        Assert.Equal(2, inner.Range!.StartLineNumber);
        Assert.Empty(inner.Children);
    }

    [Fact]
    public void FromFoldingRegions_RangeEndsOneLineAfterTheRegion()
    {
        var model = new TextModel("A\n  A\n  A\n  A");
        FoldingRegions regions = IndentRanges.ComputeRanges(model, false, null);

        StickyElement child = Assert.Single(StickyModelProvider.FromFoldingRegions(regions).Children);

        Assert.Equal(new StickyRange(1, 5), child.Range);
    }

    [Fact]
    public void Update_DefaultModelOutline_FallsBackToIndentation()
    {
        var model = new TextModel("A\n  A");
        using var provider = new StickyModelProvider(model, new RangesLimitReporter(() => 5000));

        StickyModel? sticky = provider.Update();

        Assert.Single(sticky!.Element!.Children);
    }

    [Fact]
    public void Update_EmptyDocument_HasAnEmptyRoot()
    {
        StickyModel sticky = Compute("");

        Assert.Empty(sticky.Element!.Children);
    }
}
