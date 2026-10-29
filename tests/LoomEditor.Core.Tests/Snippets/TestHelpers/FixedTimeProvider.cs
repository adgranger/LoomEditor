namespace LoomEditor.Core.Tests.Snippets.TestHelpers;

/// <summary>A clock that only moves when the test says so, in a time zone the test chooses.</summary>
/// <remarks>Threading: not thread-safe; tests only.</remarks>
internal sealed class FixedTimeProvider(DateTimeOffset now, TimeZoneInfo? timeZone = null) : TimeProvider
{
    private DateTimeOffset _now = now;

    public override TimeZoneInfo LocalTimeZone { get; } = timeZone ?? TimeZoneInfo.Local;

    public override DateTimeOffset GetUtcNow() => _now.ToUniversalTime();

    public void Advance(TimeSpan duration) => _now += duration;
}
