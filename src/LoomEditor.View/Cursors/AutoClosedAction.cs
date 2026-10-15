// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursor.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>The characters the editor closed automatically (Monaco: <c>AutoClosedAction</c>).</summary>
/// <remarks>
/// Phase 4–5 cut point: auto-closed characters (cursor.ts 682–748). They need model decorations, which phase 3 does
/// not have, so nothing creates an action and <see cref="IsValid"/> is always <see langword="false"/>.
/// Threading: UI thread only.
/// </remarks>
internal sealed class AutoClosedAction
{
    public static bool IsValid(IReadOnlyList<Selection> selections) => false;
}
