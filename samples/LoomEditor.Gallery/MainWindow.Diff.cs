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
//   `LoomEditor.Gallery.exe --diff-selfcheck=<file>` runs DiffSelfCheck (algorithm, provider, view model, alignment, accessible model) and writes a PASS / FAIL report.
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

    private const string DiffSelfCheckArgument = "--diff-selfcheck=";
    private string? _diffSelfCheckFile;

    // `LoomEditor.Gallery.exe --diff-selfcheck=<file>` runs DiffSelfCheck on the real dispatcher and writes a PASS / FAIL report (the same
    // file rules as --lsp-selfcheck): exit code 0 when every check passed, else 1.
    private void InitializeDiffSelfCheck()
    {
        _diffSelfCheckFile = Environment.GetCommandLineArgs()
            .FirstOrDefault(argument => argument.StartsWith(DiffSelfCheckArgument, StringComparison.Ordinal))?[DiffSelfCheckArgument.Length..];
        if (_diffSelfCheckFile is not null)
        {
            Activated += OnDiffSelfCheckActivated;
        }
    }

    private async void OnDiffSelfCheckActivated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= OnDiffSelfCheckActivated;
        int failures = 1;
        try
        {
            // The hang guard: a check that never completes must not keep the process alive.
            failures = await DiffSelfCheck.RunToFileAsync(_diffSelfCheckFile!, new QueueDispatcher(DispatcherQueue), new QueueScheduler(DispatcherQueue))
                .WaitAsync(TimeSpan.FromSeconds(120));
        }
        catch (Exception ex)
        {
            File.WriteAllText(_diffSelfCheckFile!, $"FAIL  diff-selfcheck — {ex.GetType().Name}: {ex.Message}\nSUMMARY: 0 passed, 1 failed\n");
        }

        Environment.Exit(failures == 0 ? 0 : 1);
    }

    private sealed class QueueDispatcher(Microsoft.UI.Dispatching.DispatcherQueue queue) : LoomEditor.Core.Threading.IUiDispatcher
    {
        public bool Post(Action action) => queue.TryEnqueue(() => action());
    }

    private sealed class QueueScheduler(Microsoft.UI.Dispatching.DispatcherQueue queue) : LoomEditor.View.Features.Scheduling.IEditorScheduler
    {
        public IDisposable Schedule(TimeSpan delay, Action action)
        {
            Microsoft.UI.Dispatching.DispatcherQueueTimer timer = queue.CreateTimer();
            timer.Interval = delay;
            timer.IsRepeating = false;
            timer.Tick += (sender, _) =>
            {
                sender.Stop();
                action();
            };
            timer.Start();
            return new TimerStop(timer);
        }

        private sealed class TimerStop(Microsoft.UI.Dispatching.DispatcherQueueTimer timer) : IDisposable
        {
            public void Dispose() => timer.Stop();
        }
    }
}
