// Ported from microsoft/vscode: src/vs/editor/test/browser/viewModel/modelLineProjection.test.ts (helpers)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout.TestHelpers;

internal static class ProjectionTestHelpers
{
    public static IModelLineProjection CreateSplitLine(int[] splitLengths, double[] breakingOffsetsVisibleColumn, int wrappedTextIndentWidth, bool isVisible = true) =>
        ModelLineProjection.Create(CreateLineBreakData(splitLengths, breakingOffsetsVisibleColumn, wrappedTextIndentWidth), isVisible);

    public static ModelLineProjectionData CreateLineBreakData(int[] breakingLengths, double[] breakingOffsetsVisibleColumn, int wrappedTextIndentWidth)
    {
        var sums = new int[breakingLengths.Length];
        for (int i = 0; i < breakingLengths.Length; i++)
        {
            sums[i] = (i > 0 ? sums[i - 1] : 0) + breakingLengths[i];
        }

        return new ModelLineProjectionData(null, null, sums, breakingOffsetsVisibleColumn, wrappedTextIndentWidth);
    }
}
