using LoomEditor.View.Comments;

namespace LoomEditor.View.Tests.Comments;

public class RelativeTimeTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.FromHours(1));

    private static string Ago(TimeSpan span) => RelativeTime.Format(Now - span, Now);

    [Fact]
    public void Format_UnderMinute_JustNow() => Assert.Equal("just now", Ago(TimeSpan.FromSeconds(59)));

    [Fact]
    public void Format_Future_JustNow() => Assert.Equal("just now", Ago(TimeSpan.FromHours(-2)));

    [Fact]
    public void Format_OneMinute_Singular() => Assert.Equal("1 minute ago", Ago(TimeSpan.FromSeconds(119)));

    [Fact]
    public void Format_Minutes() => Assert.Equal("5 minutes ago", Ago(TimeSpan.FromMinutes(5)));

    [Fact]
    public void Format_OneHour_Singular() => Assert.Equal("1 hour ago", Ago(TimeSpan.FromMinutes(61)));

    [Fact]
    public void Format_Hours() => Assert.Equal("23 hours ago", Ago(TimeSpan.FromHours(23.5)));

    [Fact]
    public void Format_PreviousDay_Yesterday() => Assert.Equal("yesterday", Ago(TimeSpan.FromHours(25)));

    [Fact]
    public void Format_YesterdayAcrossMidnight() =>
        Assert.Equal(
            "yesterday",
            RelativeTime.Format(new DateTimeOffset(2026, 10, 28, 0, 5, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 29, 23, 10, 0, TimeSpan.Zero)));

    [Fact]
    public void Format_Days() => Assert.Equal("3 days ago", Ago(TimeSpan.FromDays(3)));

    [Fact]
    public void Format_OverThirtyDays_IsoDate() => Assert.Equal("2026-09-19", Ago(TimeSpan.FromDays(40)));
}
