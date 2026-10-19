// Ported from microsoft/vscode: src/vs/editor/common/model.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Tokens;

/// <summary>A view attached to a model, which reports its visible lines (Monaco: <c>IAttachedView</c>). Dispose to detach it.</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal interface IAttachedView : IDisposable
{
    /// <summary>
    /// <c>setVisibleLines</c>: <paramref name="visibleLines"/> are (first line, last line) pairs, both inclusive.
    /// <paramref name="stabilized"/> tells whether the visible lines are probably going to change soon (false on scroll) or
    /// can be considered stable (true on reveal range); tokenizers tokenize synchronously if it is true.
    /// </summary>
    void SetVisibleLines(IReadOnlyList<(int StartLineNumber, int EndLineNumber)> visibleLines, bool stabilized);
}
