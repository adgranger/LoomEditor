// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorWordOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>A word found on a line (Monaco: <c>IFindWordResult</c>).</summary>
/// <param name="Start">The index where the word starts.</param>
/// <param name="End">The index where the word ends.</param>
/// <param name="WordType">The word type.</param>
/// <param name="NextCharClass">The reason the word ended.</param>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal readonly record struct FindWordResult(int Start, int End, WordType WordType, WordCharacterClass NextCharClass);
