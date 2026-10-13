// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

/// <summary>
/// The text of a document as a red-black tree of <see cref="Piece"/>s pointing into append-only buffers.
/// This part: construction and metadata. Reading lines is in <c>PieceTreeBase.Lines.cs</c>.
/// </summary>
/// <remarks>
/// <para>Monaco: <c>PieceTreeBase</c>.</para>
/// <para>
/// Differences from Monaco: the tree is owned through a <see cref="RedBlackTree"/> (with its own sentinel), the EOL
/// is validated to be <c>"\n"</c> or <c>"\r\n"</c>, and string buffers are <see cref="StringBuffer"/>s.
/// </para>
/// <para>Threading: not thread-safe, even for reads (reading lines updates caches); callers must serialize access.</para>
/// </remarks>
internal sealed partial class PieceTreeBase
{
    private readonly RedBlackTree _tree = new();

    // 0 is the change buffer, the others are the read-only original buffers.
    private List<StringBuffer> _buffers = [];
    private int _lineCnt;
    private int _length;
    private string _eol = "\n";
    private int _eolLength;
    private bool _eolNormalized;
    private BufferCursor _lastChangeBufferPos;
    private PieceTreeSearchCache _searchCache = new(1);
    private (int LineNumber, string Value) _lastVisitedLine;

    /// <summary>Builds a tree with one piece per non-empty chunk.</summary>
    /// <param name="chunks">The original content, in order. Not copied.</param>
    /// <param name="eol">The document's EOL: <c>"\n"</c> or <c>"\r\n"</c>.</param>
    /// <param name="eolNormalized">True when every line break in <paramref name="chunks"/> equals <paramref name="eol"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="eol"/> is not <c>"\n"</c> or <c>"\r\n"</c>.</exception>
    public PieceTreeBase(IReadOnlyList<StringBuffer> chunks, string eol, bool eolNormalized)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        if (eol is not ("\n" or "\r\n"))
        {
            throw new ArgumentException("The EOL must be \"\\n\" or \"\\r\\n\".", nameof(eol));
        }

        Create(chunks, eol, eolNormalized);
    }

    /// <summary>The underlying tree, exposed for tests.</summary>
    internal RedBlackTree Tree => _tree;

    private void Create(IReadOnlyList<StringBuffer> chunks, string eol, bool eolNormalized)
    {
        _buffers = [new StringBuffer("", [0])];
        _lastChangeBufferPos = new BufferCursor(0, 0);
        _tree.Root = _tree.Sentinel;
        _lineCnt = 1;
        _length = 0;
        _eol = eol;
        _eolLength = eol.Length;
        _eolNormalized = eolNormalized;

        TreeNode? lastNode = null;
        for (int i = 0, len = chunks.Count; i < len; i++)
        {
            StringBuffer chunk = chunks[i];
            if (chunk.Length > 0)
            {
                List<int> lineStarts = chunk.LineStarts;
                var piece = new Piece(
                    i + 1,
                    new BufferCursor(0, 0),
                    new BufferCursor(lineStarts.Count - 1, chunk.Length - lineStarts[^1]),
                    lineStarts.Count - 1,
                    chunk.Length);
                _buffers.Add(chunk);
                lastNode = _tree.InsertRight(lastNode, piece);
            }
        }

        _searchCache = new PieceTreeSearchCache(1);
        _lastVisitedLine = (0, "");
        ComputeBufferMetadata();
    }

    /// <summary>The EOL of the document (Monaco: <c>getEOL</c>).</summary>
    public string GetEol() => _eol;

    /// <summary>Total number of characters (Monaco: <c>getLength</c>).</summary>
    public int GetLength() => _length;

    /// <summary>Number of lines; an empty document has one (Monaco: <c>getLineCount</c>).</summary>
    public int GetLineCount() => _lineCnt;

    private void ComputeBufferMetadata()
    {
        TreeNode x = _tree.Root;

        int lfCnt = 1;
        int len = 0;

        while (!ReferenceEquals(x, _tree.Sentinel))
        {
            lfCnt += x.LineFeedCountLeft + x.Piece.LineFeedCount;
            len += x.SizeLeft + x.Piece.Length;
            x = x.Right;
        }

        _lineCnt = lfCnt;
        _length = len;
        _searchCache.Validate(_length);
    }
}
