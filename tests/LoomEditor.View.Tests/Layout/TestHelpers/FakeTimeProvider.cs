namespace LoomEditor.View.Tests.Layout.TestHelpers;

/// <summary>A clock that only moves when the test says so.</summary>
/// <remarks>Threading: not thread-safe; tests only.</remarks>
internal sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(double milliseconds) => _now = _now.AddMilliseconds(milliseconds);
}
