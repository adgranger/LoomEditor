using System.Text;

namespace LoomEditor.Gallery;

/// <summary>Deterministic sample documents to exercise the editor with large and unusual text.</summary>
/// <remarks>Threading: every method is pure, so it can run on a background thread.</remarks>
internal static class LargeDocuments
{
    private const int HundredMegabytes = 100 * 1024 * 1024;
    private const int OneMegabyte = 1024 * 1024;
    private const int Seed = 42;

    private static readonly string[] Identifiers =
    [
        "value", "index", "count", "result", "buffer", "offset", "length", "node", "range", "line", "column", "model",
        "editor", "token", "scope", "state", "if", "for", "return", "var", "new", "await", "static", "private",
    ];

    /// <summary>Tabs, CJK, emoji and combining marks.</summary>
    public static string Unicode { get; } = string.Join(
        '\n',
        "// Unicode sample",
        "Tab stops:\ta\tbb\tccc\tdddd\teeeee",
        "\tIndented with a tab, then\tanother tab",
        "ASCII  : 0123456789 abcdefghij",
        "CJK    : 你好，世界 こんにちは世界 안녕하세요",
        "Emoji  : 😀 🚀 👨‍👩‍👧‍👦 🇫🇷 ❤️",
        "Marks  : é ä ñ ộ (combining) vs é ä ñ ộ (precomposed)",
        "RTL    : שלום עולם مرحبا",
        "Wide   : ＡＢＣ１２３ (full-width) next to ABC123 (ASCII)");

    /// <summary>About 100 × 1024 × 1024 characters of code-like lines: 0–120 characters, ~40 on average.</summary>
    public static string Create100MB()
    {
        var random = new Random(Seed);
        // The slack covers the last line, so the builder never grows.
        var builder = new StringBuilder(HundredMegabytes + 256);
        while (builder.Length < HundredMegabytes)
        {
            AppendCodeLine(builder, random);
            builder.Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>A single line of 1 MB of words and numbers, without any line break.</summary>
    public static string CreateLongLine()
    {
        var random = new Random(Seed);
        var builder = new StringBuilder(OneMegabyte + 32);
        while (builder.Length < OneMegabyte)
        {
            builder.Append(Identifiers[random.Next(Identifiers.Length)]).Append(random.Next(1000)).Append(' ');
        }

        return builder.ToString();
    }

    private static void AppendCodeLine(StringBuilder builder, Random random)
    {
        // r² has a mean of 1/3: lengths 0–120 with an average of 40.
        double r = random.NextDouble();
        int length = (int)(r * r * 121);
        int start = builder.Length;

        builder.Append('\t', Math.Min(random.Next(4), length));
        if (builder.Length - start < length)
        {
            builder.Append(' ', Math.Min(random.Next(5), length - (builder.Length - start)));
        }

        while (builder.Length - start < length)
        {
            string word = random.Next(4) == 0 ? random.Next(100_000).ToString(System.Globalization.CultureInfo.InvariantCulture) : Identifiers[random.Next(Identifiers.Length)];
            builder.Append(word);
            builder.Append(random.Next(3) == 0 ? "();" : " ");
        }

        builder.Length = start + length;
    }
}
