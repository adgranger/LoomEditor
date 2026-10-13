// Not ported from Monaco: LoomEditor helper. Temporary until P1-09 adds PieceTreeTextBufferBuilder.

using LoomEditor.Core.Model.PieceTree;

namespace LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

internal static class TestPieceTrees
{
    /// <summary>Builds a piece tree whose original buffers are <paramref name="chunks"/>, one piece per non-empty chunk.</summary>
    public static PieceTreeBase FromChunks(string eol, params string[] chunks) => FromChunks(eol, true, chunks);

    /// <summary>Same as above, saying whether every line break of the chunks equals <paramref name="eol"/>.</summary>
    public static PieceTreeBase FromChunks(string eol, bool eolNormalized, params string[] chunks)
    {
        var buffers = new List<StringBuffer>(chunks.Length);
        foreach (string chunk in chunks)
        {
            buffers.Add(new StringBuffer(chunk, LineStarts.CreateFast(chunk)));
        }

        return new PieceTreeBase(buffers, eol, eolNormalized);
    }
}
