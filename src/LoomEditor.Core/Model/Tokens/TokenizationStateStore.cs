// Ported from microsoft/vscode: src/vs/editor/common/model/textModelTokens.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Model.Tokens;

/// <summary>The end state of every line, shifted when lines are inserted or deleted (Monaco: <c>TokenizationStateStore</c>).</summary>
/// <typeparam name="TState">The tokenizer state; "no state" is <c>null</c>.</typeparam>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class TokenizationStateStore<TState>
    where TState : class, IState
{
    private readonly FixedArray<TState?> _lineEndStates = new(null);

    /// <summary><c>getEndState</c>.</summary>
    public TState? GetEndState(int lineNumber) => _lineEndStates.Get(lineNumber);

    /// <summary><c>setEndState</c>: returns whether the state changed.</summary>
    public bool SetEndState(int lineNumber, TState state)
    {
        TState? oldState = _lineEndStates.Get(lineNumber);
        if (oldState is not null && oldState.Equals(state))
        {
            return false;
        }

        _lineEndStates.Set(lineNumber, state);
        return true;
    }

    /// <summary>
    /// <c>acceptChange</c>: <paramref name="range"/> (line numbers, end exclusive; Monaco's <c>LineRange</c>) is replaced
    /// by <paramref name="newLineCount"/> lines.
    /// </summary>
    public void AcceptChange(OffsetRange range, int newLineCount)
    {
        int length = range.Length;
        if (newLineCount > 0 && length > 0)
        {
            // Keep the last state, even though it is unrelated.
            // But if the new state happens to agree with this last state, then we know we can stop tokenizing.
            length--;
            newLineCount--;
        }

        _lineEndStates.Replace(range.Start, length, newLineCount);
    }

    /// <summary><c>acceptChanges</c>.</summary>
    public void AcceptChanges(IReadOnlyList<ModelContentChange> changes)
    {
        foreach (ModelContentChange c in changes)
        {
            AcceptChange(new OffsetRange(c.Range.StartLineNumber, c.Range.EndLineNumber + 1), EolCounter.Count(c.Text).Count + 1);
        }
    }
}
