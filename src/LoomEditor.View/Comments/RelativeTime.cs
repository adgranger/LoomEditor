using System.Globalization;

namespace LoomEditor.View.Comments;

/// <summary>Formats a comment's timestamp relative to now ("2 hours ago").</summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class RelativeTime
{
    /// <summary>Formats <paramref name="when"/> relative to <paramref name="now"/>, in whole truncated units.</summary>
    public static string Format(DateTimeOffset when, DateTimeOffset now)
    {
        TimeSpan age = now - when;
        if (age < TimeSpan.FromMinutes(1))
        {
            return "just now";
        }

        if (age < TimeSpan.FromHours(1))
        {
            return Plural((int)age.TotalMinutes, "minute");
        }

        if (age < TimeSpan.FromDays(1))
        {
            return Plural((int)age.TotalHours, "hour");
        }

        if (when.Date.AddDays(1) == now.ToOffset(when.Offset).Date)
        {
            return "yesterday";
        }

        return age < TimeSpan.FromDays(30)
            ? Plural((int)age.TotalDays, "day")
            : when.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string Plural(int n, string unit) =>
        string.Create(CultureInfo.InvariantCulture, $"{n} {unit}{(n == 1 ? string.Empty : "s")} ago");
}
