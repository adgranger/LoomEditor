using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using LoomEditor.View.Features.Find;

namespace LoomEditor.Avalonia.Controls.Find;

/// <summary>
/// The find widget of the editor: a find box with the match case, whole word and regex toggles, and the previous, next and
/// close buttons, plus the replace row (Monaco: <c>FindWidget</c>, behavior only, not its DOM).
/// </summary>
/// <remarks>
/// Public only to mirror the WinUI control; every member except the constructor is internal. The editor control
/// connects it to its find controller; without a controller it does nothing.
/// Threading: UI thread only.
/// </remarks>
public sealed partial class FindWidget : UserControl, IFindWidget
{
    /// <summary>Creates the widget.</summary>
    public FindWidget()
    {
        InitializeComponent();
        FindTextBox.TextChanged += OnFindTextChanged;
        ReplaceTextBox.TextChanged += OnReplaceTextChanged;
        AliasFluentBrushes();
        InitializeKeyboard();
        IsVisible = false;
    }

    // The brushes under the keys that the Fluent control themes look up (as the StaticResource aliases of the WinUI XAML).
    private void AliasFluentBrushes()
    {
        Alias("InputBackgroundBrush", "TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused");
        Alias("InputForegroundBrush", "TextControlForeground", "TextControlForegroundPointerOver", "TextControlForegroundFocused");
        Alias("InputBorderBrush", "TextControlBorderBrush", "TextControlBorderBrushPointerOver");
        Alias("FocusBorderBrush", "TextControlBorderBrushFocused");
        Alias(
            "TransparentBrush",
            "ToggleButtonBackground", "ToggleButtonBorderBrush", "ToggleButtonBorderBrushPointerOver", "ToggleButtonBorderBrushPressed",
            "ButtonBackground", "ButtonBorderBrush", "ButtonBorderBrushPointerOver", "ButtonBorderBrushPressed");
        Alias("HoverBackgroundBrush", "ToggleButtonBackgroundPointerOver", "ToggleButtonBackgroundPressed", "ButtonBackgroundPointerOver", "ButtonBackgroundPressed");
        Alias("OptionActiveBackgroundBrush", "ToggleButtonBackgroundChecked", "ToggleButtonBackgroundCheckedPointerOver", "ToggleButtonBackgroundCheckedPressed");
        Alias(
            "WidgetForegroundBrush",
            "ToggleButtonForeground", "ToggleButtonForegroundPointerOver", "ToggleButtonForegroundPressed",
            "ButtonForeground", "ButtonForegroundPointerOver", "ButtonForegroundPressed");
        Alias("OptionActiveForegroundBrush", "ToggleButtonForegroundChecked", "ToggleButtonForegroundCheckedPointerOver", "ToggleButtonForegroundCheckedPressed");
        Alias("OptionActiveBorderBrush", "ToggleButtonBorderBrushChecked", "ToggleButtonBorderBrushCheckedPointerOver", "ToggleButtonBorderBrushCheckedPressed");
    }

    private void Alias(string brushKey, params string[] keys)
    {
        object brush = Resources[brushKey]!;
        foreach (string key in keys)
        {
            Resources[key] = brush;
        }
    }

    private IBrush GetBrush(string key) => (IBrush)Resources[key]!;

    private void SetVisible(bool visible) => IsVisible = visible;

    private static string ToBoxText(string text) => text;

    private void OnFindTextChanged(object? sender, TextChangedEventArgs e) => FindTextChanged();

    private void OnOptionToggleClick(object? sender, RoutedEventArgs e) => OptionToggled();

    private void OnPreviousClick(object? sender, RoutedEventArgs e) => PreviousMatch();

    private void OnNextClick(object? sender, RoutedEventArgs e) => NextMatch();

    private void OnCloseClick(object? sender, RoutedEventArgs e) => CloseWidget();
}
