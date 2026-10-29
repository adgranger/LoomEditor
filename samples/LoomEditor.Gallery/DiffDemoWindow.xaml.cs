using LoomEditor.Themes;
using Microsoft.UI.Xaml;

namespace LoomEditor.Gallery;

/// <summary>The diff editor demo (phase 9): a <c>LoomDiffEditor</c> over two sample files, with a switch for every option.</summary>
public sealed partial class DiffDemoWindow : Window
{
    /// <summary>Opens the window on the sample files, with the theme of the main window.</summary>
    /// <param name="theme">The theme of both editors.</param>
    public DiffDemoWindow(EditorTheme theme)
    {
        InitializeComponent();
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));
        DiffEditor.LanguageId = "csharp";
        DiffEditor.Theme = theme;
        DiffEditor.DiffChanged += OnDiffChanged;
        OnResetTexts(this, new RoutedEventArgs());
        OnOptionChanged(this, new RoutedEventArgs());
        OnDiffChanged(this, EventArgs.Empty);
        InitializeComments();
        Closed += (_, _) => DiffEditor.DiffChanged -= OnDiffChanged;
    }

    // The CheckBoxes raise nothing before InitializeComponent is done, but the guard keeps the handler safe to call from anywhere.
    private void OnOptionChanged(object sender, RoutedEventArgs e)
    {
        if (DiffEditor is null)
        {
            return;
        }

        DiffEditor.RenderSideBySide = SideBySideBox.IsChecked == true;
        DiffEditor.IgnoreTrimWhitespace = IgnoreTrimWhitespaceBox.IsChecked == true;
        DiffEditor.HideUnchangedRegions = HideUnchangedRegionsBox.IsChecked == true;
        DiffEditor.RenderIndicators = RenderIndicatorsBox.IsChecked == true;
        DiffEditor.OriginalEditable = OriginalEditableBox.IsChecked == true;
        DiffEditor.IsReadOnly = ReadOnlyBox.IsChecked == true;
        DiffEditor.RenderOverviewRuler = OverviewRulerBox.IsChecked == true;
    }

    private void OnDiffChanged(object? sender, EventArgs e) =>
        StatusText.Text = $"{DiffEditor.ChangeCount} change(s) — diff up to date: {DiffEditor.IsDiffUpToDate}";

    private void OnPreviousChange(object sender, RoutedEventArgs e) => DiffEditor.GoToPreviousChange();

    private void OnNextChange(object sender, RoutedEventArgs e) => DiffEditor.GoToNextChange();

    private void OnAccessibleViewer(object sender, RoutedEventArgs e) => DiffEditor.OpenAccessibleDiffViewer();

    private void OnCollapseAll(object sender, RoutedEventArgs e) => DiffEditor.CollapseAllUnchangedRegions();

    private void OnShowAll(object sender, RoutedEventArgs e) => DiffEditor.ShowAllUnchangedRegions();

    private void OnSwapSides(object sender, RoutedEventArgs e)
    {
        string original = DiffEditor.OriginalText;
        DiffEditor.OriginalText = DiffEditor.ModifiedText;
        DiffEditor.ModifiedText = original;
    }

    private void OnResetTexts(object sender, RoutedEventArgs e)
    {
        DiffEditor.OriginalText = DiffSamples.Original;
        DiffEditor.ModifiedText = DiffSamples.Modified;
    }

    private void OnLargeFiles(object sender, RoutedEventArgs e)
    {
        (string original, string modified) = DiffSamples.CreateLargePair();
        DiffEditor.OriginalText = original;
        DiffEditor.ModifiedText = modified;
    }
}
