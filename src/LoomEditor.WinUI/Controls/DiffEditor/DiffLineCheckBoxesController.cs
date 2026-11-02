using LoomEditor.Themes;
using LoomEditor.View.Controller;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Layout;
using LoomEditor.WinUI.Controls.Gutter;
using LoomEditor.WinUI.Controls.Overlay;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.WinUI.Controls.DiffEditor;

/// <summary>
/// The line checkboxes and the line action button of one side editor of a side-by-side <see cref="LoomDiffEditor"/>: a checkbox in the select lane on every
/// visible changed line of this side, from a pool, and one line action button in its lane on the hovered changed line.
/// </summary>
/// <remarks>
/// The selection model is the truth: a click reports the line and the diff editor changes the model, then <see cref="SyncChecks"/> re-reads it. Placed after each
/// draw like <see cref="DiffBlockActionsController"/>. Threading: UI thread only.
/// </remarks>
internal sealed class DiffLineCheckBoxesController : IDisposable
{
    private const double MaxCheckBoxSize = 16;
    private const double MaxButtonSize = 22;
    private const string CheckMarkGlyph = "";

    private readonly LoomEditor _editor;
    private readonly OverlayHost _layer;
    private readonly bool _modifiedSide;
    private readonly Func<double, double, MouseTarget> _hitTest;
    private readonly DiffLineSelection _selection;
    private readonly List<(ToggleButton Box, int Line)> _pool = [];
    private Button? _lineActionButton;
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

    /// <summary>Raised on a checkbox click with its line; the diff editor reads the modifiers and changes the selection.</summary>
    public event Action<int>? CheckBoxClicked;

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
        foreach ((ToggleButton box, int line) in _pool)
        {
            if (box.Visibility == Visibility.Visible)
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
        foreach ((ToggleButton box, _) in _pool)
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
        foreach ((ToggleButton box, _) in _pool)
        {
            box.Click -= OnCheckBoxClick;
            _layer.Remove(box);
        }

        _pool.Clear();
        if (_lineActionButton is not null)
        {
            _lineActionButton.Click -= OnLineActionClick;
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

    private static void SetChecked(ToggleButton box, bool isChecked)
    {
        box.IsChecked = isChecked;
        ((FontIcon)box.Content).Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;
    }

    private static FontIcon CreateIcon(double fontSize)
    {
        var icon = new FontIcon { FontSize = fontSize };
        if (Application.Current?.Resources.TryGetValue("SymbolThemeFontFamily", out object? font) == true && font is FontFamily family)
        {
            icon.FontFamily = family;
        }

        return icon;
    }

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

        ToggleButton box = _pool[index].Box;
        if (_pool[index].Line != line)
        {
            _pool[index] = (box, line);
            AutomationProperties.SetName(box, DiffStrings.SelectLine(_modifiedSide, line));
        }

        SetChecked(box, _selection.IsSelected(_modifiedSide, line));
        box.Width = size;
        box.Height = size;
        _layer.Show(box, new OverlayRect(x, y, size, size));
    }

    private ToggleButton CreateBox()
    {
        FontIcon icon = CreateIcon(10);
        icon.Glyph = CheckMarkGlyph;
        var box = new ToggleButton
        {
            Content = icon,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(3),
            MinWidth = 0,
            MinHeight = 0,
            IsTabStop = false,
            Visibility = Visibility.Collapsed,
        };
        box.Click += OnCheckBoxClick;
        if (_theme is not null)
        {
            GutterButtonTheme.Apply(box, _theme);
        }

        return box;
    }

    private void PlaceLineAction(int line, double x, double y, double size)
    {
        if (_lineActionButton is null)
        {
            _lineActionButton = new Button
            {
                Content = CreateIcon(12),
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(4),
                IsTabStop = false,
                Visibility = Visibility.Collapsed,
            };
            _lineActionButton.Click += OnLineActionClick;
            if (_theme is not null)
            {
                GutterButtonTheme.Apply(_lineActionButton, _theme);
            }
        }

        Button button = _lineActionButton;
        ((FontIcon)button.Content).Glyph = _lineActionGlyph;
        ToolTipService.SetToolTip(button, _lineActionToolTip);
        AutomationProperties.SetName(button, DiffStrings.BlockAction(_lineActionToolTip ?? DiffStrings.LineActionFallback, line, line));
        _lineActionLine = line;
        button.Width = size;
        button.Height = size;
        _layer.Show(button, new OverlayRect(x, y, size, size));
    }

    private void OnCheckBoxClick(object sender, RoutedEventArgs e)
    {
        foreach ((ToggleButton box, int line) in _pool)
        {
            if (ReferenceEquals(box, sender))
            {
                CheckBoxClicked?.Invoke(line);

                // The model is the truth: undo the button's own toggle when the click changed nothing.
                SyncChecks();
                return;
            }
        }
    }

    private void OnLineActionClick(object sender, RoutedEventArgs e) => LineActionClicked?.Invoke(_lineActionLine);
}
