// Ported from microsoft/vscode: src/vs/editor/common/model/textModelTokens.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Model.Tokens;

/// <summary>
/// A <see cref="TokenizationStateStore{TState}"/> that also tracks which lines have an invalid end state (Monaco:
/// <c>TrackingTokenizationStateStore</c>). <b>Invariant:</b> if the model is retokenized from line 1 to
/// <see cref="GetFirstInvalidEndStateLineNumber"/> - 1, the recomputed end state of line l equals
/// <see cref="GetEndState"/>(l). Line ranges are <see cref="OffsetRange"/>s of line numbers (Monaco's <c>LineRange</c>).
/// </summary>
/// <typeparam name="TState">The tokenizer state.</typeparam>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class TrackingTokenizationStateStore<TState>
    where TState : class, IState
{
    private readonly TokenizationStateStore<TState> _tokenizationStateStore = new();
    private readonly RangePriorityQueue _invalidEndStatesLineNumbers = new();
    private int _lineCount;

    /// <summary>Creates a store where every line of a <paramref name="lineCount"/>-line model is invalid.</summary>
    public TrackingTokenizationStateStore(int lineCount)
    {
        _lineCount = lineCount;
        _invalidEndStatesLineNumbers.AddRange(new OffsetRange(1, lineCount + 1));
    }

    /// <summary><c>getEndState</c>.</summary>
    public TState? GetEndState(int lineNumber) => _tokenizationStateStore.GetEndState(lineNumber);

    /// <summary><c>setEndState</c>: returns whether the end state has changed.</summary>
    public bool SetEndState(int lineNumber, TState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        _invalidEndStatesLineNumbers.Delete(lineNumber);
        bool r = _tokenizationStateStore.SetEndState(lineNumber, state);
        if (r && lineNumber < _lineCount)
        {
            // because the state changed, we cannot trust the next state anymore and have to invalidate it.
            _invalidEndStatesLineNumbers.AddRange(new OffsetRange(lineNumber + 1, lineNumber + 2));
        }

        return r;
    }

    /// <summary><c>acceptChange</c>: <paramref name="range"/> (line numbers, end exclusive) is replaced by <paramref name="newLineCount"/> lines.</summary>
    public void AcceptChange(OffsetRange range, int newLineCount)
    {
        _lineCount += newLineCount - range.Length;
        _tokenizationStateStore.AcceptChange(range, newLineCount);
        _invalidEndStatesLineNumbers.AddRangeAndResize(new OffsetRange(range.Start, range.EndExclusive), newLineCount);
    }

    /// <summary><c>acceptChanges</c>.</summary>
    public void AcceptChanges(IReadOnlyList<ModelContentChange> changes)
    {
        foreach (ModelContentChange c in changes)
        {
            AcceptChange(new OffsetRange(c.Range.StartLineNumber, c.Range.EndLineNumber + 1), EolCounter.Count(c.Text).Count + 1);
        }
    }

    /// <summary><c>invalidateEndStateRange</c>.</summary>
    public void InvalidateEndStateRange(OffsetRange range) =>
        _invalidEndStatesLineNumbers.AddRange(new OffsetRange(range.Start, range.EndExclusive));

    /// <summary><c>getFirstInvalidEndStateLineNumber</c>.</summary>
    public int? GetFirstInvalidEndStateLineNumber() => _invalidEndStatesLineNumbers.Min;

    /// <summary><c>getFirstInvalidEndStateLineNumberOrMax</c>: <see cref="int.MaxValue"/> when everything is valid.</summary>
    public int GetFirstInvalidEndStateLineNumberOrMax() => GetFirstInvalidEndStateLineNumber() is int n and not 0 ? n : int.MaxValue;

    /// <summary><c>allStatesValid</c>.</summary>
    public bool AllStatesValid() => _invalidEndStatesLineNumbers.Min is null;

    /// <summary><c>getStartState</c>.</summary>
    public TState? GetStartState(int lineNumber, TState initialState) =>
        lineNumber == 1 ? initialState : GetEndState(lineNumber - 1);

    /// <summary><c>getFirstInvalidLine</c>: <c>null</c> when everything is valid.</summary>
    public (int LineNumber, TState StartState)? GetFirstInvalidLine(TState initialState)
    {
        if (GetFirstInvalidEndStateLineNumber() is not int lineNumber)
        {
            return null;
        }

        TState startState = GetStartState(lineNumber, initialState)
            ?? throw new InvalidOperationException("Start state must be defined");
        return (lineNumber, startState);
    }
}
