using Avalonia.Interactivity;
using LoomEditor.Avalonia.Threading;

namespace LoomEditor.Gallery;

// Diff editor section of the Gallery (phase 9).
//
// Diff editor
//   "Diff editor window" opens DiffDemoWindow: a LoomDiffEditor over two sample files (DiffSamples) with a switch for every option
//   (side by side, ignore trim whitespace, hide unchanged regions, indicators, editable original, read-only modified, overview ruler),
//   buttons for the previous / next change, the accessible viewer, collapse / show all, swap sides, reset texts and "Large files"
//   (two generated texts of 5 000 lines). Resize the window below 900 DIPs to see the switch to the inline view.
//   "Diff editor preview" swaps a LoomDiffEditor in place of the editor of this window. The manual plan is docs/testing/phase-9-manual-tests.md.
//   `LoomEditor.Gallery.Avalonia --diff-selfcheck=<file>` runs DiffSelfCheck (algorithm, provider, view model, alignment, accessible model) and writes a PASS / FAIL report.
public sealed partial class MainWindow
{
    private void OnOpenDiffWindow(object? sender, RoutedEventArgs e) => new DiffDemoWindow(Editor.Theme).Show();

    private void OnOpenHostApiWindow(object? sender, RoutedEventArgs e) => new HostApiDemoWindow(Editor.Theme).Show();

    // Switches set in XAML raise their handlers during InitializeComponent, before the controls exist: guard on DiffEditor.
    private void OnDiffToggled(object? sender, RoutedEventArgs e)
    {
        bool on = DiffSwitch.IsChecked == true;
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

        DiffEditor.IsVisible = on;
        Editor.IsVisible = !on;
        DiffStatusText.Text = on ? "Diff computing…" : string.Empty;
    }

    private void OnDiffChanged(object? sender, EventArgs e) => DiffStatusText.Text = $"{DiffEditor.ChangeCount} change(s)";

    private void OnDiffOptionToggled(object? sender, RoutedEventArgs e) => ApplyDiffOptions();

    private void ApplyDiffOptions()
    {
        if (DiffEditor is null)
        {
            return;
        }

        DiffEditor.RenderSideBySide = DiffSideBySideSwitch.IsChecked == true;
        DiffEditor.HideUnchangedRegions = DiffHideUnchangedSwitch.IsChecked == true;
        DiffEditor.IgnoreTrimWhitespace = DiffIgnoreWhitespaceSwitch.IsChecked == true;
        DiffEditor.IsReadOnly = DiffReadOnlySwitch.IsChecked == true;
    }

    private void OnDiffCollapseAll(object? sender, RoutedEventArgs e) => DiffEditor.CollapseAllUnchangedRegions();

    private void OnDiffShowAll(object? sender, RoutedEventArgs e) => DiffEditor.ShowAllUnchangedRegions();

    private const string DiffSelfCheckArgument = "--diff-selfcheck=";
    private string? _diffSelfCheckFile;

    // `LoomEditor.Gallery.Avalonia --diff-selfcheck=<file>` runs DiffSelfCheck on the real dispatcher and writes a PASS / FAIL report (the same
    // file rules as --lsp-selfcheck): exit code 0 when every check passed, else 1.
    private void InitializeDiffSelfCheck()
    {
        _diffSelfCheckFile = Environment.GetCommandLineArgs()
            .FirstOrDefault(argument => argument.StartsWith(DiffSelfCheckArgument, StringComparison.Ordinal))?[DiffSelfCheckArgument.Length..];
        if (_diffSelfCheckFile is not null)
        {
            Opened += OnDiffSelfCheckOpened;
        }
    }

    private async void OnDiffSelfCheckOpened(object? sender, EventArgs args)
    {
        Opened -= OnDiffSelfCheckOpened;
        int failures = 1;
        try
        {
            // The hang guard: a check that never completes must not keep the process alive.
            failures = await DiffSelfCheck.RunToFileAsync(_diffSelfCheckFile!, new DispatcherUiDispatcher(), new DispatcherEditorScheduler())
                .WaitAsync(TimeSpan.FromSeconds(120));
        }
        catch (Exception ex)
        {
            File.WriteAllText(_diffSelfCheckFile!, $"FAIL  diff-selfcheck — {ex.GetType().Name}: {ex.Message}\nSUMMARY: 0 passed, 1 failed\n");
        }

        Environment.Exit(failures == 0 ? 0 : 1);
    }
}
