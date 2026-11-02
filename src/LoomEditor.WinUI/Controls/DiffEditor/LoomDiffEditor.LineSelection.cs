using LoomEditor.View.DiffEditor;

namespace LoomEditor.WinUI.Controls;

// The selection of changed lines. It lives with the control, not the view model, so SetSelection works before the control is loaded.
public sealed partial class LoomDiffEditor
{
    private readonly DiffLineSelection _lineSelection = new();
    private int _selectionOriginalVersionId = -1;
    private int _selectionModifiedVersionId = -1;

    /// <summary>Raised when <see cref="SelectedLines"/> changed.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// The selected changed lines: the original side ascending, then the modified side ascending. A new diff keeps them (minus the lines that are no longer changed
    /// lines) when neither text was edited since the previous diff, and clears them otherwise.
    /// </summary>
    public IReadOnlyList<DiffLine> SelectedLines => _lineSelection.Lines.Select(ToDiffLine).ToArray();

    /// <summary>Replaces the selection. Lines that are not changed lines of the last published diff are ignored, so nothing is selected before the first diff.</summary>
    /// <param name="lines">The lines to select.</param>
    /// <exception cref="ArgumentNullException"><paramref name="lines"/> is null.</exception>
    public void SetSelection(IEnumerable<DiffLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        RaiseSelectionChanged(_lineSelection.Set(lines.Select(l => (l.Side == DiffSide.Modified, l.LineNumber))));
    }

    /// <summary>Unselects every line.</summary>
    public void ClearSelection() => RaiseSelectionChanged(_lineSelection.Clear());

    // Click: flips one changed line.
    internal void ToggleLine(DiffLine line) => RaiseSelectionChanged(_lineSelection.Toggle(line.Side == DiffSide.Modified, line.LineNumber));

    // Ctrl+click: flips the line and its partner (same index in the same block).
    internal void ToggleLinePair(DiffLine line) => RaiseSelectionChanged(_lineSelection.TogglePair(line.Side == DiffSide.Modified, line.LineNumber));

    // Shift+click: adds the changed lines from the anchor, on the same side.
    internal void ExtendLineSelection(DiffLine line) => RaiseSelectionChanged(_lineSelection.ExtendTo(line.Side == DiffSide.Modified, line.LineNumber));

    // Called for each new diff, after Changes is rebuilt: the selection survives only when both texts have the version ids of the previous diff.
    private void UpdateLineSelectionBlocks()
    {
        int originalVersionId = _original!.Model.VersionId;
        int modifiedVersionId = _modified!.Model.VersionId;
        bool keepSelection = originalVersionId == _selectionOriginalVersionId && modifiedVersionId == _selectionModifiedVersionId;
        _selectionOriginalVersionId = originalVersionId;
        _selectionModifiedVersionId = modifiedVersionId;
        RaiseSelectionChanged(_lineSelection.SetBlocks(_changes, keepSelection));
    }

    private void RaiseSelectionChanged(bool changed)
    {
        if (changed)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static DiffLine ToDiffLine((bool Modified, int Line) line) => new(line.Modified ? DiffSide.Modified : DiffSide.Original, line.Line);
}
