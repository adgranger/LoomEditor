namespace LoomEditor.View.Comments;

/// <summary>The initials and the palette color of an author's avatar.</summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class AvatarInitials
{
    /// <summary>The upper-cased first letter of the first two words of <paramref name="name"/>, or "?" when blank.</summary>
    public static string Get(string name)
    {
        string[] words = name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return "?";
        }

        string result = First(words[0]);
        return words.Length > 1 ? result + First(words[1]) : result;
    }

    /// <summary>A hash of <paramref name="name"/> that is stable across processes, in <c>[0, paletteSize)</c>.</summary>
    public static int PaletteIndex(string name, int paletteSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(paletteSize, 1);
        int hash = 0;
        int power = 1;
        unchecked
        {
            foreach (char c in name)
            {
                hash += c * power;
                power *= 31;
            }
        }

        return (int)((uint)hash % (uint)paletteSize);
    }

    private static string First(string word)
    {
        int length = char.IsHighSurrogate(word[0]) && word.Length > 1 ? 2 : 1;
        return word[..length].ToUpperInvariant();
    }
}
