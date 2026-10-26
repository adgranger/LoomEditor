using LoomEditor.Lsp.Client;

namespace LoomEditor.Lsp.Editor;

public sealed partial class LspEditorSession
{
    private const string NoSyncMessage = "The server did not declare text document synchronization: language features are off.";

    /// <summary>UI thread: opens the document and registers the providers the server supports.</summary>
    private void FinishStart(LspClient client, TaskCompletionSource done)
    {
        try
        {
            if (State != LspSessionState.Starting)
            {
                // Stopped or faulted meanwhile.
                done.TrySetResult();
                _ = ShutDownAsync(client);
                return;
            }

            _sync = new LspDocumentSync(client, _model, _documentUri, _languageId);
            _sync.Open();
            if (!client.TextDocumentSync.OpenClose)
            {
                ServerOutput?.Invoke(NoSyncMessage);
            }
            else
            {
                _binder = new LspDiagnosticsBinder(client, _model, _markers, _documentUri, _post, _options.DiagnosticsOwner);
                if (client.SupportsCompletion)
                {
                    _registrations.Add(_features.CompletionProviders.Register(_languageId, new LspCompletionProvider(client, _documentUri)));
                }

                if (client.SupportsHover)
                {
                    _registrations.Add(_features.HoverProviders.Register(_languageId, new LspHoverProvider(client, _documentUri)));
                }

                if (client.SupportsDefinition)
                {
                    _registrations.Add(_features.DefinitionProviders.Register(_languageId, new LspDefinitionProvider(client, _documentUri)));
                }
            }

            SetState(LspSessionState.Running);
            done.TrySetResult();
        }
        catch (Exception e)
        {
            Fault(e);
            done.TrySetException(e);
        }
    }

    /// <summary>UI thread, idempotent: removes everything the session added and ends in <see cref="LspSessionState.Faulted"/>.</summary>
    private void Fault(Exception? reason)
    {
        if (State is not (LspSessionState.Starting or LspSessionState.Running))
        {
            return;
        }

        Teardown();
        string message = reason?.Message ?? "The language server stopped unexpectedly";
        _faultMessage = message;
        SetState(LspSessionState.Faulted);
        ServerOutput?.Invoke(message);
        if (_client is { } client)
        {
            _ = ShutDownAsync(client);
        }
    }

    /// <summary>UI thread: unregisters the providers, stops syncing and clears the markers.</summary>
    private void Teardown()
    {
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
        _sync?.Dispose();
        _binder?.Dispose();
        _sync = null;
        _binder = null;
    }

    private async Task StopCoreAsync()
    {
        SetState(LspSessionState.Stopping);
        _startCts?.Cancel();
        Teardown();
        if (_client is { } client)
        {
            try
            {
                await client.ShutdownAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // A server that does not shut down politely is killed by DisposeAsync.
            }

            await ShutDownAsync(client).ConfigureAwait(false);
        }

        TaskCompletionSource stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (_post(() =>
            {
                SetState(LspSessionState.Stopped);
                stopped.TrySetResult();
            }))
        {
            await stopped.Task.ConfigureAwait(false);
        }
        else
        {
            Volatile.Write(ref _state, (int)LspSessionState.Stopped);
        }
    }

    private static async Task ShutDownAsync(LspClient client)
    {
        try
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Nothing left to do for a server that cannot be disposed.
        }
    }
}
