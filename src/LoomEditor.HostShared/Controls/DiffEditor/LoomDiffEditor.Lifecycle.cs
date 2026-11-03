#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls;
#endif

/// <content>The teardown when the control leaves the tree.</content>
public sealed partial class LoomDiffEditor
{
    // The control leaves the tree: the view model and what was built on it go; Loaded builds them again (EnsureViewModel).
    private void DestroyViewModel()
    {
        DestroyAccessibleViewer();
        if (_viewModel is null)
        {
            return;
        }

        _hideUnchangedRegions?.Dispose();
        _hideUnchangedRegions = null;
        _viewZones?.Dispose();
        _viewZones = null;
        _viewModel.DiffChanged -= OnViewModelDiffChanged;
        _viewModel.Dispose();
        _viewModel = null;
        _changes = [];
        _changesDiff = null;
        UpdateBlockActionBlocks(_changes);
        _viewModelReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _originalTarget?.Clear();
        _modifiedTarget?.Clear();
        _decorations = null;
    }
}
