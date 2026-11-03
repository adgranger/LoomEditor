
namespace LoomEditor.Gallery;

public sealed partial class MainWindow
{
    private const string TextSelfCheckArgument = "--text-selfcheck=";
    private string? _textSelfCheckFile;

    // `LoomEditor.Gallery.Avalonia --text-selfcheck=<file>` runs TextSelfCheck on the UI thread and writes a PASS / FAIL report (the same file rules as
    // --diff-selfcheck): exit code 0 when every check passed, else 1.
    private void InitializeTextSelfCheck()
    {
        _textSelfCheckFile = Environment.GetCommandLineArgs()
            .FirstOrDefault(argument => argument.StartsWith(TextSelfCheckArgument, StringComparison.Ordinal))?[TextSelfCheckArgument.Length..];
        if (_textSelfCheckFile is not null)
        {
            Opened += OnTextSelfCheckOpened;
        }
    }

    private void OnTextSelfCheckOpened(object? sender, EventArgs args)
    {
        Opened -= OnTextSelfCheckOpened;
        int failures = 1;
        try
        {
            failures = TextSelfCheck.RunToFile(_textSelfCheckFile!);
        }
        catch (Exception ex)
        {
            File.WriteAllText(_textSelfCheckFile!, $"LoomEditor text self-check\nFAIL text-selfcheck — {ex.GetType().Name}: {ex.Message}\n0 of 1 checks passed\n");
        }

        Environment.Exit(failures == 0 ? 0 : 1);
    }
}
