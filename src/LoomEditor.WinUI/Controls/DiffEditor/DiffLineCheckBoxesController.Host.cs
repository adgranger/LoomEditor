using LoomEditor.WinUI.Controls.Gutter;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Core;

namespace LoomEditor.WinUI.Controls.DiffEditor;

internal sealed partial class DiffLineCheckBoxesController
{
    private const string CheckMarkGlyph = "";

    private readonly List<(ToggleButton Box, int Line)> _pool = [];
    private Button? _lineActionButton;

    private static bool IsShown(ToggleButton box) => box.Visibility == Visibility.Visible;

    private static void SetAutomationName(ButtonBase button, string name) => AutomationProperties.SetName(button, name);

    private static void SetLineAction(Button button, string? glyph, string? toolTip)
    {
        ((FontIcon)button.Content).Glyph = glyph;
        ToolTipService.SetToolTip(button, toolTip);
    }

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

    private static bool IsDown(VirtualKey key) => InputKeyboardSource.GetKeyStateForCurrentThread(key).HasFlag(CoreVirtualKeyStates.Down);

    // The modifiers down at the click: WinUI has no modifiers on the click, so the keyboard state is read.
    private static View.Commands.KeyModifiers CurrentModifiers() =>
        (IsDown(VirtualKey.Shift) ? View.Commands.KeyModifiers.Shift : View.Commands.KeyModifiers.None)
        | (IsDown(VirtualKey.Control) ? View.Commands.KeyModifiers.Ctrl : View.Commands.KeyModifiers.None);

    private void DetachBox(ToggleButton box) => box.Click -= OnCheckBoxClick;

    private void DetachLineAction(Button button) => button.Click -= OnLineActionClick;

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

    private Button CreateLineActionButton()
    {
        var button = new Button
        {
            Content = CreateIcon(12),
            Padding = new Thickness(0),
            CornerRadius = new CornerRadius(4),
            IsTabStop = false,
            Visibility = Visibility.Collapsed,
        };
        button.Click += OnLineActionClick;
        if (_theme is not null)
        {
            GutterButtonTheme.Apply(button, _theme);
        }

        return button;
    }

    private void OnCheckBoxClick(object sender, RoutedEventArgs e) => Click(sender, CurrentModifiers());

    private void OnLineActionClick(object sender, RoutedEventArgs e) => LineActionClicked?.Invoke(_lineActionLine);
}
