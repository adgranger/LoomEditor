using LoomEditor.Themes;
using LoomEditor.View.Controller;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Layout;

#if LOOM_WINUI
using LoomEditor.WinUI.Controls.Gutter;
using LoomEditor.WinUI.Controls.Overlay;
using Range = LoomEditor.Core.Text.Range; // only the Avalonia project has the global alias
namespace LoomEditor.WinUI.Controls.DiffEditor;
#elif LOOM_AVALONIA
using LoomEditor.Avalonia.Controls.Gutter;
using LoomEditor.Avalonia.Controls.Overlay;
namespace LoomEditor.Avalonia.Controls.DiffEditor;
#else
#error This file is compiled into LoomEditor.WinUI or LoomEditor.Avalonia only.
#endif

/// <summary>
/// The line checkboxes and the line action button of one side editor of a side-by-side <see cref="LoomDiffEditor"/>: a checkbox in the select lane on every
/// visible changed line of this side, from a pool, and one line action button in its lane on the hovered changed line.
/// </summary>
/// <remarks>
/// The selection model is the truth: a click reports the line and the diff editor changes the model, then <see cref="SyncChecks"/> re-reads it. Placed after each
/// draw like <see cref="DiffBlockActionsController"/>. Threading: UI thread only.
/// </remarks>
internal sealed partial class DiffLineCheckBoxesController : IDisposable
{
    private const double MaxCheckBoxSize = 16;
    private const double MaxButtonSize = 22;

    private readonly LoomEditor _editor;
    private readonly OverlayHost _layer;
    private readonly bool _modifiedSide;
    private readonly Func<double, double, MouseTarget> _hitTest;
    private readonly DiffLineSelection _selection;
    private int _lineActionLine;
    private int _selectLane = -1;
    private int _lineActionLane = -1;
    private string? _lineActionGlyph;
    private string? _lineActionToolTip;
    private bool _pointerInside;
    private double _pointerX;
    private double _pointerY;
    private int _hoveredLine;
    private EditorTheme? _theme;

