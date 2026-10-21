namespace LoomEditor.View.Features.Comment;

/// <summary>The JavaScript string searches the comment commands rely on, which differ from .NET's at the edges.</summary>
/// <remarks>Threading: pure functions; thread-safe.</remarks>
internal static class JsStrings
{
    /// <summary>JavaScript's <c>lastIndexOf(search, position)</c>: the last match that starts at or before <paramref name="position"/> (negative counts as 0).</summary>
    public static int LastIndexOf(string text, string search, int position)
    {
        int lastStart = Math.Min(Math.Max(position, 0), text.Length - search.Length);
        return lastStart < 0 ? -1 : text.LastIndexOf(search, lastStart + search.Length - 1, StringComparison.Ordinal);
    }

    /// <summary>JavaScript's <c>indexOf(search, fromIndex)</c>: a negative <paramref name="fromIndex"/> counts as 0, one past the end finds nothing.</summary>
    public static int IndexOf(string text, string search, int fromIndex)
    {
        int from = Math.Max(fromIndex, 0);
        return from > text.Length ? -1 : text.IndexOf(search, from, StringComparison.Ordinal);
    }

    /// <summary>JavaScript's <c>substring(start, end)</c>: the arguments are clamped, and swapped when start is after end.</summary>
    public static string Substring(string text, int start, int end)
    {
        int from = Math.Clamp(Math.Min(start, end), 0, text.Length);
        int to = Math.Clamp(Math.Max(start, end), 0, text.Length);
        return text.Substring(from, to - from);
    }

    /// <summary>JavaScript's <c>charCodeAt(index) === code</c>: false out of range.</summary>
    public static bool CharAtEquals(string text, int index, char code) => index >= 0 && index < text.Length && text[index] == code;
}
