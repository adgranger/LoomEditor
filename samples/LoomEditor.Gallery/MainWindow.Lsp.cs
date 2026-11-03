using LoomEditor.Gallery.Lsp;

namespace LoomEditor.Gallery;

// Language server section of the Gallery (phase 8).
//
// Language server
//   Start language server connects the editor to csharp-ls (`dotnet tool install --global csharp-ls`) or to the stdio server named by
//   LOOMEDITOR_LSP_COMMAND (arguments allowed, double quotes group a path with spaces) with the language id LOOMEDITOR_LSP_LANGUAGE (default csharp).
//   Demo providers / Demo markers need no server. `LoomEditor.Gallery.exe --lsp-selfcheck=<file>` drives the whole stack and writes a PASS / FAIL report.
public sealed partial class MainWindow
{
    private const string SelfCheckArgument = "--lsp-selfcheck=";
    private const int MaxLogLines = 200;
    private readonly List<string> _logLines = [];
    private LspDemoController? _lsp;
    private IDisposable? _demoProviders;
    private bool _demoMarkers;
    private bool _closing;
    private string? _selfCheckFile;

    private void SetLspStatus(string text)
    {
        LspStatus.Text = text;
        RefreshLspButtons();
    }

    private void RefreshLspButtons()
    {
        bool running = _lsp?.IsRunning == true;
        StartLspButton.IsEnabled = !running;
        StopLspButton.IsEnabled = running;
    }

    private void AppendLspLog(string line)
    {
        _logLines.Add(line);
        if (_logLines.Count > MaxLogLines)
        {
            _logLines.RemoveRange(0, _logLines.Count - MaxLogLines);
        }

        LspLog.Text = string.Join('\n', _logLines);
        ScrollLspLogToEnd();
    }
}
