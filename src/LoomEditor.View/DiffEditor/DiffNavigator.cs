// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/diffEditorWidget.ts (goToDiff)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.DiffEditor;

/// <summary>The direction of <see cref="DiffNavigator.Find"/>.</summary>
internal enum NavigationDirection
{
    /// <summary>Towards the start of the document.</summary>
    Previous,

    /// <summary>Towards the end of the document.</summary>
    Next,
}

/// <summary>Finds the change a "go to next / previous change" command jumps to (Monaco: <c>DiffEditorWidget.goToDiff</c>).</summary>
/// <remarks>Threading: pure functions; thread-safe.</remarks>
internal static class DiffNavigator
{
    /// <summary>Returns the mapping to go to from <paramref name="currentModifiedLine"/>, wrapping around; null when there is none.</summary>
    /// <param name="mappings">The changes, in document order.</param>
    /// <param name="currentModifiedLine">The line of the cursor in the modified model.</param>
    /// <param name="modifiedLineCount">The number of lines of the modified model.</param>
    /// <param name="direction">Where to go.</param>
    public static DiffMapping? Find(IReadOnlyList<DiffMapping> mappings, int currentModifiedLine, int modifiedLineCount, NavigationDirection direction)
    {
        ArgumentNullException.ThrowIfNull(mappings);

        if (mappings.Count == 0)
        {
            return null;
        }

        if (direction == NavigationDirection.Next)
        {
            // Monaco: on the last line, go back to the first change.
            if (currentModifiedLine == modifiedLineCount)
            {
                return mappings[0];
            }

            return mappings.FirstOrDefault(m => m.LineRangeMapping.Modified.StartLineNumber > currentModifiedLine) ?? mappings[0];
        }

        return mappings.LastOrDefault(m => m.LineRangeMapping.Modified.StartLineNumber < currentModifiedLine) ?? mappings[^1];
    }
}
