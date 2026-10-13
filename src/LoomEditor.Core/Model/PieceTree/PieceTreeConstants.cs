// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.PieceTree;

/// <summary>Constants shared by the piece tree classes.</summary>
/// <remarks>Threading: constants only; thread-safe.</remarks>
internal static class PieceTreeConstants
{
    /// <summary>Monaco: <c>AverageBufferSize</c>. Preferred size of the chunks the original content is split into.</summary>
    public const int AverageBufferSize = 65535;
}
