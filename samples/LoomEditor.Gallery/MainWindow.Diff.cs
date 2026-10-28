using Microsoft.UI.Xaml;

namespace LoomEditor.Gallery;

// Diff editor section of the Gallery (phase 9).
//
// Diff editor
//   "Diff editor window" opens DiffDemoWindow: a LoomDiffEditor over two sample files (DiffSamples) with a switch for every option
//   (side by side, ignore trim whitespace, hide unchanged regions, indicators, editable original, read-only modified, overview ruler),
//   buttons for the previous / next change, the accessible viewer, collapse / show all, swap sides, reset texts and "Large files"
//   (two generated texts of 5 000 lines). Resize the window below 900 DIPs to see the switch to the inline view.
//   "Diff editor preview" swaps a LoomDiffEditor in place of the editor of this window. The manual plan is docs/testing/phase-9-manual-tests.md.
public sealed partial class MainWindow
{
    private void OnOpenDiffWindow(object sender, RoutedEventArgs e) => new DiffDemoWindow(Editor.Theme).Activate();

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
