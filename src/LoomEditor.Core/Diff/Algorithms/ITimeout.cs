// Ported from microsoft/vscode: src/vs/editor/common/diff/defaultLinesDiffComputer/algorithms/diffAlgorithm.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Diff.Algorithms;

/// <summary>Tells a diff algorithm whether it may keep going (Monaco: <c>ITimeout</c>).</summary>
/// <remarks>Threading: see the implementations.</remarks>
internal interface ITimeout
{
    /// <summary><c>isValid</c>: false once the time is up.</summary>
    bool IsValid();
}

/// <summary>A timeout that never expires (Monaco: <c>InfiniteTimeout</c>).</summary>
/// <remarks>Threading: stateless.</remarks>
internal sealed class InfiniteTimeout : ITimeout
{
    /// <summary><c>instance</c>.</summary>
    public static readonly InfiniteTimeout Instance = new();

    private InfiniteTimeout()
    {
    }

    /// <inheritdoc />
    public bool IsValid() => true;
}

/// <summary>A timeout that expires after a delay, and then stays expired (Monaco: <c>DateTimeout</c>).</summary>
/// <remarks>Threading: DateTimeout is safe to call from the thread that runs the diff. No <c>disable</c> (a debugging aid in Monaco).</remarks>
internal sealed class DateTimeout : ITimeout
{
    private readonly long _startTime = Environment.TickCount64;
    private readonly int _timeoutMs;
    private bool _valid = true;

    /// <summary>Starts the clock now.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeoutMs"/> is not positive.</exception>
    public DateTimeout(int timeoutMs)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeoutMs);
        _timeoutMs = timeoutMs;
    }

    /// <inheritdoc />
    public bool IsValid()
    {
        if (Environment.TickCount64 - _startTime >= _timeoutMs)
        {
            _valid = false;
        }

        return _valid;
    }
}
