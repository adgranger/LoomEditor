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
    private const double NumberColumnWidth = 48;
    private const double SignColumnWidth = 24;

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

    // Monaco: _createRow. The classes line-insert / line-delete are the background of the row, char-insert / char-delete the one of the numbers.
    private ThemeColor? RowBackground(LineType type) => type switch
    {
        LineType.Added => _theme.GetColor("diffEditor.insertedLineBackground"),
        LineType.Deleted => _theme.GetColor("diffEditor.removedLineBackground"),
        _ => null,
    };

    private ThemeColor? NumberBackground(LineType type) => type switch
    {
        LineType.Added => _theme.GetColor("diffEditor.insertedTextBackground"),
        LineType.Deleted => _theme.GetColor("diffEditor.removedTextBackground"),
        _ => null,
    };

    // The four cells of a row: the two line numbers, the sign and the text.
    private void AddCells(DiffViewerRowItem row, Action<int, string, ThemeColor?, ThemeColor?> addCell)
    {
        ThemeColor? numberForeground = _theme.GetColor("editorLineNumber.foreground");
        ThemeColor? numberBackground = NumberBackground(row.Element.Type);
        ThemeColor? textForeground = _theme.GetColor("editor.foreground");
        addCell(0, row.OriginalNumber, numberForeground, numberBackground);
        addCell(1, row.ModifiedNumber, numberForeground, numberBackground);
        addCell(2, row.Sign, textForeground, null);
        addCell(3, row.Text, textForeground, null);
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
