using LoomEditor.Themes;
using LoomEditor.View.Controller;
using LoomEditor.View.DiffEditor;
using LoomEditor.View.Layout;
using LoomEditor.WinUI.Controls.Gutter;
using LoomEditor.WinUI.Controls.Overlay;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.WinUI.Controls.DiffEditor;

/// <summary>
/// The block action buttons of one side editor of a side-by-side <see cref="LoomDiffEditor"/>: one button per action at the first line of each block anchored on
/// this side (the modified side, or the original side for a pure deletion), only for the visible lines, from a pool of buttons.
/// </summary>
/// <remarks>
/// Placed after each draw like the comment "+" (<see cref="Comments.CommentGutterController"/>); a click raises <see cref="Invoked"/>. Threading: UI thread only.
/// </remarks>
internal sealed class DiffBlockActionsController : IDisposable
{
    private const double MaxButtonSize = 22;

    private readonly LoomEditor _editor;
    private readonly OverlayHost _layer;
    private readonly bool _modifiedSide;
    private readonly Func<double, double, MouseTarget> _hitTest;
    private readonly List<Slot> _pool = [];
    private List<DiffChangeBlock> _blocks = [];
    private DiffBlockActionsVisibility _visibility;
    private string? _primaryGlyph;
    private string? _primaryToolTip;
    private string? _secondaryGlyph;
    private string? _secondaryToolTip;
    private int _primaryLane = -1;
    private int _secondaryLane = -1;
    private bool _pointerInside;
    private double _pointerX;
    private double _pointerY;
    private int _hoveredLine;
    private EditorTheme? _theme;

