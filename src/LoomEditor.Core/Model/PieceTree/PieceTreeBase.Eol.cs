// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using System.Text.RegularExpressions;

namespace LoomEditor.Core.Model.PieceTree;

// Changing the EOL of the whole document.
internal sealed partial class PieceTreeBase
{
    [GeneratedRegex(@"\r\n|\r|\n")]
    private static partial Regex LineBreakRegex();

    /// <summary>Rewrites every line break to <paramref name="eol"/> and rebuilds the tree (Monaco: <c>normalizeEOL</c>).</summary>
    /// <param name="eol">The new EOL: <c>"\n"</c> or <c>"\r\n"</c>.</param>
    /// <exception cref="ArgumentException"><paramref name="eol"/> is not <c>"\n"</c> or <c>"\r\n"</c>.</exception>
    public void NormalizeEol(string eol)
    {
        ValidateEol(eol);

        const int averageBufferSize = PieceTreeConstants.AverageBufferSize;
        const int min = averageBufferSize - averageBufferSize / 3;
        const int max = min * 2;

        var tempChunk = new StringBuilder();
        int tempChunkLen = 0;
        var chunks = new List<StringBuffer>();

        Iterate(_tree.Root, node =>
        {
            string str = GetNodeContent(node);
            int len = str.Length;
            if (tempChunkLen <= min || tempChunkLen + len < max)
            {
                tempChunk.Append(str);
                tempChunkLen += len;
                return true;
            }

            // flush anyways
            chunks.Add(CreateNormalizedChunk(tempChunk.ToString(), eol));
            tempChunk.Clear().Append(str);
            tempChunkLen = len;
            return true;
        });

        if (tempChunkLen > 0)
        {
            chunks.Add(CreateNormalizedChunk(tempChunk.ToString(), eol));
        }

        Create(chunks, eol, true);
    }

    /// <summary>Sets the EOL of the document and rewrites all line breaks (Monaco: <c>setEOL</c>).</summary>
    /// <param name="newEol">The new EOL: <c>"\n"</c> or <c>"\r\n"</c>.</param>
    /// <exception cref="ArgumentException"><paramref name="newEol"/> is not <c>"\n"</c> or <c>"\r\n"</c>.</exception>
    public void SetEol(string newEol)
    {
        ValidateEol(newEol);
        _eol = newEol;
        _eolLength = newEol.Length;
        NormalizeEol(newEol);
    }

    private static StringBuffer CreateNormalizedChunk(string text, string eol)
    {
        string normalized = LineBreakRegex().Replace(text, eol);
        return new StringBuffer(normalized, LineStarts.CreateFast(normalized));
    }

    private static void ValidateEol(string eol)
    {
        ArgumentNullException.ThrowIfNull(eol);
        if (eol is not ("\n" or "\r\n"))
        {
            throw new ArgumentException("The EOL must be \"\n\" or \"\r\n\".", nameof(eol));
        }
    }
}
