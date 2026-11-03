using LoomEditor.View.DiffEditor;
using LoomEditor.WinUI.Controls.Gutter;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LoomEditor.WinUI.Controls.DiffEditor;

internal sealed partial class DiffBlockActionsController
{
    private static void SetGlyph(Button button, string glyph) => ((FontIcon)button.Content).Glyph = glyph;

    private static void SetToolTip(Button button, string? toolTip) => ToolTipService.SetToolTip(button, toolTip);

    private static void SetAutomationName(Button button, string name) => AutomationProperties.SetName(button, name);

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

    private void OnClick(object sender, RoutedEventArgs e) => Click(sender);

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
