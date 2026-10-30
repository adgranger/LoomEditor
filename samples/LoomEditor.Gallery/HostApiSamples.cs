namespace LoomEditor.Gallery;

/// <summary>
/// The text and the fake blame data of the host API demo. No WinUI types, so the tests can compile this file.
/// </summary>
/// <remarks>Threading: immutable.</remarks>
public static class HostApiSamples
{
    /// <summary>The file name the language is picked from.</summary>
    public const string FileName = "Conflicts.cs";

    private static readonly string[] Authors = ["alice", "bob", "carol", "dave"];

    // Ages in days of the successive commits: 1 ... 400.
    private static readonly int[] Ages = [1, 3, 9, 20, 45, 90, 200, 400, 2];

    /// <summary>A C# file with a conflict block: the start marker on line 9, current side 10-12, separator on 13, incoming side 14-16, end marker on 17.</summary>
    public static string Text { get; } = BuildText();

    /// <summary>The fake blame of a 1-based line: a new commit every 7 lines.</summary>
    /// <param name="line">The 1-based line number.</param>
    /// <returns>The commit id, the author and the age of the commit in days.</returns>
    public static (string Commit, string Author, int AgeDays) Blame(int line)
    {
        int commit = Math.Max(0, line - 1) / 7;
        return ((commit * 2654435 % 0xFFFFFF).ToString("x6", System.Globalization.CultureInfo.InvariantCulture), Authors[commit % Authors.Length], Ages[commit % Ages.Length]);
    }

    private static string BuildText()
    {
        List<string> lines =
        [
            "using System;",
            "using System.Collections.Generic;",
            string.Empty,
            "namespace Demo;",
            string.Empty,
            "public sealed class Inventory",
            "{",
            "    private readonly Dictionary<string, int> _stock = [];",
            "<<<<<<< current",
            "    public int Count => _stock.Count;",
            "    public bool IsEmpty => Count == 0;",
            "    public void Clear() => _stock.Clear();",
            "=======",
            "    public int Count => _stock.Count;",
            "    public bool IsEmpty => _stock.Count == 0;",
            "    public void Reset() { _stock.Clear(); }",
            ">>>>>>> incoming",
            string.Empty,
        ];
        for (int i = 1; lines.Count < 66; i++)
        {
            lines.Add($"    public int Get{i}(string key)");
            lines.Add("    {");
            lines.Add($"        return _stock.TryGetValue(key, out int value) ? value + {i} : 0;");
            lines.Add("    }");
            lines.Add(string.Empty);
        }

        lines.Add("}");
        return string.Join('\n', lines);
    }
}
