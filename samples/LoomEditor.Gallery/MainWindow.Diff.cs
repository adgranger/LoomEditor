using Microsoft.UI.Xaml;

namespace LoomEditor.Gallery;

/// <summary>The phase 9 preview: a <c>LoomDiffEditor</c> swapped in place of the editor.</summary>
public sealed partial class MainWindow
{
    // Switches set in XAML raise their handlers during InitializeComponent, before the controls exist: guard on DiffEditor.
    private void OnDiffToggled(object sender, RoutedEventArgs e)
    {
        bool on = DiffSwitch.IsOn;
        DiffEditor.DiffChanged -= OnDiffChanged;
        if (on)
        {
            DiffEditor.DiffChanged += OnDiffChanged;
            DiffEditor.LanguageId = "csharp";
            DiffEditor.Theme = Editor.Theme;
            DiffEditor.OriginalText = DiffSamples.Original;
            DiffEditor.ModifiedText = DiffSamples.Modified;
            ApplyDiffOptions();
        }

        DiffEditor.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
        Editor.Visibility = on ? Visibility.Collapsed : Visibility.Visible;
        DiffStatusText.Text = on ? "Diff computing…" : string.Empty;
    }

    private void OnDiffChanged(object? sender, EventArgs e) => DiffStatusText.Text = $"{DiffEditor.ChangeCount} change(s)";

    private void OnDiffOptionToggled(object sender, RoutedEventArgs e) => ApplyDiffOptions();

    private void ApplyDiffOptions()
    {
        if (DiffEditor is null)
        {
            return;
        }

        DiffEditor.RenderSideBySide = DiffSideBySideSwitch.IsOn;
        DiffEditor.HideUnchangedRegions = DiffHideUnchangedSwitch.IsOn;
        DiffEditor.IgnoreTrimWhitespace = DiffIgnoreWhitespaceSwitch.IsOn;
        DiffEditor.IsReadOnly = DiffReadOnlySwitch.IsOn;
    }

    private void OnDiffCollapseAll(object sender, RoutedEventArgs e) => DiffEditor.CollapseAllUnchangedRegions();

    private void OnDiffShowAll(object sender, RoutedEventArgs e) => DiffEditor.ShowAllUnchangedRegions();
}
