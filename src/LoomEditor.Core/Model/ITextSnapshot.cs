// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model;

/// <summary>
/// Text snapshot that works like an iterator. It returns chunks of text and <see langword="null"/> when finished.
/// </summary>
/// <remarks>
/// <para>Monaco: <c>ITextSnapshot</c>.</para>
/// <para>Threading: an instance is a cursor and must not be shared between threads without synchronization.</para>
/// </remarks>
public interface ITextSnapshot
{
    /// <summary>Reads the next chunk of text, or <see langword="null"/> at the end (Monaco: <c>read</c>).</summary>
    /// <returns>The next chunk, or <see langword="null"/> when the snapshot is exhausted.</returns>
    string? Read();
}
