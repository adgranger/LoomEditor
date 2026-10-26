namespace LoomEditor.Lsp.Editor;

/// <summary>The lifecycle state of an <see cref="LspEditorSession"/>.</summary>
public enum LspSessionState
{
    /// <summary>The session was created and not started.</summary>
    NotStarted,

    /// <summary>The server is starting and the handshake is running.</summary>
    Starting,

    /// <summary>The server runs and the document is open.</summary>
    Running,

    /// <summary>The session is shutting the server down.</summary>
    Stopping,

    /// <summary>The server is shut down and everything the session added to the editor is removed.</summary>
    Stopped,

    /// <summary>The server crashed or failed to start; everything the session added to the editor is removed.</summary>
    Faulted,
}
