// Ported from microsoft/vscode: src/vs/editor/common/cursorCommon.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors;

/// <summary>The state of a column (box) selection (Monaco: <c>IColumnSelectData</c>).</summary>
/// <remarks>Threading: immutable value; thread-safe.</remarks>
internal readonly record struct ColumnSelectData(
    bool IsReal,
    int FromViewLineNumber,
    int FromViewVisualColumn,
    int ToViewLineNumber,
    int ToViewVisualColumn);
