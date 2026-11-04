using Avalonia.Interactivity;

namespace LoomEditor.Gallery;

public sealed partial class DiffDemoWindow
{
    private void OnAddOnOriginal(object? sender, RoutedEventArgs e) => AddOnSelection(DiffSide.Original, DiffEditor.Original);

    private void OnAddOnModified(object? sender, RoutedEventArgs e) => AddOnSelection(DiffSide.Modified, DiffEditor.Modified);

    private void OnCommentGutterChanged(object? sender, RoutedEventArgs e) => ApplyCommentGutter();

    private void OnLoadCommentSample(object? sender, RoutedEventArgs e) => LoadCommentSample();
}