    public DiffBlockActionsController(LoomEditor editor, OverlayHost layer, bool modifiedSide, Func<double, double, MouseTarget> hitTest)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(layer);
        ArgumentNullException.ThrowIfNull(hitTest);
        _editor = editor;
        _layer = layer;
        _modifiedSide = modifiedSide;
        _hitTest = hitTest;
    }

    /// <summary>Raised on a click, with the block and the action of the button.</summary>
    public event Action<DiffChangeBlock, DiffBlockAction>? Invoked;

    /// <summary>Takes the blocks of a new diff; only the ones anchored on this side are kept.</summary>
    public void SetBlocks(IReadOnlyList<DiffChangeBlock> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        _blocks = blocks.Where(b => (b.ModifiedLineCount > 0) == _modifiedSide).ToList();
        Arrange();
    }

    /// <summary>Sets the options; a lane of -1 (a null glyph) hides that action.</summary>
    public void Configure(
        DiffBlockActionsVisibility visibility, string? primaryGlyph, string? primaryToolTip, string? secondaryGlyph, string? secondaryToolTip, int primaryLane, int secondaryLane)
    {
        _visibility = visibility;
        _primaryGlyph = primaryGlyph;
        _primaryToolTip = primaryToolTip;
        _secondaryGlyph = secondaryGlyph;
        _secondaryToolTip = secondaryToolTip;
        _primaryLane = primaryGlyph is null ? -1 : primaryLane;
        _secondaryLane = secondaryGlyph is null ? -1 : secondaryLane;
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

    /// <summary>Places the buttons; called after each draw (scrolling, layout) and when the blocks or the options change.</summary>
    public void Arrange()
    {
        int used = 0;
        double lineHeight = _editor.LineHeight;
        if (_visibility != DiffBlockActionsVisibility.Off && (_primaryLane >= 0 || _secondaryLane >= 0) && _blocks.Count > 0 && lineHeight > 2)
        {
            if (_pointerInside && _visibility == DiffBlockActionsVisibility.OnHover)
            {
                // A scroll under a still pointer changes the hovered line.
                _hoveredLine = LineOf(_hitTest(_pointerX, _pointerY));
            }

            int hovered = _visibility == DiffBlockActionsVisibility.OnHover ? BlockAt(_hoveredLine) : -1;
            double size = Math.Min(MaxButtonSize, lineHeight - 2);
            double inset = (lineHeight - size) / 2;
            EditorLayoutInfo layout = _editor.LayoutInfo;

            // The visible ranges skip the hidden unchanged regions: a block whose first line is hidden gets no button.
            foreach (Range range in _editor.GetVisibleModelRanges())
            {
                for (int i = FirstBlockFrom(range.StartLineNumber); i < _blocks.Count && Anchor(_blocks[i]) <= range.EndLineNumber; i++)
                {
                    if (_visibility == DiffBlockActionsVisibility.Always || i == hovered)
                    {
                        double top = _editor.GetTopForLineNumber(Anchor(_blocks[i]), includeViewZones: true) - _editor.ScrollTop + inset;
                        if (_primaryLane >= 0)
                        {
                            Place(used++, _blocks[i], DiffBlockAction.Primary, _primaryGlyph!, _primaryToolTip, layout.GlyphMarginLeft + (_primaryLane * lineHeight) + inset, top, size);
                        }

                        if (_secondaryLane >= 0)
                        {
                            Place(used++, _blocks[i], DiffBlockAction.Secondary, _secondaryGlyph!, _secondaryToolTip, layout.GlyphMarginLeft + (_secondaryLane * lineHeight) + inset, top, size);
                        }
                    }
                }
            }
        }

        for (int i = used; i < _pool.Count; i++)
        {
            OverlayHost.Hide(_pool[i].Button);
        }
    }

    /// <summary>Applies the button colors.</summary>
    public void ApplyTheme(EditorTheme theme)
    {
        _theme = theme;
        foreach (Slot slot in _pool)
        {
            GutterButtonTheme.Apply(slot.Button, theme);
        }
    }

    public void Dispose()
    {
        foreach (Slot slot in _pool)
        {
            slot.Button.Click -= OnClick;
            _layer.Remove(slot.Button);
        }

        _pool.Clear();
    }

    private static int LineOf(MouseTarget target) =>
        target.Type is MouseTargetType.GutterGlyphMargin or MouseTargetType.GutterLineNumbers or MouseTargetType.GutterLineDecorations
            or MouseTargetType.ContentText or MouseTargetType.ContentEmpty
        && !target.IsAfterLines && target.Position is { } position
            ? position.LineNumber
            : 0;

    private int Anchor(DiffChangeBlock block) => _modifiedSide ? block.ModifiedStartLineNumber : block.OriginalStartLineNumber;

    private int LineCount(DiffChangeBlock block) => _modifiedSide ? block.ModifiedLineCount : block.OriginalLineCount;

    // The first block whose anchor is at or after the line (the blocks are in line order).
    private int FirstBlockFrom(int line)
    {
        int low = 0;
        int high = _blocks.Count;
        while (low < high)
        {
            int mid = (low + high) / 2;
            if (Anchor(_blocks[mid]) < line)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    // The block whose lines on this side contain the line, or -1.
    private int BlockAt(int line)
    {
        if (line <= 0)
        {
            return -1;
        }

        int i = FirstBlockFrom(line + 1) - 1;
        return i >= 0 && line < Anchor(_blocks[i]) + LineCount(_blocks[i]) ? i : -1;
    }

    private void SetHoveredLine(int line)
    {
        if (line != _hoveredLine)
        {
            _hoveredLine = line;
            if (_visibility == DiffBlockActionsVisibility.OnHover)
            {
                Arrange();
            }
        }
    }

    private void Place(int index, DiffChangeBlock block, DiffBlockAction action, string glyph, string? toolTip, double x, double y, double size)
    {
        if (index == _pool.Count)
        {
            _pool.Add(CreateSlot());
        }

        Slot slot = _pool[index];
        Button button = slot.Button;
        if (slot.Glyph != glyph)
        {
            slot.Glyph = glyph;
            ((FontIcon)button.Content).Glyph = glyph;
        }

        if (slot.ToolTip != toolTip || !ReferenceEquals(slot.Block, block))
        {
            int start = Anchor(block);
            AutomationProperties.SetName(button, DiffStrings.BlockAction(toolTip ?? DiffStrings.BlockActionFallback, start, start + LineCount(block) - 1));
        }

        if (slot.ToolTip != toolTip)
        {
            slot.ToolTip = toolTip;
            ToolTipService.SetToolTip(button, toolTip);
        }

        slot.Block = block;
        slot.Action = action;
        button.Width = size;
        button.Height = size;
        _layer.Show(button, new OverlayRect(x, y, size, size));
    }

    private Slot CreateSlot()
    {
        var icon = new FontIcon { FontSize = 12 };
        if (Application.Current?.Resources.TryGetValue("SymbolThemeFontFamily", out object? font) == true && font is FontFamily family)
        {
            icon.FontFamily = family;
        }

        var button = new Button
        {
            Content = icon,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(4),
            IsTabStop = false,
            Visibility = Visibility.Collapsed,
        };
        button.Click += OnClick;
        if (_theme is not null)
        {
            GutterButtonTheme.Apply(button, _theme);
        }

        return new Slot(button);
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        foreach (Slot slot in _pool)
        {
            if (ReferenceEquals(slot.Button, sender) && slot.Block is { } block)
            {
                Invoked?.Invoke(block, slot.Action);
                return;
            }
        }
    }

    // A pooled button and what it shows now.
    private sealed class Slot(Button button)
    {
        public Button Button { get; } = button;

        public DiffChangeBlock? Block { get; set; }

        public DiffBlockAction Action { get; set; }

        public string? Glyph { get; set; }

        public string? ToolTip { get; set; }
    }
}
