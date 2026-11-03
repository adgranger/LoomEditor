using Avalonia.Interactivity;

namespace LoomEditor.Gallery;

public sealed partial class DiffDemoWindow
{
    private int BlockActionsIndex => Math.Max(0, BlockActionsBox.SelectedIndex);

    // The glyphs are characters of Segoe Fluent Icons (the default glyph font of the diff editor), which only exists on Windows.
    private static string Glyph(string segoe, string fallback) => OperatingSystem.IsWindows() ? segoe : fallback;

    private void OnStagingOptionChanged(object? sender, RoutedEventArgs e) => ApplyStagingOptions(ReferenceEquals(sender, LightThemeBox));

    private void OnStagingSample(object? sender, RoutedEventArgs e) => LoadStagingSample();

    private void OnLargeFile(object? sender, RoutedEventArgs e) => LoadLargeStagingFile();

    private void OnClearSelection(object? sender, RoutedEventArgs e) => DiffEditor.ClearSelection();

    private void OnSelectAllAdded(object? sender, RoutedEventArgs e) => SelectAllAdded();
}
