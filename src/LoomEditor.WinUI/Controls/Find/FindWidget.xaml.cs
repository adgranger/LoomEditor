using LoomEditor.View.Features.Find;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LoomEditor.WinUI.Controls.Find;

/// <summary>
/// The find widget of the editor: a find box with the match case, whole word and regex toggles, and the previous, next and
/// close buttons (Monaco: <c>FindWidget</c>, behavior only, not its DOM).
/// </summary>
/// <remarks>
/// Public only because the XAML compiler needs it; every member except the constructor is internal. The editor control
/// connects it to its find controller; without a controller it does nothing.
/// Threading: UI thread only.
/// </remarks>
public sealed partial class FindWidget : UserControl, IFindWidget
{
    /// <summary>Creates the widget.</summary>
    public FindWidget()
    {
        InitializeComponent();
        InitializeKeyboard();
    }

    private void SetVisible(bool visible) => Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

    // The TextBox of WinUI stores line breaks as `\r`.
    private static string ToBoxText(string text) => text.Replace('\n', '\r');

    private void OnFindTextChanged(object sender, TextChangedEventArgs e) => FindTextChanged();

    private void OnOptionToggleClick(object sender, RoutedEventArgs e) => OptionToggled();

    private void OnPreviousClick(object sender, RoutedEventArgs e) => PreviousMatch();

    private void OnNextClick(object sender, RoutedEventArgs e) => NextMatch();

    private void OnCloseClick(object sender, RoutedEventArgs e) => CloseWidget();
}
