// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/diffEditorWidget.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.DiffEditor;

#if LOOM_WINUI
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

// The diff state API, and how the decorations follow the diff and the edits.
public sealed partial class LoomDiffEditor
{
    /// <summary>False from an edit until its diff is published (and before the first one).</summary>
    public bool IsDiffUpToDate => _viewModel?.IsDiffUpToDate ?? false;

    /// <summary>The number of changes (blocks of changed lines) of the last diff.</summary>
    public int ChangeCount => _viewModel?.Diff?.Mappings.Count ?? 0;

    /// <summary>
    /// The change blocks of the last published diff, in order; empty before the first diff. Stale while <see cref="IsDiffUpToDate"/> is false; refreshed before
    /// <see cref="DiffChanged"/> is raised. <see cref="GoToNextChange"/> and <see cref="GoToPreviousChange"/> walk the same blocks.
    /// </summary>
    /// <remarks>See <see cref="DiffChangeBlock"/> for the line numbers of an empty side and the effect of <c>IgnoreTrimWhitespace</c>.</remarks>
    public IReadOnlyList<DiffChangeBlock> Changes => _changes;

    /// <summary>Collapses every unchanged region (Monaco: <c>diffEditor.collapseAllUnchangedRegions</c>); only has an effect while <see cref="HideUnchangedRegions"/> is on.</summary>
    public void CollapseAllUnchangedRegions()
    {
        foreach (UnchangedRegion region in _viewModel?.UnchangedRegions ?? [])
        {
            region.CollapseAll();
        }
    }

    /// <summary>Shows every unchanged region (Monaco: <c>diffEditor.showAllUnchangedRegions</c>).</summary>
    public void ShowAllUnchangedRegions()
    {
        foreach (UnchangedRegion region in _viewModel?.UnchangedRegions ?? [])
        {
            region.ShowAll();
        }
    }

    /// <summary>Completes when <see cref="IsDiffUpToDate"/> is true; waits for the control to be loaded first.</summary>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <remarks>After an unload the wait resumes at the next load. shortcut: a wait that already passed the load gate when the control unloads throws, when a use needs it, complete the waiters in OnUnloaded.</remarks>
    public async Task WaitForDiffAsync(CancellationToken cancellationToken = default)
    {
        await _viewModelReady.Task.WaitAsync(cancellationToken);
        await _viewModel!.WaitForDiffAsync(cancellationToken);
    }

    private void OnViewModelDiffChanged(object? sender, EventArgs e)
    {
        // IsDiffUpToDate changes raise this too: rebuild only for a new diff.
        DiffState? diff = _viewModel?.Diff;
        if (!ReferenceEquals(diff, _changesDiff))
        {
            _changesDiff = diff;
            _changes = diff is null ? [] : DiffChangeBlock.FromMappings(diff.Mappings);
            UpdateLineSelectionBlocks();
            UpdateBlockActionBlocks(_changes);
        }

        InvalidateOverview();
        UpdateViewerDiffs();
        UpdateDecorations();
        DiffChanged?.Invoke(this, EventArgs.Empty);
    }

    // Monaco: DiffEditorDecorations runs on the diff state and the options; the line counts shift the ranges of the last diff.
    private void UpdateDecorations()
    {
        if (_decorations is null || _original is null || _modified is null)
        {
            return;
        }

        _decoratedOriginalLines = _original.Model.LineCount;
        _decoratedModifiedLines = _modified.Model.LineCount;
        _decorations.Update(_viewModel?.Diff, _options, _decoratedOriginalLines, _decoratedModifiedLines);
    }

    private void OnOriginalContentChanged(object? sender, Core.Model.ModelContentChangedEventArgs e)
    {
        ReflectText(_original!, OriginalTextProperty);
        UpdateDecorationsIfLineCountChanged();
    }

    private void OnModifiedContentChanged(object? sender, Core.Model.ModelContentChangedEventArgs e)
    {
        ReflectText(_modified!, ModifiedTextProperty);
        UpdateDecorationsIfLineCountChanged();
    }

    private void UpdateDecorationsIfLineCountChanged()
    {
        if (_original!.Model.LineCount != _decoratedOriginalLines || _modified!.Model.LineCount != _decoratedModifiedLines)
        {
            UpdateDecorations();
        }
    }
}
