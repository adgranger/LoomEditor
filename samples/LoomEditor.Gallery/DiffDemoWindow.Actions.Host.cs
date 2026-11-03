using Microsoft.UI.Xaml;

namespace LoomEditor.Gallery;

public sealed partial class DiffDemoWindow
{
    private int BlockActionsIndex => BlockActionsBox.SelectedIndex;

    // The glyphs are characters of Segoe Fluent Icons (the default glyph font of the diff editor).
    private static string Glyph(string segoe, string fallback) => segoe;

    private void OnStagingOptionChanged(object sender, RoutedEventArgs e) => ApplyStagingOptions(ReferenceEquals(sender, LightThemeBox));

    private void OnStagingSample(object sender, RoutedEventArgs e) => LoadStagingSample();

    private void OnLargeFile(object sender, RoutedEventArgs e) => LoadLargeStagingFile();

    private void OnClearSelection(object sender, RoutedEventArgs e) => DiffEditor.ClearSelection();

    private void OnSelectAllAdded(object sender, RoutedEventArgs e) => SelectAllAdded();
}
