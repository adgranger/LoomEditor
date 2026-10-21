// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/folding.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Features.Folding;

/// <summary>The lines where the selections start (Monaco: <c>SelectedLines</c>).</summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal interface ISelectedLines
{
    bool StartsInside(int startLine, int endLine);
}

/// <summary>Creates <see cref="ISelectedLines"/> (Monaco: <c>toSelectedLines</c>).</summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal static class SelectedLines
{
    public static ISelectedLines From(IReadOnlyList<Selection>? selections) =>
        selections is null || selections.Count == 0 ? new NoSelectedLines() : new SelectionLines(selections);

    private sealed class NoSelectedLines : ISelectedLines
    {
        public bool StartsInside(int startLine, int endLine) => false;
    }

    private sealed class SelectionLines(IReadOnlyList<Selection> selections) : ISelectedLines
    {
        public bool StartsInside(int startLine, int endLine)
        {
            foreach (Selection s in selections)
            {
                int line = s.Range.StartLineNumber;
                if (line >= startLine && line <= endLine)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

