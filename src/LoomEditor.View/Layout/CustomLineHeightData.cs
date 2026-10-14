// Ported from microsoft/vscode: src/vs/editor/common/viewLayout/lineHeights.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Layout;

/// <summary>
/// The custom line height of a decoration, over a range of view lines. Monaco: <c>CustomLineHeightData</c>.
/// </summary>
/// <remarks>
/// Behavior difference: <c>fromDecorations</c> is not ported (there are no decorations yet).
/// Threading: immutable; safe to share between threads.
/// </remarks>
/// <param name="DecorationId">The id of the decoration.</param>
/// <param name="StartLineNumber">First line (1-based) of the range.</param>
/// <param name="EndLineNumber">Last line (1-based, inclusive) of the range.</param>
/// <param name="LineHeight">The custom height of each line of the range.</param>
internal sealed record CustomLineHeightData(string DecorationId, int StartLineNumber, int EndLineNumber, int LineHeight);
