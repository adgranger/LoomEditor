// Ported from microsoft/vscode: .../textMate/browser/backgroundTokenization/worker/textMateTokenizationWorker.worker.ts (StateDeltas), textMateWorkerTokenizer.ts (StateDeltaBuilder)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Grammar;

namespace LoomEditor.TextMate.Tokenization.Worker;

/// <summary>
/// The end states of consecutive lines, from <paramref name="StartLineNumber"/> (Monaco: <c>StateDeltas</c>). Monaco
/// sends <c>StackDiff</c>s because it serializes; here the states themselves cross threads (ADR 0008, decision 5).
/// </summary>
/// <param name="StartLineNumber">The line of the first state.</param>
/// <param name="States">One entry per line: the end state, or <see langword="null"/> when it did not change (<c>null</c> in Monaco too).</param>
/// <remarks>Threading: immutable once sent; the states are shared read-only (a <see cref="StateStack"/> never changes).</remarks>
internal sealed record StateDeltas(int StartLineNumber, IReadOnlyList<StateStack?> States);

/// <summary>Groups the end states of the lines of a batch into runs of consecutive lines (Monaco: <c>StateDeltaBuilder</c>).</summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class StateDeltaBuilder
{
    private readonly List<(int StartLineNumber, List<StateStack?> States)> _runs = [];
    private int _lastStartLineNumber = -1;

    /// <summary><c>setState</c>: <paramref name="state"/> is <see langword="null"/> when the end state of the line did not change.</summary>
    public void SetState(int lineNumber, StateStack? state)
    {
        if (lineNumber == _lastStartLineNumber + 1)
        {
            _runs[^1].States.Add(state);
        }
        else
        {
            _runs.Add((lineNumber, [state]));
        }

        _lastStartLineNumber = lineNumber;
    }

    /// <summary><c>getStateDeltas</c>.</summary>
    public List<StateDeltas> GetStateDeltas() => [.. _runs.Select(r => new StateDeltas(r.StartLineNumber, r.States))];
}
