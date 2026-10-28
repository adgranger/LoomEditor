// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/diffEditorWidget.ts (goToDiff, revealFirstDiff)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Features.DiffNavigation;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// Go to next / previous change. Monaco plays accessibility signal sounds here: not ported.
public sealed partial class LoomDiffEditor : IDiffNavigationTarget
{
    /// <summary>Moves the cursor of the modified editor to the next change, wrapping around, and reveals it (<c>Alt+F5</c>).</summary>
    public void GoToNextChange() => GoToChange(NavigationDirection.Next);

    /// <summary>Moves the cursor of the modified editor to the previous change, wrapping around, and reveals it (<c>Shift+Alt+F5</c>).</summary>
    public void GoToPreviousChange() => GoToChange(NavigationDirection.Previous);

    /// <summary>Waits for the diff, then goes to the first change, if any (Monaco: <c>revealFirstDiff</c>).</summary>
    /// <param name="cancellationToken">Cancels the wait.</param>
    public async Task RevealFirstChangeAsync(CancellationToken cancellationToken = default)
    {
        await WaitForDiffAsync(cancellationToken);
        if (_viewModel?.Diff?.Mappings is { Count: > 0 } mappings)
        {
            GoTo(mappings[0]);
        }
    }

    void IDiffNavigationTarget.GoToChange(NavigationDirection direction) => GoToChange(direction);

    private void GoToChange(NavigationDirection direction)
    {
        if (_modified is null || _viewModel?.Diff is not { } diff)
        {
            return;
        }

        int currentLine = _modified.Selections[0].PositionLineNumber;
        if (DiffNavigator.Find(diff.Mappings, currentLine, _modified.Model.LineCount, direction) is { } mapping)
        {
            GoTo(mapping);
        }
    }

    // Monaco: _goTo. An empty modified range (a pure deletion) goes to its start line.
    private void GoTo(DiffMapping mapping)
    {
        LineRange range = mapping.LineRangeMapping.Modified;
        int lineCount = _modified!.Model.LineCount;
        int start = Math.Clamp(range.StartLineNumber, 1, lineCount);
        int end = Math.Clamp(Math.Max(range.EndLineNumberExclusive, start), 1, lineCount + 1);
        _modified.SetSelections(new[] { Selection.Collapsed(new Position(start, 1)) });
        _modified.RevealRangeInCenter(end > lineCount ? new Core.Text.Range(start, 1, lineCount, _modified.Model.GetLineMaxColumn(lineCount)) : new Core.Text.Range(start, 1, end, 1));
        _modified.Focus(FocusState.Programmatic);
    }
}
