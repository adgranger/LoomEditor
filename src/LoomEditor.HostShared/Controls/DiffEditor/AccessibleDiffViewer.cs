// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/components/accessibleDiffViewer.ts (View)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Themes;
using LoomEditor.View.DiffEditor;

#if !LOOM_AVALONIA
namespace LoomEditor.WinUI.Controls.DiffEditor;
#elif LOOM_AVALONIA
namespace LoomEditor.Avalonia.Controls.DiffEditor;
#endif

// The model side of the viewer: following the model and rebuilding the rows of the current group. The controls are in AccessibleDiffViewer.Host.cs.
internal sealed partial class AccessibleDiffViewer
{
    private readonly AccessibleDiffViewerModel _model;
    private readonly IAccessibleDiffViewerHost _host;
    private readonly EditorTheme _theme;
    private readonly double _fontSize;
    private readonly int _tabSize;
    private IReadOnlyList<ViewElementGroup>? _renderedGroups;
    private int _renderedGroupIndex = -1;
    private bool _isSyncing;

    /// <summary>Stops following the model.</summary>
    public void Detach() => _model.Changed -= OnModelChanged;

    private void OnModelChanged(object? sender, EventArgs e) => Render();

    // Rebuilds the rows when the group changed (Monaco: the "render" autorun reads the current group), then selects the current row.
    private void Render()
    {
        _isSyncing = true;
        try
        {
            if (!ReferenceEquals(_renderedGroups, _model.Groups) || _renderedGroupIndex != _model.CurrentGroupIndex)
            {
                RebuildRows();
            }

            int index = _model.CurrentElementIndex;
            if (index >= 0 && index < _items.Count)
            {
                ShowRow(index);
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void RebuildRows()
    {
        _renderedGroups = _model.Groups;
        _renderedGroupIndex = _model.CurrentGroupIndex;
        ClearRows();
        if (_model.CurrentGroup is not { } group)
        {
            return;
        }

        foreach (ViewElement element in group.Lines)
        {
            DiffViewerRowItem row = DiffViewerRowItem.Create(element, group, _model.CurrentGroupIndex, _model.Groups.Count, _host, _tabSize);
            AddRow(row, () =>
            {
                if (!_isSyncing)
                {
                    _model.GoToLine(element);
                }
            });
        }
    }
}
