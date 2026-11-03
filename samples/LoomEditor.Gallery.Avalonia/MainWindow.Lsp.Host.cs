using LoomEditor.Gallery.Lsp;
using LoomEditor.Lsp.Editor;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LoomEditor.Gallery;

// Language server section of the Gallery: the framework part (events, window lifetime, caret).
public sealed partial class MainWindow
{
    private void InitializeLsp()
    {
        _lsp = new LspDemoController(Editor, SetLspStatus, AppendLspLog);
        Editor.LocationOpener = new GalleryLocationOpener(text => StatusText.Text = text);
        Closing += OnWindowClosing;
        _selfCheckFile = Environment.GetCommandLineArgs()
            .FirstOrDefault(argument => argument.StartsWith(SelfCheckArgument, StringComparison.Ordinal))?[SelfCheckArgument.Length..];
        if (_selfCheckFile is not null)
        {
            Opened += OnFirstOpened;
        }
    }

    private async void OnStartLsp(object? sender, RoutedEventArgs e)
    {
        await _lsp!.StartAsync();
        RefreshLspButtons();
    }

    private async void OnStopLsp(object? sender, RoutedEventArgs e)
    {
        await _lsp!.StopAsync();
        RefreshLspButtons();
    }

    private void OnDemoProviders(object? sender, RoutedEventArgs e)
    {
        if (_demoProviders is null)
        {
            _demoProviders = DemoProviders.Register(Editor.LanguageFeatures, Editor.LanguageId, Editor.DocumentUri);
        }
        else
        {
            _demoProviders.Dispose();
            _demoProviders = null;
        }

        DemoProvidersButton.Content = _demoProviders is null ? "Demo providers: off" : "Demo providers: on";
    }

    private void OnDemoMarkers(object? sender, RoutedEventArgs e)
    {
        _demoMarkers = !_demoMarkers;
        if (_demoMarkers)
        {
            DemoMarkers.Set(Editor.Markers, Editor.Model);
        }
        else
        {
            DemoMarkers.Clear(Editor.Markers);
        }

        DemoMarkersButton.Content = _demoMarkers ? "Demo markers: on" : "Demo markers: off";
    }

    private void OnLoadLspSample(object? sender, RoutedEventArgs e)
    {
        Editor.LanguageId = "csharp";
        LanguageBox.SelectedIndex = Array.IndexOf(LanguageIds, "csharp");
        Editor.Text = string.Empty;
        Editor.Text = LspDemoSample.Text;
    }

    private async void OnFirstOpened(object? sender, EventArgs args)
    {
        Opened -= OnFirstOpened;
        int failures = 1;
        try
        {
            failures = await LspSelfCheck.RunAsync(Editor, _selfCheckFile!, SetLspStatus, AppendLspLog);
        }
        catch (Exception ex)
        {
            File.WriteAllText(_selfCheckFile!, $"FAIL self-check: {ex.GetType().Name}: {ex.Message}\n");
        }

        Environment.Exit(failures == 0 ? 0 : 1);
    }

    // The server process must not outlive the window: stop it first (at most 3 seconds), then really close.
    // The close is postponed (Handled) instead of blocking the UI thread, which the session needs to finish stopping.
    private async void OnWindowClosing(object? sender, WindowClosingEventArgs args)
    {
        if (_closing || _lsp is null || _lsp.State == LspSessionState.NotStarted)
        {
            return;
        }

        args.Cancel = true;
        _closing = true;
        try
        {
            await _lsp.StopAsync().WaitAsync(TimeSpan.FromSeconds(3));
        }
        catch (TimeoutException)
        {
            // Give up: the process is killed with the app.
        }

        Close();
    }

    private void ScrollLspLogToEnd() => LspLog.CaretIndex = LspLog.Text!.Length;
}
