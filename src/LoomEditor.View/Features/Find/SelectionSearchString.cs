// Ported from microsoft/vscode: src/vs/editor/contrib/find/browser/findController.ts (getSelectionSearchString)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.Find;

/// <summary>Which selections seed the find box (Monaco: <c>'none' | 'single' | 'multiple'</c>).</summary>
internal enum SeedSearchStringFromSelection
{
    /// <summary>Nothing.</summary>
    None,

    /// <summary>A selection on one line.</summary>
    Single,

    /// <summary>A selection on one or several lines.</summary>
    Multiple,
}

/// <summary>What Ctrl+F puts in the find box.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal static class SelectionSearchString
{
    private const int SearchStringMaxLength = 524288;

    /// <summary>
    /// The selected text, or the word at the cursor when the selection is empty; <c>null</c> when there is nothing to
    /// seed (Monaco: <c>getSelectionSearchString</c>).
    /// </summary>
    public static string? Get(
        ViewModel viewModel,
        SeedSearchStringFromSelection mode = SeedSearchStringFromSelection.Single,
        bool seedSearchStringFromNonEmptySelection = false)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        Selection selection = viewModel.GetSelection();
        // if selection spans multiple lines, default search string to empty
        if ((mode == SeedSearchStringFromSelection.Single && selection.Range.StartLineNumber == selection.Range.EndLineNumber)
            || mode == SeedSearchStringFromSelection.Multiple)
        {
            if (selection.IsEmpty)
            {
                WordAtPosition? word = Cursors.WordOperations.GetWordAtPosition(
                    viewModel.Model, viewModel.CursorConfig.WordSeparators, viewModel.CursorConfig.WordSegmenterLocales, selection.Range.Start);
                if (word is { } w && !seedSearchStringFromNonEmptySelection)
                {
                    return w.Word;
                }
            }
            else if (viewModel.Model.GetValueLengthInRange(selection.Range) < SearchStringMaxLength)
            {
                return viewModel.Model.GetValueInRange(selection.Range);
            }
        }

        return null;
    }
}
