using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.Editor;
using LoomEditorControl = LoomEditor.WinUI.Controls.LoomEditor;

namespace LoomEditor.Gallery.Lsp;

/// <summary>Starts and stops the language server session of the Gallery's editor, reporting through a status line and a log.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class LspDemoController(LoomEditorControl editor, Action<string> setStatus, Action<string> appendLog)
{
    private LspEditorSession? _session;
    private string _description = string.Empty;

    /// <summary>Whether the session is starting or running.</summary>
    internal bool IsRunning => State is LspSessionState.Starting or LspSessionState.Running;

    /// <summary>The state of the current session (<see cref="LspSessionState.NotStarted"/> without one).</summary>
    internal LspSessionState State => _session?.State ?? LspSessionState.NotStarted;

    /// <summary>Finds a server and connects the editor to it. Never throws: a problem becomes the status text.</summary>
    internal async Task StartAsync()
    {
        if (IsRunning)
        {
            return;
        }

        // A faulted or stopped session is replaced: clean it up first.
        await StopAsync().ConfigureAwait(true);
        LspServerChoice? choice = LspServerLocator.Find();
        if (choice is null)
        {
            setStatus($"Language server not found. Install it with: {LspServerLocator.InstallHint} (or set LOOMEDITOR_LSP_COMMAND)");
            return;
        }

        try
        {
            LspDemoWorkspaceInfo info = LspDemoWorkspace.Create(LspDemoWorkspace.DefaultBaseFolder, editor.Text);
            editor.DocumentUri = info.ProgramUri;
            editor.LanguageId = choice.LanguageId;
            _description = choice.Description;
            LspEditorSession session = new(
                editor.Model,
                editor.LanguageFeatures,
                editor.Markers,
                info.ProgramUri,
                choice.LanguageId,
                new LspServerCommand(choice.FileName, choice.Arguments, info.Folder),
                action => editor.DispatcherQueue.TryEnqueue(() => action()),
                new LspSessionOptions { RootUri = info.FolderUri, ClientVersion = "gallery" });
            session.StateChanged += state => setStatus(Describe(state, session));
            session.ServerOutput += appendLog;
            _session = session;
            await session.StartAsync().ConfigureAwait(true);
        }
        catch (Exception e)
        {
            setStatus($"Failed to start: {e.Message}");
        }
    }

    /// <summary>Stops the server and removes what the session added to the editor.</summary>
    internal async Task StopAsync()
    {
        LspEditorSession? session = _session;
        if (session is null)
        {
            return;
        }

        try
        {
            await session.StopAsync().ConfigureAwait(true);
        }
        catch (Exception e)
        {
            setStatus($"Failed to stop: {e.Message}");
        }
        finally
        {
            _session = null;
        }
    }

    private string Describe(LspSessionState state, LspEditorSession session) => state switch
    {
        LspSessionState.Starting => "Starting…",
        LspSessionState.Running => $"Running ({_description})",
        LspSessionState.Stopping => "Stopping…",
        LspSessionState.Stopped => "Stopped",
        LspSessionState.Faulted => $"Faulted: {session.FaultMessage}",
        _ => state.ToString(),
    };
}
