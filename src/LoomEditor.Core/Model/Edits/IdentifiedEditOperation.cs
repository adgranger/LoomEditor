// Ported from microsoft/vscode: src/vs/editor/common/model.ts (IIdentifiedSingleEditOperation)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.Edits;

/// <summary>An edit that knows which cursor and command produced it.</summary>
/// <remarks>
/// <para>Monaco: <c>IIdentifiedSingleEditOperation</c>. A <see langword="null"/> <paramref name="Text"/> deletes the range.</para>
/// <para>Threading: immutable value; thread-safe.</para>
/// </remarks>
/// <param name="Range">The range to replace.</param>
/// <param name="Text">The new text, or <see langword="null"/> to delete the range.</param>
/// <param name="Identifier">Which cursor and command produced the edit; copied to the inverse operation.</param>
/// <param name="ForceMoveMarkers">True when markers at the edit position must move.</param>
/// <param name="IsAutoWhitespaceEdit">True when the edit inserts whitespace automatically (it can be trimmed later).</param>
/// <param name="IsTracked">True when the edit must be tracked: it is never merged with the other edits.</param>
internal readonly record struct IdentifiedEditOperation(
    Range Range,
    string? Text,
    EditOperationIdentifier? Identifier = null,
    bool ForceMoveMarkers = false,
    bool IsAutoWhitespaceEdit = false,
    bool IsTracked = false);
