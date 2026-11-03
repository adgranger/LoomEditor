using Avalonia;
using Avalonia.Input;
using LoomEditor.Avalonia.Controls.DiffEditor;
using LoomEditor.Avalonia.Controls.Overlay;
using LoomEditor.Avalonia.Input;
using LoomEditor.View.Controller;
using LoomEditor.View.DiffEditor;

namespace LoomEditor.Avalonia.Controls;

// The selection of changed lines, its checkboxes and the line action. The selection lives with the control, not the view model, so SetSelection works before
// the control is loaded.
public sealed partial class LoomDiffEditor
{
    /// <summary>Identifies the <see cref="LineSelectionEnabled"/> styled property.</summary>
    public static readonly StyledProperty<bool> LineSelectionEnabledProperty =
        AvaloniaProperty.Register<LoomDiffEditor, bool>(nameof(LineSelectionEnabled), false);

    /// <summary>Identifies the <see cref="LineActionGlyph"/> styled property.</summary>
    public static readonly StyledProperty<string?> LineActionGlyphProperty = RegisterGutterString(nameof(LineActionGlyph));

    /// <summary>Identifies the <see cref="LineActionToolTip"/> styled property.</summary>
    public static readonly StyledProperty<string?> LineActionToolTipProperty = RegisterGutterString(nameof(LineActionToolTip));

    /// <summary>Identifies the <see cref="LineActionKey"/> styled property.</summary>
    public static readonly StyledProperty<Key> LineActionKeyProperty =
        AvaloniaProperty.Register<LoomDiffEditor, Key>(nameof(LineActionKey), Key.S);

    private readonly DiffLineSelection _lineSelection = new();
    private int _selectionOriginalVersionId = -1;
    private int _selectionModifiedVersionId = -1;

    /// <summary>Raised when <see cref="SelectedLines"/> changed.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// Raised by the line action button or the <see cref="LineActionKey"/>, on the UI thread. The host decides what the action does; the editor changes nothing.
    /// </summary>
    public event EventHandler<DiffLineActionInvokedEventArgs>? LineActionInvoked;

    /// <summary>
    /// Whether every changed line of the side-by-side view has a checkbox in the glyph margin (deleted lines on the left, added lines on the right): a click
    /// toggles the line, Ctrl+click the line and its partner on the other side, Shift+click adds the changed lines from the last clicked one on the same side.
    /// Turning it off clears the selection. No checkbox in the inline view. Defaults to false.
    /// </summary>
    public bool LineSelectionEnabled
    {
        get => GetValue(LineSelectionEnabledProperty);
        set => SetValue(LineSelectionEnabledProperty, value);
    }

    /// <summary>
    /// The glyph (a character of <see cref="GlyphFontFamily"/>) of the line action button, shown on the hovered changed line when
    /// <see cref="LineSelectionEnabled"/> is on; null (the default) hides the button. The <see cref="LineActionKey"/> works without it.
    /// </summary>
    public string? LineActionGlyph
    {
        get => GetValue(LineActionGlyphProperty);
        set => SetValue(LineActionGlyphProperty, value);
    }

    /// <summary>The tooltip of the line action button, also the start of its automation name (<c>"{tooltip}, line {n}"</c>). Defaults to null.</summary>
    public string? LineActionToolTip
    {
        get => GetValue(LineActionToolTipProperty);
        set => SetValue(LineActionToolTipProperty, value);
    }

    /// <summary>
    /// The key that raises <see cref="LineActionInvoked"/> when <see cref="LineSelectionEnabled"/> is on in the side-by-side view, no modifier is down and the
    /// focused editor is read-only (an editable side keeps the key for typing). The lines are the selection, else the caret line if it is a changed line.
    /// Defaults to <see cref="Key.S"/>.
    /// </summary>
    public Key LineActionKey
    {
        get => GetValue(LineActionKeyProperty);
        set => SetValue(LineActionKeyProperty, value);
    }

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

    // Tunnels before the editors see the key (WinUI: OnPreviewKeyDown).
    private void OnLineActionKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || e.Key != LineActionKey || !LineSelectionEnabled || !_options.RenderSideBySideEffective || _original is null || _modified is null
            || KeyMapping.ToModifiers(e.KeyModifiers, OperatingSystem.IsMacOS()) != View.Commands.KeyModifiers.None)
        {
            return;
        }

        // Only a read-only focused editor: an editable side never loses the key.
        bool modified;
        if (_modified.IsKeyboardFocusWithin && IsReadOnly)
        {
            modified = true;
        }
        else if (_original.IsKeyboardFocusWithin && !OriginalEditable)
        {
            modified = false;
        }
        else
        {
            return;
        }

        IReadOnlyList<DiffLine> lines = SelectedLines;
        if (lines.Count == 0)
        {
            int caretLine = (modified ? _modified : _original).Selections[0].PositionLineNumber;
            if (!_lineSelection.IsChangedLine(modified, caretLine))
            {
                return;
            }

            lines = (DiffLine[])[new DiffLine(modified ? DiffSide.Modified : DiffSide.Original, caretLine)];
        }

        e.Handled = true;
        LineActionInvoked?.Invoke(this, new DiffLineActionInvokedEventArgs(lines));
    }

    private static void OnLineSelectionEnabledChanged(LoomDiffEditor editor, AvaloniaPropertyChangedEventArgs e)
    {
        if (!(bool)e.NewValue!)
        {
            editor.ClearSelection();
        }

        editor.UpdateGutterLanes();
    }

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
    private void OnCheckBoxClicked(DiffLine line, KeyModifiers modifiers)
    {
        View.Commands.KeyModifiers mapped = KeyMapping.ToModifiers(modifiers, OperatingSystem.IsMacOS());
        if (mapped.HasFlag(View.Commands.KeyModifiers.Shift))
        {
            ExtendLineSelection(line);
        }
        else if (mapped.HasFlag(View.Commands.KeyModifiers.Ctrl))
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
}
