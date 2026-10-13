// Ported from microsoft/vscode: src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts (helper createTextBuffer)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.PieceTree;

namespace LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

internal static class TestTextBuffers
{
    /// <summary>Monaco: <c>createTextBuffer(val, normalizeEOL)</c>; the default EOL is LF.</summary>
    public static PieceTreeTextBuffer Create(string[] chunks, bool normalizeEol = true)
    {
        var builder = new PieceTreeTextBufferBuilder();
        foreach (string chunk in chunks)
        {
            builder.AcceptChunk(chunk);
        }

        return builder.Finish(normalizeEol).Create(DefaultEndOfLine.LF);
    }

    /// <summary>Monaco: <c>createTextBufferFactory(text)</c>: one chunk, then <c>finish()</c>.</summary>
    public static PieceTreeTextBufferFactory CreateFactory(string text)
    {
        var builder = new PieceTreeTextBufferBuilder();
        builder.AcceptChunk(text);
        return builder.Finish();
    }
}