    public DiffLineCheckBoxesController(
        LoomEditor editor, OverlayHost layer, bool modifiedSide, Func<double, double, MouseTarget> hitTest, DiffLineSelection selection)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(hitTest);
        ArgumentNullException.ThrowIfNull(selection);
        _editor = editor;
        _layer = layer;
        _modifiedSide = modifiedSide;
        _hitTest = hitTest;
        _selection = selection;
    }

    /// <summary>Raised on a checkbox click with its line and the modifiers that were down; the diff editor changes the selection.</summary>
    public event Action<int, View.Commands.KeyModifiers>? CheckBoxClicked;

    /// <summary>Raised on a click of the line action button with its line.</summary>
    public event Action<int>? LineActionClicked;

    /// <summary>Sets the lanes (-1 hides one) and the line action; a null glyph hides the line action button.</summary>
    public void Configure(int selectLane, int lineActionLane, string? lineActionGlyph, string? lineActionToolTip)
    {
        _selectLane = selectLane;
        _lineActionLane = lineActionGlyph is null ? -1 : lineActionLane;
        _lineActionGlyph = lineActionGlyph;
        _lineActionToolTip = lineActionToolTip;
        Arrange();
    }

    /// <summary>The pointer moved over the editor (not a drag); <paramref name="target"/> is the hit test the pointer code already made.</summary>
    public void OnPointerMoved(MouseTarget target, double x, double y)
    {
        _pointerInside = true;
        _pointerX = x;
        _pointerY = y;
        SetHoveredLine(LineOf(target));
    }

    /// <summary>The pointer left the editor, or a drag selection started.</summary>
    public void OnPointerLeft()
    {
        _pointerInside = false;
        SetHoveredLine(0);
    }

    /// <summary>Re-reads the checked state of every shown checkbox from the selection model.</summary>
    public void SyncChecks()
    {
        foreach (var (box, line) in _pool)
        {
            if (IsShown(box))
            {
                SetChecked(box, _selection.IsSelected(_modifiedSide, line));
            }
        }
    }

    /// <summary>Places the checkboxes and the line action button; called after each draw and when the diff, the selection or the options change.</summary>
    public void Arrange()
    {
        int used = 0;
        bool lineActionShown = false;
        double lineHeight = _editor.LineHeight;
        if ((_selectLane >= 0 || _lineActionLane >= 0) && lineHeight > 4)
        {
            if (_pointerInside && _lineActionLane >= 0)
            {
                // A scroll under a still pointer changes the hovered line.
                _hoveredLine = LineOf(_hitTest(_pointerX, _pointerY));
            }

            EditorLayoutInfo layout = _editor.LayoutInfo;
            double boxSize = Math.Min(MaxCheckBoxSize, lineHeight - 4);
            double boxInset = (lineHeight - boxSize) / 2;

            // The visible ranges skip the hidden unchanged regions.
            foreach (Range range in _editor.GetVisibleModelRanges())
            {
                for (int line = range.StartLineNumber; line <= range.EndLineNumber; line++)
                {
                    if (!_selection.IsChangedLine(_modifiedSide, line))
                    {
                        continue;
                    }

                    double lineTop = _editor.GetTopForLineNumber(line, includeViewZones: true) - _editor.ScrollTop;
                    if (_selectLane >= 0)
                    {
                        PlaceBox(used++, line, layout.GlyphMarginLeft + (_selectLane * lineHeight) + boxInset, lineTop + boxInset, boxSize);
                    }

                    if (_lineActionLane >= 0 && line == _hoveredLine)
                    {
                        double size = Math.Min(MaxButtonSize, lineHeight - 2);
                        double inset = (lineHeight - size) / 2;
                        PlaceLineAction(line, layout.GlyphMarginLeft + (_lineActionLane * lineHeight) + inset, lineTop + inset, size);
                        lineActionShown = true;
                    }
                }
            }
        }

        for (int i = used; i < _pool.Count; i++)
        {
            OverlayHost.Hide(_pool[i].Box);
        }

        if (!lineActionShown && _lineActionButton is not null)
        {
            OverlayHost.Hide(_lineActionButton);
        }
    }

    /// <summary>Applies the button colors.</summary>
    public void ApplyTheme(EditorTheme theme)
    {
        _theme = theme;
        foreach (var (box, _) in _pool)
        {
            GutterButtonTheme.Apply(box, theme);
        }

        if (_lineActionButton is not null)
        {
            GutterButtonTheme.Apply(_lineActionButton, theme);
        }
    }

    public void Dispose()
    {
        foreach (var (box, _) in _pool)
        {
            DetachBox(box);
            _layer.Remove(box);
        }

        _pool.Clear();
        if (_lineActionButton is not null)
        {
            DetachLineAction(_lineActionButton);
            _layer.Remove(_lineActionButton);
            _lineActionButton = null;
        }
    }

    private static int LineOf(MouseTarget target) =>
        target.Type is MouseTargetType.GutterGlyphMargin or MouseTargetType.GutterLineNumbers or MouseTargetType.GutterLineDecorations
            or MouseTargetType.ContentText or MouseTargetType.ContentEmpty
        && !target.IsAfterLines && target.Position is { } position
            ? position.LineNumber
            : 0;

    private void SetHoveredLine(int line)
    {
        int changedLine = _selection.IsChangedLine(_modifiedSide, line) ? line : 0;
        if (changedLine != _hoveredLine)
        {
            _hoveredLine = changedLine;
            if (_lineActionLane >= 0)
            {
                Arrange();
            }
        }
    }

    private void PlaceBox(int index, int line, double x, double y, double size)
    {
        if (index == _pool.Count)
        {
            _pool.Add((CreateBox(), 0));
        }

        var box = _pool[index].Box;
        if (_pool[index].Line != line)
        {
            _pool[index] = (box, line);
            SetAutomationName(box, DiffStrings.SelectLine(_modifiedSide, line));
        }

        SetChecked(box, _selection.IsSelected(_modifiedSide, line));
        box.Width = size;
        box.Height = size;
        _layer.Show(box, new OverlayRect(x, y, size, size));
    }

    private void PlaceLineAction(int line, double x, double y, double size)
    {
        _lineActionButton ??= CreateLineActionButton();
        var button = _lineActionButton;
        SetLineAction(button, _lineActionGlyph, _lineActionToolTip);
        SetAutomationName(button, DiffStrings.BlockAction(_lineActionToolTip ?? DiffStrings.LineActionFallback, line, line));
        _lineActionLine = line;
        button.Width = size;
        button.Height = size;
        _layer.Show(button, new OverlayRect(x, y, size, size));
    }

    private void Click(object? sender, View.Commands.KeyModifiers modifiers)
    {
        foreach (var (box, line) in _pool)
        {
            if (ReferenceEquals(box, sender))
            {
                CheckBoxClicked?.Invoke(line, modifiers);

                // The model is the truth: undo the button's own toggle when the click changed nothing.
                SyncChecks();
                return;
            }
        }
    }
}
