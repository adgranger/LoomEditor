// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelImpl.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Themes;

namespace LoomEditor.View.Layout;

/// <summary>Accumulates the overview ruler decorations into one group per color (Monaco: <c>OverviewRulerDecorations</c>).</summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class OverviewRulerDecorations
{
    private readonly Dictionary<ThemeColor, OverviewRulerDecorationsGroup> _asMap = [];

    /// <summary>The groups, in order of first appearance.</summary>
    public List<OverviewRulerDecorationsGroup> AsArray { get; } = [];

    public void Accept(ThemeColor color, int zIndex, int startLineNumber, int endLineNumber, int lane)
    {
        if (_asMap.TryGetValue(color, out OverviewRulerDecorationsGroup? prevGroup))
        {
            List<int> prevData = prevGroup.Data;
            int prevLane = prevData[^3];
            int prevEndLineNumber = prevData[^1];
            if (prevLane == lane && prevEndLineNumber + 1 >= startLineNumber)
            {
                // merge into prev
                if (endLineNumber > prevEndLineNumber)
                {
                    prevData[^1] = endLineNumber;
                }

                return;
            }

            prevData.Add(lane);
            prevData.Add(startLineNumber);
            prevData.Add(endLineNumber);
        }
        else
        {
            var group = new OverviewRulerDecorationsGroup(color, zIndex, [lane, startLineNumber, endLineNumber]);
            _asMap[color] = group;
            AsArray.Add(group);
        }
    }
}
