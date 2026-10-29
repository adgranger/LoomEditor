using Microsoft.UI.Xaml;

namespace LoomEditor.Gallery;

public sealed partial class MainWindow
{
    private const string CommentsSelfCheckArgument = "--comments-selfcheck=";
    private string? _commentsSelfCheckFile;

    // `LoomEditor.Gallery.exe --comments-selfcheck=<file>` runs CommentsSelfCheck on the UI thread and writes a PASS / FAIL report (the same file
    // rules as --text-selfcheck): exit code 0 when every check passed, else 1.
    private void InitializeCommentsSelfCheck()
    {
        _commentsSelfCheckFile = Environment.GetCommandLineArgs()
            .FirstOrDefault(argument => argument.StartsWith(CommentsSelfCheckArgument, StringComparison.Ordinal))?[CommentsSelfCheckArgument.Length..];
        if (_commentsSelfCheckFile is not null)
        {
            Activated += OnCommentsSelfCheckActivated;
        }
    }

    private void OnCommentsSelfCheckActivated(object sender, WindowActivatedEventArgs args)
    {
        Activated -= OnCommentsSelfCheckActivated;
        int failures = 1;
        try
        {
            failures = CommentsSelfCheck.RunToFile(_commentsSelfCheckFile!);
        }
        catch (Exception ex)
        {
            File.WriteAllText(_commentsSelfCheckFile!, $"LoomEditor comments self-check\nFAIL comments-selfcheck — {ex.GetType().Name}: {ex.Message}\n0 of 1 checks passed\n");
        }

        Environment.Exit(failures == 0 ? 0 : 1);
    }
}
