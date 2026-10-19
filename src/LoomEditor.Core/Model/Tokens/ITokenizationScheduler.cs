namespace LoomEditor.Core.Model.Tokens;

/// <summary>Monaco's <c>IdleDeadline</c>: how long an idle callback may run.</summary>
/// <param name="timeRemaining">Returns the milliseconds left.</param>
/// <remarks>Threading: a value type.</remarks>
internal readonly struct IdleDeadline(Func<double> timeRemaining)
{
    /// <summary><c>timeRemaining</c>: milliseconds.</summary>
    public double TimeRemaining() => timeRemaining();
}

/// <summary>Replaces Monaco's <c>runWhenGlobalIdle</c> and <c>setTimeout0</c> (ADR 0008 decision 5).</summary>
/// <remarks>Threading: callbacks run on the thread that owns the model.</remarks>
internal interface ITokenizationScheduler
{
    /// <summary><c>runWhenGlobalIdle</c>: runs <paramref name="callback"/> when the host is idle; dispose to cancel.</summary>
    IDisposable RunWhenIdle(Action<IdleDeadline> callback);

    /// <summary><c>setTimeout0</c>: runs <paramref name="callback"/> as soon as the host can, after yielding.</summary>
    void Post(Action callback);
}
