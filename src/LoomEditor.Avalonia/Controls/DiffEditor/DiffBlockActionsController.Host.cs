using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using LoomEditor.View.DiffEditor;
using LoomEditor.Avalonia.Controls.Gutter;

namespace LoomEditor.Avalonia.Controls.DiffEditor;

internal sealed partial class DiffBlockActionsController
{
    private FontFamily _glyphFont = new(LoomDiffEditor.DefaultGlyphFontFamily);

    /// <summary>The buttons that are shown now, for the tests.</summary>
    internal IReadOnlyList<Button> VisibleButtons => [.. _pool.Where(slot => slot.Button.IsVisible).Select(slot => slot.Button)];

    /// <summary>Sets the font of the glyphs (<c>LoomDiffEditor.GlyphFontFamily</c>).</summary>
    public void SetGlyphFont(FontFamily font)
    {
        ArgumentNullException.ThrowIfNull(font);
        _glyphFont = font;
        foreach (Slot slot in _pool)
        {
            ((TextBlock)slot.Button.Content!).FontFamily = font;
        }
    }

    private static void SetGlyph(Button button, string glyph) => ((TextBlock)button.Content!).Text = glyph;

    private static void SetToolTip(Button button, string? toolTip) => ToolTip.SetTip(button, toolTip);

    private static void SetAutomationName(Button button, string name) => AutomationProperties.SetName(button, name);

    private Slot CreateSlot()
    {
        var icon = new TextBlock
        {
            FontSize = 12,
            FontFamily = _glyphFont,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
        };
        var button = new Button
        {
            Content = icon,
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(4),
            Focusable = false,
            IsVisible = false,
        };
        button.Click += OnClick;
        if (_theme is not null)
        {
            GutterButtonTheme.Apply(button, _theme);
        }

        return new Slot(button);
    }

    private void OnClick(object? sender, RoutedEventArgs e) => Click(sender);

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
