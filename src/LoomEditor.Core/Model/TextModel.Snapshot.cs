// Ported from microsoft/vscode: src/vs/editor/common/model/textModel.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.PieceTree;

namespace LoomEditor.Core.Model;

public sealed partial class TextModel
{
    // Chunks smaller than this are merged by TextModelSnapshot (Monaco: 64 * 1024)
    private const int SnapshotChunkLength = 64 * 1024;

    /// <summary>Creates a snapshot of the whole text, to read it chunk by chunk (Monaco: <c>createSnapshot</c>).</summary>
    /// <param name="preserveBom">True to prefix the first chunk with the BOM the model had when it was created.</param>
    /// <returns>A snapshot that is not affected by later edits of the model.</returns>
    public ITextSnapshot CreateSnapshot(bool preserveBom = false) => new TextModelSnapshot(_buffer.CreateSnapshot(preserveBom));

    /// <summary>Creates a model holding the text of a snapshot (Monaco: <c>createTextBufferFactoryFromSnapshot</c>).</summary>
    /// <param name="snapshot">The snapshot to read until it returns <see langword="null"/>.</param>
    /// <param name="defaultEol">The EOL to use when the text has no line break.</param>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is <see langword="null"/>.</exception>
    public static TextModel FromSnapshot(ITextSnapshot snapshot, DefaultEndOfLine defaultEol = DefaultEndOfLine.LF)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var builder = new PieceTreeTextBufferBuilder();
        while (snapshot.Read() is { } chunk)
        {
            builder.AcceptChunk(chunk);
        }

        return new TextModel(builder.Finish(normalizeEol: true).Create(defaultEol), defaultEol);
    }

    // Monaco: TextModelSnapshot. Merges the small chunks of the source into chunks of at least 64K characters.
    private sealed class TextModelSnapshot : ITextSnapshot
    {
        private readonly ITextSnapshot _source;
        private bool _endOfStream;

        public TextModelSnapshot(ITextSnapshot source)
        {
            _source = source;
        }

        public string? Read()
        {
            if (_endOfStream)
            {
                return null;
            }

            var result = new List<string>();
            int resultLength = 0;

            while (true)
            {
                string? chunk = _source.Read();
                if (chunk is null)
                {
                    _endOfStream = true;
                    return result.Count == 0 ? null : string.Concat(result);
                }

                if (chunk.Length > 0)
                {
                    result.Add(chunk);
                    resultLength += chunk.Length;
                }

                if (resultLength >= SnapshotChunkLength)
                {
                    return string.Concat(result);
                }
            }
        }
    }
}
