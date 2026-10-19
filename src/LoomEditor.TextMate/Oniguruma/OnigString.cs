// Ported from microsoft/vscode-oniguruma: src/index.ts
// Upstream commit: 716aeaa229e4ae2e3b0057377b55743e9a3e995b  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Oniguruma;

/// <summary>
/// vscode-oniguruma's <c>OnigString</c>: a subject with a process-unique id, used by the scanner's per-regex cache.
/// </summary>
/// <remarks>
/// <para>
/// No native copy and no UTF-8 buffers: Oniguruma runs in UTF-16 LE (ADR 0007, decision 2), so the .NET string is
/// pinned for each search. <see cref="Dispose"/> does nothing.
/// </para>
/// <para>Threading: immutable; ids are unique across threads.</para>
/// </remarks>
internal sealed class OnigString : IOnigString
{
    private static int _lastId;

    /// <summary>Creates a string with a new id.</summary>
    public OnigString(string str)
    {
        ArgumentNullException.ThrowIfNull(str);
        Content = str;
        Id = Interlocked.Increment(ref _lastId);
    }

    /// <summary><c>id</c>: unique in the process, never 0.</summary>
    public int Id { get; }

    /// <inheritdoc />
    public string Content { get; }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
