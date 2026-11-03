using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using LoomEditor.Avalonia.Controls.Gutter;
using LoomEditor.Avalonia.Input;

namespace LoomEditor.Avalonia.Controls.DiffEditor;

internal sealed partial class DiffLineCheckBoxesController
{
    private const string CheckMarkGlyph = "";

    private readonly List<(ToggleButton Box, int Line)> _pool = [];
    private Button? _lineActionButton;
    private FontFamily _glyphFont = new(LoomDiffEditor.DefaultGlyphFontFamily);
    private KeyModifiers _pressModifiers;

    /// <summary>The checkboxes that are shown now, for the tests.</summary>
    internal IReadOnlyList<ToggleButton> VisibleCheckBoxes => [.. _pool.Where(item => item.Box.IsVisible).Select(item => item.Box)];

    /// <summary>The line action button when it is shown, for the tests.</summary>
    internal Button? VisibleLineAction => _lineActionButton is { IsVisible: true } button ? button : null;

    /// <summary>Sets the font of the glyphs (<c>LoomDiffEditor.GlyphFontFamily</c>).</summary>
    public void SetGlyphFont(FontFamily font)
    {
        ArgumentNullException.ThrowIfNull(font);
        _glyphFont = font;
        foreach ((ToggleButton box, _) in _pool)
        {
            ((TextBlock)box.Content!).FontFamily = font;
        }

        if (_lineActionButton is not null)
        {
            ((TextBlock)_lineActionButton.Content!).FontFamily = font;
        }
    }

    private static bool IsShown(ToggleButton box) => box.IsVisible;

    private static void SetAutomationName(Button button, string name) => AutomationProperties.SetName(button, name);

    private static void SetLineAction(Button button, string? glyph, string? toolTip)
    {
        ((TextBlock)button.Content!).Text = glyph;
        ToolTip.SetTip(button, toolTip);
    }

    private static void SetChecked(ToggleButton box, bool isChecked)
    {
        box.IsChecked = isChecked;
        ((TextBlock)box.Content!).IsVisible = isChecked;
    }

    private TextBlock CreateIcon(double fontSize) => new()
    {
        FontSize = fontSize,
        FontFamily = _glyphFont,
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
    };

    private void DetachBox(ToggleButton box)
    {
        box.Click -= OnCheckBoxClick;
        box.RemoveHandler(InputElement.PointerPressedEvent, OnBoxPointerPressed);
    }

    private void DetachLineAction(Button button) => button.Click -= OnLineActionClick;

    private ToggleButton CreateBox()
    {
        TextBlock icon = CreateIcon(10);
        icon.Text = CheckMarkGlyph;
        var box = new ToggleButton
        {
            Content = icon,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(3),
            MinWidth = 0,
            MinHeight = 0,
            Focusable = false,
            IsVisible = false,
        };
        box.Click += OnCheckBoxClick;
        box.AddHandler(InputElement.PointerPressedEvent, OnBoxPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        if (_theme is not null)
        {
            GutterButtonTheme.Apply(box, _theme);
        }

        return box;
    }

    private Button CreateLineActionButton()
    {
        var button = new Button
        {
            Content = CreateIcon(12),
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(4),
            Focusable = false,
            IsVisible = false,
        };
        button.Click += OnLineActionClick;
        if (_theme is not null)
        {
            GutterButtonTheme.Apply(button, _theme);
        }

        return button;
    }

    private void OnBoxPointerPressed(object? sender, PointerPressedEventArgs e) => _pressModifiers = e.KeyModifiers;

    private void OnCheckBoxClick(object? sender, RoutedEventArgs e)
    {
        KeyModifiers modifiers = _pressModifiers;
        _pressModifiers = KeyModifiers.None;
        Click(sender, KeyMapping.ToModifiers(modifiers, OperatingSystem.IsMacOS()));
    }

    private void OnLineActionClick(object? sender, RoutedEventArgs e) => LineActionClicked?.Invoke(_lineActionLine);
}
