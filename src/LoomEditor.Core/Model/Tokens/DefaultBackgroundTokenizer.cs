// Ported from microsoft/vscode: src/vs/editor/common/model/textModelTokens.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;
using LoomEditor.Core.Tokens;

namespace LoomEditor.Core.Model.Tokens;

/// <summary>
/// Tokenizes the invalid lines of a model in idle-time slices (Monaco: <c>DefaultBackgroundTokenizer</c>). The editor
/// control does not use it (it has a background thread, ADR 0008); it is the fallback of <c>TextModel</c>.
/// </summary>
/// <remarks>
/// Threading: not thread-safe; runs on the thread of its <see cref="ITokenizationScheduler"/>. Differences from
/// Monaco: time is measured with <see cref="Stopwatch"/>, and <see cref="Dispose"/> also cancels the pending idle
/// callback.
/// </remarks>
internal sealed class DefaultBackgroundTokenizer : IBackgroundTokenizer
{
    private readonly TokenizerWithStateStoreAndTextModel _tokenizerWithStateStore;
    private readonly IBackgroundTokenizationStore _backgroundTokenStore;
    private readonly ITokenizationScheduler _scheduler;
    private IDisposable? _pendingIdle;
    private bool _isDisposed;
    private bool _isScheduled;

    /// <summary>Creates a background tokenizer; call <see cref="HandleChanges"/> to start it.</summary>
    public DefaultBackgroundTokenizer(
        TokenizerWithStateStoreAndTextModel tokenizerWithStateStore,
        IBackgroundTokenizationStore backgroundTokenStore,
        ITokenizationScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(tokenizerWithStateStore);
        ArgumentNullException.ThrowIfNull(backgroundTokenStore);
        ArgumentNullException.ThrowIfNull(scheduler);
        _tokenizerWithStateStore = tokenizerWithStateStore;
        _backgroundTokenStore = backgroundTokenStore;
        _scheduler = scheduler;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _isDisposed = true;
        _pendingIdle?.Dispose();
        _pendingIdle = null;
    }

    /// <summary><c>handleChanges</c>.</summary>
    public void HandleChanges() => BeginBackgroundTokenization();

    private void BeginBackgroundTokenization()
    {
        if (_isScheduled || !_tokenizerWithStateStore.ModelInfo.IsAttachedToEditor || !HasLinesToTokenize())
        {
            return;
        }

        _isScheduled = true;
        _pendingIdle = _scheduler.RunWhenIdle(deadline =>
        {
            _isScheduled = false;
            _pendingIdle = null;

            BackgroundTokenizeWithDeadline(deadline);
        });
    }

    /// <summary>Tokenize until the deadline occurs, but try to yield every 1-2ms.</summary>
    private void BackgroundTokenizeWithDeadline(IdleDeadline deadline)
    {
        // Read the time remaining from the `deadline` immediately because it is unclear
        // if the `deadline` object will be valid after execution leaves this function.
        long start = Stopwatch.GetTimestamp();
        double remaining = deadline.TimeRemaining();

        void Execute()
        {
            if (_isDisposed || !_tokenizerWithStateStore.ModelInfo.IsAttachedToEditor || !HasLinesToTokenize())
            {
                // disposed in the meantime or detached or finished
                return;
            }

            BackgroundTokenizeForAtLeast1ms();

            if (Stopwatch.GetElapsedTime(start).TotalMilliseconds < remaining)
            {
                // There is still time before reaching the deadline, so yield and then continue execution
                _scheduler.Post(Execute);
            }
            else
            {
                // The deadline has been reached, so schedule a new idle callback if necessary
                BeginBackgroundTokenization();
            }
        }

        Execute();
    }

    /// <summary>Tokenize for at least 1ms.</summary>
    private void BackgroundTokenizeForAtLeast1ms()
    {
        int lineCount = _tokenizerWithStateStore.Model.LineCount;
        var builder = new ContiguousMultilineTokensBuilder();
        long start = Stopwatch.GetTimestamp();

        do
        {
            if (Stopwatch.GetElapsedTime(start).TotalMilliseconds > 1)
            {
                // the comparison is intentionally > 1 and not >= 1 to ensure that a full millisecond has elapsed
                break;
            }

            int tokenizedLineNumber = TokenizeOneInvalidLine(builder);

            if (tokenizedLineNumber >= lineCount)
            {
                break;
            }
        }
        while (HasLinesToTokenize());

        _backgroundTokenStore.SetTokens(builder.Finalize());
        CheckFinished();
    }

    private bool HasLinesToTokenize() => !_tokenizerWithStateStore.Store.AllStatesValid();

    private int TokenizeOneInvalidLine(ContiguousMultilineTokensBuilder builder)
    {
        (int LineNumber, IState StartState)? firstInvalidLine = _tokenizerWithStateStore.GetFirstInvalidLine();
        if (firstInvalidLine is null)
        {
            return _tokenizerWithStateStore.Model.LineCount + 1;
        }

        _tokenizerWithStateStore.UpdateTokensUntilLine(builder, firstInvalidLine.Value.LineNumber);
        return firstInvalidLine.Value.LineNumber;
    }

    /// <summary><c>checkFinished</c>.</summary>
    public void CheckFinished()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_tokenizerWithStateStore.Store.AllStatesValid())
        {
            _backgroundTokenStore.BackgroundTokenizationFinished();
        }
    }

    /// <inheritdoc />
    public void RequestTokens(int startLineNumber, int endLineNumberExclusive) =>
        _tokenizerWithStateStore.Store.InvalidateEndStateRange(new Text.OffsetRange(startLineNumber, endLineNumberExclusive));
}
