using Microsoft.UI.Xaml;

namespace LoomEditor.Gallery;

public sealed partial class MainWindow
{
    private const string HostApiSelfCheckArgument = "--hostapi-selfcheck=";
    private string? _hostApiSelfCheckFile;

    // `LoomEditor.Gallery.exe --hostapi-selfcheck=<file>` runs HostApiSelfCheck on the UI thread and writes a PASS / FAIL report (the same file
    // rules as --text-selfcheck): exit code 0 when every check passed, else 1.
    private void InitializeHostApiSelfCheck()
    {
        _hostApiSelfCheckFile = Environment.GetCommandLineArgs()
            .FirstOrDefault(argument => argument.StartsWith(HostApiSelfCheckArgument, StringComparison.Ordinal))?[HostApiSelfCheckArgument.Length..];
        if (_hostApiSelfCheckFile is not null)
        {
            Activated += OnHostApiSelfCheckActivated;
        }
    }

    private void OnHostApiSelfCheckActivated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= OnHostApiSelfCheckActivated;
        int failures = 1;
        try
        {
            failures = HostApiSelfCheck.RunToFile(_hostApiSelfCheckFile!);
        }
        catch (Exception ex)
        {
            File.WriteAllText(_hostApiSelfCheckFile!, $"LoomEditor host API self-check\nFAIL hostapi-selfcheck — {ex.GetType().Name}: {ex.Message}\n0 of 1 checks passed\n");
        }

        Environment.Exit(failures == 0 ? 0 : 1);
    }
}
