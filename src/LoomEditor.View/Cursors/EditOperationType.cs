// Ported from microsoft/vscode: src/vs/editor/common/cursorCommon.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Cursors;

/// <summary>
/// An operation type that is recorded for undo/redo purposes. The goal is to introduce an undo stop when the
/// controller switches between different operation types (Monaco: <c>EditOperationType</c>).
/// </summary>
internal enum EditOperationType
{
    Other = 0,
    DeletingLeft = 2,
    DeletingRight = 3,
    TypingOther = 4,
    TypingFirstSpace = 5,
    TypingConsecutiveSpace = 6,
}
