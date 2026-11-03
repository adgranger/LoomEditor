using LoomEditor.View.Controller;

#if LOOM_WINUI
using LoomEditor.WinUI.Controls.DiffEditor;
using LoomEditor.WinUI.Controls.Overlay;
namespace LoomEditor.WinUI.Controls;
#elif LOOM_AVALONIA
using LoomEditor.Avalonia.Controls.DiffEditor;
using LoomEditor.Avalonia.Controls.Overlay;
namespace LoomEditor.Avalonia.Controls;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

// The selection of changed lines, its checkboxes and the line action. The selection lives with the control, not the view model, so SetSelection works before
// the control is loaded.
public sealed partial class LoomDiffEditor
{
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

    private static DiffLine ToDiffLine((bool Modified, int Line) line) => new(line.Modified ? DiffSide.Modified : DiffSide.Original, line.Line);

    // Called for each new diff, after Changes is rebuilt: the selection survives only when both texts have the version ids of the previous diff.
    private void UpdateLineSelectionBlocks()
    {
        int originalVersionId = _original!.Model.VersionId;
        int modifiedVersionId = _modified!.Model.VersionId;
        bool keepSelection = originalVersionId == _selectionOriginalVersionId && modifiedVersionId == _selectionModifiedVersionId;
        _selectionOriginalVersionId = originalVersionId;
        _selectionModifiedVersionId = modifiedVersionId;
        RaiseSelectionChanged(_lineSelection.SetBlocks(_changes, keepSelection));
        ArrangeLineCheckBoxes();
    }

    private DiffLineCheckBoxesController CreateLineCheckBoxes(LoomEditor editor, OverlayHost layer, bool modifiedSide, Func<double, double, MouseTarget> hitTest)
    {
        var controller = new DiffLineCheckBoxesController(editor, layer, modifiedSide, hitTest, _lineSelection);
        DiffSide side = modifiedSide ? DiffSide.Modified : DiffSide.Original;
        controller.CheckBoxClicked += (line, modifiers) => OnCheckBoxClicked(new DiffLine(side, line), modifiers);
        controller.LineActionClicked += line => OnLineActionClicked(new DiffLine(side, line));
        return controller;
    }

    // The model is the truth: change it from the modifiers; SelectionChanged re-syncs the boxes of both editors.
    private void OnCheckBoxClicked(DiffLine line, View.Commands.KeyModifiers modifiers)
    {
        if (modifiers.HasFlag(View.Commands.KeyModifiers.Shift))
        {
            ExtendLineSelection(line);
        }
        else if (modifiers.HasFlag(View.Commands.KeyModifiers.Ctrl))
        {
            ToggleLinePair(line);
        }
        else
        {
            ToggleLine(line);
        }
    }

    private void OnLineActionClicked(DiffLine line)
    {
        IReadOnlyList<DiffLine> lines = _lineSelection.IsSelected(line.Side == DiffSide.Modified, line.LineNumber) ? SelectedLines : (DiffLine[])[line];
        LineActionInvoked?.Invoke(this, new DiffLineActionInvokedEventArgs(lines));
    }

    private void ArrangeLineCheckBoxes()
    {
        _original?.DiffGutter?.LineCheckBoxes.Arrange();
        _modified?.DiffGutter?.LineCheckBoxes.Arrange();
    }

    private void RaiseSelectionChanged(bool changed)
    {
        if (changed)
        {
            ArrangeLineCheckBoxes();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    // The host already checked the key (the line action key, no modifier, line selection on, side by side); the focus comes from its framework.
    private bool HandleLineActionKey(bool modifiedFocused, bool originalFocused)
    {
        // Only a read-only focused editor: an editable side never loses the key.
        bool modified;
        if (modifiedFocused && IsReadOnly)
        {
            modified = true;
        }
        else if (originalFocused && !OriginalEditable)
        {
            modified = false;
        }
        else
        {
            return false;
        }

        IReadOnlyList<DiffLine> lines = SelectedLines;
        if (lines.Count == 0)
        {
            int caretLine = (modified ? _modified! : _original!).Selections[0].PositionLineNumber;
            if (!_lineSelection.IsChangedLine(modified, caretLine))
            {
                return false;
            }

            lines = (DiffLine[])[new DiffLine(modified ? DiffSide.Modified : DiffSide.Original, caretLine)];
        }

        LineActionInvoked?.Invoke(this, new DiffLineActionInvokedEventArgs(lines));
        return true;
    }
}
