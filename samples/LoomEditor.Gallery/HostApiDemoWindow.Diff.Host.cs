using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LoomEditor.Gallery;

public sealed partial class HostApiDemoWindow
{
    private void OnConflictColors(object sender, RoutedEventArgs e) => ApplyConflictColors();

    private void OnClearDecorations(object sender, RoutedEventArgs e) => ClearDecorations();

    private void OnHideUnchanged(object sender, RoutedEventArgs e) => HideUnchanged();

    private void OnShowAll(object sender, RoutedEventArgs e) => ShowAllAreas();

    private void OnRemoveZones(object sender, RoutedEventArgs e) => RemoveZones();

    private void OnAddConflictHeader(object sender, RoutedEventArgs e)
    {
        var current = new Button { Content = "Accept current" };
        current.Click += (_, _) => Log("Accept current");
        var incoming = new Button { Content = "Accept incoming" };
        incoming.Click += (_, _) => Log("Accept incoming");
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        panel.Children.Add(current);
        panel.Children.Add(incoming);
        _zoneIds.Add(Editor.AddViewZone(8, Editor.LineHeight, panel));
        Log("Add conflict header");
    }

    private void OnAddTextBoxZone(object sender, RoutedEventArgs e)
    {
        _zoneIds.Add(Editor.AddViewZone(30, Editor.LineHeight * 2, new TextBox { PlaceholderText = "Host text box" }));
        Log("Add text box zone");
    }
}
