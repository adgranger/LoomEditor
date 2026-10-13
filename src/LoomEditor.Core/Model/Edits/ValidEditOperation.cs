// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Undo;
using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Model.Edits;

/// <summary>The reverse of an applied edit, used for undo.</summary>
/// <remarks>
/// <para>Monaco: <c>IValidEditOperation</c>.</para>
/// <para>Threading: immutable.</para>
/// </remarks>
/// <param name="Identifier">An identifier associated with this edit, or <c>null</c>.</param>
/// <param name="Range">The range to replace; may be empty to emulate an insert.</param>
/// <param name="Text">The replacement text; empty to emulate a delete.</param>
/// <param name="TextChange">The offset-based form of the edit that was applied (what the undo stack stores).</param>
internal sealed record ValidEditOperation(EditOperationIdentifier? Identifier, Range Range, string Text, TextChange TextChange);
