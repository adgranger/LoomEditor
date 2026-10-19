// Ported from microsoft/vscode: src/vs/editor/test/common/model/textModelTokens.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.Tokens;

public class RangePriorityQueueTests
{
    private static string[] Render(IEnumerable<OffsetRange> ranges) => [.. ranges.Select(r => r.ToString())];

    // Monaco: 'RangePriorityQueueImpl' > 'addRange'
    [Fact]
    public void AddRange()
    {
        var ranges = new List<OffsetRange>();

        OffsetRange.AddRange(new OffsetRange(0, 2), ranges);
        OffsetRange.AddRange(new OffsetRange(10, 13), ranges);
        OffsetRange.AddRange(new OffsetRange(20, 24), ranges);

        Assert.Equal(["[0, 2)", "[10, 13)", "[20, 24)"], Render(ranges));

        OffsetRange.AddRange(new OffsetRange(2, 10), ranges);

        Assert.Equal(["[0, 13)", "[20, 24)"], Render(ranges));

        OffsetRange.AddRange(new OffsetRange(14, 19), ranges);

        Assert.Equal(["[0, 13)", "[14, 19)", "[20, 24)"], Render(ranges));

        OffsetRange.AddRange(new OffsetRange(10, 22), ranges);

        Assert.Equal(["[0, 24)"], Render(ranges));

        OffsetRange.AddRange(new OffsetRange(-1, 29), ranges);

        Assert.Equal(["[-1, 29)"], Render(ranges));

        OffsetRange.AddRange(new OffsetRange(-10, -5), ranges);

        Assert.Equal(["[-10, -5)", "[-1, 29)"], Render(ranges));
    }

    // Monaco: 'RangePriorityQueueImpl' > 'addRangeAndResize'
    [Fact]
    public void AddRangeAndResize()
    {
        var queue = new RangePriorityQueue();

        queue.AddRange(new OffsetRange(0, 20));
        queue.AddRange(new OffsetRange(100, 120));
        queue.AddRange(new OffsetRange(200, 220));

        // disjoint
        queue.AddRangeAndResize(new OffsetRange(25, 27), 0);

        Assert.Equal(["[0, 20)", "[98, 118)", "[198, 218)"], Render(queue.GetRanges()));

        queue.AddRangeAndResize(new OffsetRange(19, 20), 0);

        Assert.Equal(["[0, 19)", "[97, 117)", "[197, 217)"], Render(queue.GetRanges()));

        queue.AddRangeAndResize(new OffsetRange(19, 97), 0);

        Assert.Equal(["[0, 39)", "[119, 139)"], Render(queue.GetRanges()));

        queue.AddRangeAndResize(new OffsetRange(-1000, 1000), 0);

        Assert.Empty(queue.GetRanges());
    }
}
