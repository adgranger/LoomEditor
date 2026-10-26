using LoomEditor.Core.Markers;
using LoomEditor.Core.Model;
using LoomEditor.Lsp.Client;
using LoomEditor.Lsp.Protocol;

namespace LoomEditor.Lsp.Editor;

/// <summary>Turns the diagnostics the server publishes for the open document into markers, dropping the ones computed for an older text.</summary>
/// <remarks>Threading: the constructor and Dispose on the UI thread; the event handler on the read loop thread; Apply on the UI thread.</remarks>
internal sealed class LspDiagnosticsBinder : IDisposable
{
    private readonly LspClient _client;
    private readonly TextModel _model;
    private readonly MarkerService _markers;
    private readonly string _documentUri;
    private readonly Func<Action, bool> _post;
    private readonly string _owner;
    private volatile bool _disposed;

    public LspDiagnosticsBinder(LspClient client, TextModel model, MarkerService markers, string documentUri, Func<Action, bool> post, string owner = "lsp")
    {
        _client = client;
        _model = model;
        _markers = markers;
        _documentUri = documentUri;
        _post = post;
        _owner = owner;
        _client.DiagnosticsReceived += OnDiagnostics;
    }

    /// <summary>Removes the owner's markers.</summary>
    public void Clear() => _markers.Remove(_owner);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _client.DiagnosticsReceived -= OnDiagnostics;
        _disposed = true;
        Clear();
    }

    private void OnDiagnostics(PublishDiagnosticsParams p)
    {
        if (!string.Equals(p.Uri, _documentUri, StringComparison.Ordinal))
        {
            return;
        }

        _post(() => Apply(p));
    }

    private void Apply(PublishDiagnosticsParams p)
    {
        if (_disposed || (p.Version is { } version && version != _model.VersionId))
        {
            return;
        }

        _markers.Change(_owner, [.. p.Diagnostics.Select(d => LspConvert.ToMarker(d, _model, p.Version))]);
    }
}
