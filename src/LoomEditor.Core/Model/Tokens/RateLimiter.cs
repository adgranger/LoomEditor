// Ported from microsoft/vscode: src/vs/editor/common/tokens/common.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics;

namespace LoomEditor.Core.Model.Tokens;

/// <summary>Runs a callback at most <c>timesPerSecond</c> times per second (Monaco: <c>RateLimiter</c>).</summary>
/// <remarks>Threading: not thread-safe. Time is measured with <see cref="Stopwatch"/>.</remarks>
internal sealed class RateLimiter
{
    private readonly double _minimumMillisecondsBetweenRuns;
    private long? _lastRun;

    /// <summary>Creates a limiter allowing <paramref name="timesPerSecond"/> runs per second.</summary>
    public RateLimiter(int timesPerSecond = 5)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timesPerSecond, 0);
        TimesPerSecond = timesPerSecond;
        _minimumMillisecondsBetweenRuns = 1000.0 / timesPerSecond;
    }

    /// <summary><c>timesPerSecond</c>.</summary>
    public int TimesPerSecond { get; }

    /// <summary><c>runIfNotLimited</c>: the first call always runs.</summary>
    public void RunIfNotLimited(Action callback)
    {
        long now = Stopwatch.GetTimestamp();
        if (_lastRun is not long last || Stopwatch.GetElapsedTime(last, now).TotalMilliseconds >= _minimumMillisecondsBetweenRuns)
        {
            _lastRun = now;
            callback();
        }
    }
}
