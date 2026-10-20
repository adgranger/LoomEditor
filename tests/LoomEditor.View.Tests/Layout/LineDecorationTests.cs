// Ported from microsoft/vscode: src/vs/editor/test/common/viewLayout/lineDecorations.test.ts (suite 'Editor ViewLayout - ViewLineParts')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

// Not ported: 'Bug 9827', 'issue #3462', 'empty width only decorations are kept, empty regular ones are not',
// 'width only decorations come before other decorations at the same position', 'width only decorations are not turned
// into pseudo elements', 'ViewLineParts' (normalizer, injected text), 'issue #37401' (a range with column 0 cannot be
// built in C#). The other tests are new.
public class LineDecorationTests
{
    // Monaco: 'issue #3661: Link decoration bleeds to next line when wrapping'
    [Fact]
    public void Issue3661_LinkDecorationBleedsToNextLineWhenWrapping()
    {
        IReadOnlyList<LineDecoration> result = LineDecoration.Filter(
            [new InlineDecoration(new Range(2, 12, 3, 30), "detected-link", InlineDecorationType.Regular)], 3, 12, 500);

        Assert.Equal(new[] { new LineDecoration(12, 30, "detected-link", InlineDecorationType.Regular) }, result);
    }

    [Fact]
    public void Filter_DecorationOnOtherLine_IsDropped()
    {
        IReadOnlyList<LineDecoration> result = LineDecoration.Filter(
            [new InlineDecoration(new Range(1, 1, 1, 5), "a", InlineDecorationType.Regular), new InlineDecoration(new Range(3, 1, 4, 5), "b", InlineDecorationType.Regular)], 2, 1, 10);

        Assert.Empty(result);
    }

    [Fact]
    public void Filter_EmptyRegular_IsDropped_EmptyAfter_IsKept()
    {
        IReadOnlyList<LineDecoration> result = LineDecoration.Filter(
            [new InlineDecoration(new Range(1, 3, 1, 3), "regular", InlineDecorationType.Regular), new InlineDecoration(new Range(1, 3, 1, 3), "after", InlineDecorationType.After)], 1, 1, 10);

        Assert.Equal(new[] { new LineDecoration(3, 3, "after", InlineDecorationType.After) }, result);
    }

    [Fact]
    public void Filter_MultiLine_UsesMinAndMaxColumnOnInnerLines()
    {
        IReadOnlyList<LineDecoration> result = LineDecoration.Filter(
            [new InlineDecoration(new Range(1, 4, 3, 2), "a", InlineDecorationType.Regular)], 2, 1, 7);

        Assert.Equal(new[] { new LineDecoration(1, 7, "a", InlineDecorationType.Regular) }, result);
    }
}
