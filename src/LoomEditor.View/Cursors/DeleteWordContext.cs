// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorWordOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>What the word delete operations read (Monaco: <c>DeleteWordContext</c>).</summary>
/// <remarks>
/// <para>Phase 4–5 cut point: Monaco's <c>autoClosingPairs</c> (language configuration) is not ported.</para>
/// <para>Threading: immutable; thread-safe.</para>
/// </remarks>
internal sealed record DeleteWordContext(
    WordCharacterClassifier WordSeparators,
    TextModel Model,
    Selection Selection,
    bool WhitespaceHeuristics,
    EditorAutoClosingEditStrategy AutoClosingDelete,
    EditorAutoClosingStrategy AutoClosingBrackets,
    EditorAutoClosingStrategy AutoClosingQuotes,
    IReadOnlyList<Range> AutoClosedCharacters);
