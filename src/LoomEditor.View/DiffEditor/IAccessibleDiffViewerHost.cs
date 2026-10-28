// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/accessibleDiffViewer.ts (IAccessibleDiffViewerModel)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.DiffEditor;

/// <summary>What the accessible diff viewer needs from the two editors (Monaco: <c>IAccessibleDiffViewerModel</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface IAccessibleDiffViewerHost
{
    /// <summary>The number of lines of the original model.</summary>
    int OriginalLineCount { get; }

    /// <summary>The number of lines of the modified model.</summary>
    int ModifiedLineCount { get; }

    /// <summary>The line of the cursor of the modified editor; <c>null</c> when there is none.</summary>
    int? ModifiedCursorLine { get; }

    /// <summary>The text of a line of the original model.</summary>
    string GetOriginalLine(int lineNumber);

    /// <summary>The text of a line of the modified model.</summary>
    string GetModifiedLine(int lineNumber);

    /// <summary>Selects, reveals and focuses a range of the original editor.</summary>
    void OriginalReveal(Range range);

    /// <summary>Selects, reveals and focuses a range of the modified editor; with <c>null</c> only focuses it.</summary>
    void ModifiedReveal(Range? range);

    /// <summary>Selects a range of the modified editor, so that its commands keep working while the viewer is open.</summary>
    void ModifiedSetSelection(Range range);

    /// <summary>Focuses the modified editor.</summary>
    void ModifiedFocus();

    /// <summary>Shows or hides the viewer (Monaco: <c>_setVisible</c>).</summary>
    void SetViewerVisible(bool visible);
}
