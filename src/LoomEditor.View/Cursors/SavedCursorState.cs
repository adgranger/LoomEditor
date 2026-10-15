// Ported from microsoft/vscode: src/vs/editor/common/editorCommon.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors;

/// <summary>A line and a column that may be missing, as a saved cursor state can come from outside (Monaco: <c>IPosition</c> with optional values).</summary>
/// <remarks>Threading: immutable value; thread-safe.</remarks>
internal readonly record struct SavedCursorPosition(int? LineNumber, int? Column);

/// <summary>The state of one cursor, as saved by <c>SaveState</c> and read by <c>RestoreState</c> (Monaco: <c>ICursorState</c>).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed record SavedCursorState(bool InSelectionMode, SavedCursorPosition? SelectionStart, SavedCursorPosition? Position);
