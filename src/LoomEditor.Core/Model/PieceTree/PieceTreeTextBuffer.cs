// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

/// <summary>
/// The text of a model: a <see cref="PieceTreeBase"/> plus the BOM and flags about its content.
/// This part is a shell (fields, constructor, flags, BOM, EOL); queries and edits are added by later cards.
/// </summary>
/// <remarks>
/// <para>Monaco: <c>PieceTreeTextBuffer</c>.</para>
/// <para>
/// Differences from Monaco: not <c>IDisposable</c> and no <c>onDidChangeContent</c> event (nothing to dispose in
/// this port); the EOL is a <see cref="string"/> validated by <see cref="PieceTreeBase"/>.
/// </para>
/// <para>Threading: not thread-safe; callers must serialize access.</para>
/// </remarks>
internal sealed class PieceTreeTextBuffer
{
    private readonly PieceTreeBase _pieceTree;
    private readonly string _bom;
    private readonly bool _mightContainRtl;
    private readonly bool _mightContainUnusualLineTerminators;
    private readonly bool _mightContainNonBasicAscii;

    /// <summary>Creates a buffer over already-chunked text.</summary>
    /// <param name="chunks">The text, in order. Not copied.</param>
    /// <param name="bom">The BOM that was removed from the text, or an empty string.</param>
    /// <param name="eol">The EOL of the document: <c>"\n"</c> or <c>"\r\n"</c>.</param>
    /// <param name="containsRtl">True when the text may contain right-to-left characters.</param>
    /// <param name="containsUnusualLineTerminators">True when the text may contain U+2028 or U+2029.</param>
    /// <param name="isBasicAscii">True when the text has only basic ASCII characters.</param>
    /// <param name="eolNormalized">True when every line break of the chunks equals <paramref name="eol"/>.</param>
    public PieceTreeTextBuffer(
        IReadOnlyList<StringBuffer> chunks,
        string bom,
        string eol,
        bool containsRtl,
        bool containsUnusualLineTerminators,
        bool isBasicAscii,
        bool eolNormalized)
    {
        ArgumentNullException.ThrowIfNull(bom);
        _bom = bom;
        _mightContainNonBasicAscii = !isBasicAscii;
        _mightContainRtl = containsRtl;
        _mightContainUnusualLineTerminators = containsUnusualLineTerminators;
        _pieceTree = new PieceTreeBase(chunks, eol, eolNormalized);
    }

    /// <summary>True when the text may contain right-to-left characters (Monaco: <c>mightContainRTL</c>).</summary>
    public bool MightContainRtl() => _mightContainRtl;

    /// <summary>True when the text may contain U+2028 or U+2029 (Monaco: <c>mightContainUnusualLineTerminators</c>).</summary>
    public bool MightContainUnusualLineTerminators() => _mightContainUnusualLineTerminators;

    /// <summary>True when the text may contain non basic-ASCII characters (Monaco: <c>mightContainNonBasicASCII</c>).</summary>
    public bool MightContainNonBasicAscii() => _mightContainNonBasicAscii;

    /// <summary>The BOM removed from the text, or an empty string (Monaco: <c>getBOM</c>).</summary>
    public string GetBom() => _bom;

    /// <summary>The EOL of the document (Monaco: <c>getEOL</c>).</summary>
    public string GetEol() => _pieceTree.GetEol();

    /// <summary>The underlying piece tree, exposed for tests (Monaco: <c>getPieceTree</c>).</summary>
    internal PieceTreeBase GetPieceTree() => _pieceTree;
}
