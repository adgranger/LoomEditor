// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.Edits;

/// <summary>An edit whose range was validated against the model, before it becomes a <see cref="ValidatedEditOperation"/>.</summary>
/// <remarks>
/// <para>Monaco: <c>ValidAnnotatedEditOperation</c>.</para>
/// <para>Threading: immutable.</para>
/// </remarks>
/// <param name="Identifier">An identifier associated with this edit, or <c>null</c>.</param>
/// <param name="Range">The range to replace; may be empty to insert.</param>
/// <param name="Text">The replacement text; <c>null</c> or empty to delete.</param>
/// <param name="ForceMoveMarkers">True when markers at the edit position must move.</param>
/// <param name="IsAutoWhitespaceEdit">True when the edit was produced by auto-whitespace.</param>
/// <param name="IsTracked">Monaco <c>_isTracked</c>.</param>
internal sealed record ValidAnnotatedEditOperation(
    EditOperationIdentifier? Identifier,
    Range Range,
    string? Text,
    bool ForceMoveMarkers,
    bool IsAutoWhitespaceEdit,
    bool IsTracked);
