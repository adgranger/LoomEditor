using LoomEditor.Core.Model;
using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Editor;

/// <summary>Keeps the server's copy of a document equal to the editor's model: <c>didOpen</c>, <c>didChange</c> and <c>didClose</c>.</summary>
/// <remarks>Threading: UI thread only (it subscribes to the model); the notifications it sends are queued and written by the connection's writer loop.</remarks>
internal sealed class LspDocumentSync(LspClient client, TextModel model, string documentUri, string languageId) : IDisposable
{
    private int _change;
    private bool _subscribed;

    public bool IsOpen { get; private set; }

    public bool IsFaulted { get; private set; }

    public string DocumentUri { get; } = documentUri;

    public int SentVersion { get; private set; }

    public void Open()
    {
        if (IsOpen)
        {
            return;
        }

        if (client.State != LspClientState.Running)
        {
            throw new InvalidOperationException("The language server is not running");
        }

        (bool openClose, int change) = client.TextDocumentSync;
        if (!openClose)
        {
            return;
        }

        _change = change;
        TextDocumentItem item = new() { Uri = DocumentUri, LanguageId = languageId, Version = model.VersionId, Text = model.GetValue() };
        client.Notify(LspMethods.DidOpen, new DidOpenTextDocumentParams { TextDocument = item }, LspJsonContext.Default.DidOpenTextDocumentParams);
        SentVersion = model.VersionId;
        IsOpen = true;
        model.ContentChanged += OnContentChanged;
        _subscribed = true;
    }

    public void Close()
    {
        if (_subscribed)
        {
            model.ContentChanged -= OnContentChanged;
            _subscribed = false;
        }

        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        if (client.State != LspClientState.Running)
        {
            return;
        }

        try
        {
            client.Notify(
                LspMethods.DidClose,
                new DidCloseTextDocumentParams { TextDocument = new TextDocumentIdentifier { Uri = DocumentUri } },
                LspJsonContext.Default.DidCloseTextDocumentParams);
        }
        catch (LspClientException)
        {
            // The server is gone: nothing to close.
        }
    }

    public void Dispose() => Close();

    private void OnContentChanged(object? sender, ModelContentChangedEventArgs e)
    {
        if (!IsOpen || IsFaulted)
        {
            return;
        }

        SentVersion = e.VersionId;
        if (_change == 0)
        {
            return;
        }

        TextDocumentContentChangeEvent[] changes;
        if (e.IsFlush || e.IsEolChange || _change == 1)
        {
            changes = [new TextDocumentContentChangeEvent { Text = model.GetValue() }];
        }
        else
        {
            changes = new TextDocumentContentChangeEvent[e.Changes.Count];
            for (int i = 0; i < changes.Length; i++)
            {
                ModelContentChange c = e.Changes[i];
                changes[i] = new TextDocumentContentChangeEvent { Range = LspConvert.ToLsp(c.Range), RangeLength = c.RangeLength, Text = c.Text };
            }
        }

        try
        {
            client.Notify(
                LspMethods.DidChange,
                new DidChangeTextDocumentParams
                {
                    TextDocument = new VersionedTextDocumentIdentifier { Uri = DocumentUri, Version = e.VersionId },
                    ContentChanges = changes,
                },
                LspJsonContext.Default.DidChangeTextDocumentParams);
        }
        catch (LspClientException)
        {
            IsFaulted = true;
        }
    }
}
