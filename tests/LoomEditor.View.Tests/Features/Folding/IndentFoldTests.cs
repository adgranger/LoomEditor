// Ported from microsoft/vscode: src/vs/editor/contrib/folding/test/browser/indentFold.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.
using LoomEditor.Core.Model;
using LoomEditor.View.Features.Folding;

namespace LoomEditor.View.Tests.Features.Folding;

public class IndentFoldTests
{
    private readonly record struct IndentRange(int Start, int End);

    private sealed class Reporter(int limit) : IFoldingLimitReporter
    {
        public int? Reported { get; private set; }

        public int Limit => limit;

        public void Update(int computed, int? limited) => Reported = limited;
    }

    // Monaco: 'Indentation Folding' › 'Limit by indent'
    [Fact]
    public void LimitByIndent()
    {
        string[] lines =
        [
            "A",
            "  A",
            "  A",
            "    A",
            "      A",
            "    A",
            "      A",
            "      A",
            "         A",
            "      A",
            "         A",
            "  A",
            "              A",
            "                 A",
            "A",
            "  A",
        ];
        var r1 = new IndentRange(1, 14);
        var r2 = new IndentRange(3, 11);
        var r3 = new IndentRange(4, 5);
        var r4 = new IndentRange(6, 11);
        var r5 = new IndentRange(8, 9);
        var r6 = new IndentRange(10, 11);
        var r7 = new IndentRange(12, 14);
        var r8 = new IndentRange(13, 14);
        var r9 = new IndentRange(15, 16);
        var model = new TextModel(string.Join('\n', lines));

        void AssertLimit(int maxEntries, IndentRange[] expectedRanges, string message)
        {
            var reporter = new Reporter(maxEntries);
            FoldingRegions indentRanges = IndentRanges.ComputeRanges(model, true, null, reporter);
            Assert.True(indentRanges.Length <= maxEntries, "max " + message);
            var actual = new List<IndentRange>();
            for (int i = 0; i < indentRanges.Length; i++)
            {
                actual.Add(new IndentRange(indentRanges.GetStartLineNumber(i), indentRanges.GetEndLineNumber(i)));
            }

            Assert.Equal(expectedRanges, actual);
            Assert.Equal(9 <= maxEntries ? null : maxEntries, reporter.Reported);
        }

        AssertLimit(1000, [r1, r2, r3, r4, r5, r6, r7, r8, r9], "1000");
        AssertLimit(9, [r1, r2, r3, r4, r5, r6, r7, r8, r9], "9");
        AssertLimit(8, [r1, r2, r3, r4, r5, r6, r7, r9], "8");
        AssertLimit(7, [r1, r2, r3, r4, r5, r7, r9], "7");
        AssertLimit(6, [r1, r2, r3, r4, r7, r9], "6");
        AssertLimit(5, [r1, r2, r3, r7, r9], "5");
        AssertLimit(4, [r1, r2, r7, r9], "4");
        AssertLimit(3, [r1, r2, r9], "3");
        AssertLimit(2, [r1, r9], "2");
        AssertLimit(1, [r1], "1");
        AssertLimit(0, [], "0");
    }
}