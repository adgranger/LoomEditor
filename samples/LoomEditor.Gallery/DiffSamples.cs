namespace LoomEditor.Gallery;

/// <summary>The two texts compared by the diff editor preview.</summary>
internal static class DiffSamples
{
    private static readonly string Filler = string.Join(
        Environment.NewLine,
        Enumerable.Range(1, 40).Select(i => $"    // Unchanged line {i}"));

    /// <summary>The original side.</summary>
    public static string Original { get; } = string.Join(
        Environment.NewLine,
        "using System;",
        "",
        "public static class Greeter",
        "{",
        "    public static void Hello()",
        "    {",
        "        Console.WriteLine(\"Hello\");",
        "    }",
        Filler,
        "    public static int Add(int a, int b)",
        "    {",
        "        return a + b;",
        "    }",
        "}");

    /// <summary>The modified side: a changed signature, a changed return and an added method.</summary>
    public static string Modified { get; } = string.Join(
        Environment.NewLine,
        "using System;",
        "",
        "public static class Greeter",
        "{",
        "    public static void Hello(string name)",
        "    {",
        "        Console.WriteLine($\"Hello, {name}!\");",
        "    }",
        Filler,
        "    public static int Add(int a, int b)",
        "    {",
        "        return checked(a + b);",
        "    }",
        "",
        "    public static int Subtract(int a, int b)",
        "    {",
        "        return a - b;",
        "    }",
        "}");
}
