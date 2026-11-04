using System.Diagnostics;
using System.Globalization;

namespace LoomEditor.Gallery;

public sealed partial class MainWindow
{
    private bool _isBusy;

    // Generates on a background thread; only the assignment of the text happens on the UI thread.
    private async Task LoadDocumentAsync(Func<string> create)
    {
        if (!BeginBusy("Generating…"))
        {
            return;
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();
            string text = await Task.Run(create);
            Editor.ScrollTop = 0;
            Editor.ScrollLeft = 0;
            Editor.Text = text;
            await NextRenderingAsync();
            stopwatch.Stop();
            StatusText.Text = string.Create(
                CultureInfo.InvariantCulture, $"Loaded {Editor.Model.LineCount:N0} lines in {stopwatch.ElapsedMilliseconds:N0} ms");
        }
        finally
        {
            EndBusy();
        }
    }

    private bool BeginBusy(string message)
    {
        if (_isBusy)
        {
            return false;
        }

        _isBusy = true;
        SetSampleButtonsEnabled(false);
        StatusText.Text = message;
        return true;
    }

    private void EndBusy()
    {
        _isBusy = false;
        SetSampleButtonsEnabled(true);
    }

    private void SetSampleButtonsEnabled(bool isEnabled)
    {
        LargeFileButton.IsEnabled = isEnabled;
        LongLineButton.IsEnabled = isEnabled;
        UnicodeButton.IsEnabled = isEnabled;
        ScrollTestButton.IsEnabled = isEnabled;
        MultiCursorButton.IsEnabled = isEnabled;
        LargeCSharpButton.IsEnabled = isEnabled;
        FeatureSampleBox.IsEnabled = isEnabled;
    }
}
