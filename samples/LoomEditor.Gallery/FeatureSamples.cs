namespace LoomEditor.Gallery;

/// <summary>One entry of the "Features" picker: a text to load, where to put the cursor, and what to try.</summary>
internal sealed record FeatureSample(string Name, string LanguageId, string Text, int Line, int Column, string Hint);

/// <summary>Short hand-written samples, one per phase 5 feature (find, folding, brackets, editing commands, word highlight).</summary>
/// <remarks>Threading: every member is pure.</remarks>
internal static class FeatureSamples
{
    private static readonly string[] BlockLanguages = ["csharp", "typescript", "javascript", "json", "html", "css", "xml", "markdown", "python", "yaml"];

    /// <summary>All samples, in picker order.</summary>
    public static IReadOnlyList<FeatureSample> All { get; } = Create();

    private static List<FeatureSample> Create()
    {
        var samples = new List<FeatureSample>
        {
            new("Find / replace", "csharp", FindText, 3, 20, "Ctrl+F, Ctrl+H, Alt+C / Alt+W / Alt+R, Ctrl+D, Enter / Shift+Enter, Escape"),
            new("Folding", "csharp", FoldingText, 1, 1, "Hover the gutter, click the chevrons, Ctrl+Shift+[ / ], Ctrl+K Ctrl+0 / J, Ctrl+K Ctrl+8"),
            new("Folding (off-side, Python)", "python", PythonFolding, 1, 1, "Fold the blocks: blank lines inside a block stay in the fold"),
            new("Folding (off-side, YAML)", "yaml", YamlFolding, 1, 1, "Fold the blocks: blank lines inside a block stay in the fold"),
            new("Brackets", "typescript", BracketsText, 1, 12, "Colorized pairs, matching bracket box, Ctrl+Shift+\\ to jump"),
            new("Line operations", "csharp", LinesText, 3, 1, "Alt+Up/Down, Shift+Alt+Up/Down, Ctrl+Shift+K, Ctrl+Enter, Ctrl+Shift+Enter"),
            new("Multi-cursor", "csharp", MultiCursorText, 1, 5, "Ctrl+D, Ctrl+K Ctrl+D, Ctrl+Shift+L, Ctrl+Alt+Up/Down"),
            new("Word highlight", "csharp", FindText, 5, 20, "Put the cursor on an identifier: occurrences light up; F7 / Shift+F7 cycle"),
            new("Indentation (tabs)", "csharp", TabText, 1, 1, "The Tab size box follows the file; Tab inserts a tab character"),
        };

        foreach (string id in BlockLanguages)
        {
            samples.Add(new(
                $"Typing: {id}", id, "\n\n", 1, 1,
                "Type { ( [ \" ' then Enter between brackets, Ctrl+/ for line comments, Shift+Alt+A for block comments"));
        }

        return samples;
    }

    private const string FindText = """
        using System;

        public sealed class Counter
        {
            private int _count;
            private int _step = 1;

            public int Count => _count;

            public void Increment() => _count += _step;

            public void Reset() => _count = 0;

            public override string ToString() => $"count={_count}, step={_step}";
        }

        // log: 2024-01-05 INFO  start
        // log: 2024-01-05 WARN  retry 3
        // log: 2024-01-06 INFO  stop
        // log: 2024-01-06 ERROR retry 12
        """;

    private const string FoldingText = """
        namespace Demo;

        #region Models
        public class Outer
        {
            public class Inner
            {
                public void A()
                {
                    if (true)
                    {
                        Console.WriteLine("a");
                    }
                }

                public void B()
                {
                    Console.WriteLine("b");
                }
            }
        }
        #endregion

        /*
         * A long comment
         * on several lines.
         */
        public static class Util
        {
            public static int Twice(int x)
            {
                return x * 2;
            }
        }
        """;

    private const string PythonFolding = """
        def first(a, b):
            total = a + b

            if total > 10:
                print("big")

            return total


        class Shape:
            def area(self):
                return 0

            def name(self):
                return "shape"
        """;

    private const string YamlFolding = """
        server:
          host: localhost

          ports:
            - 80
            - 443

        database:
          user: admin
          options:
            timeout: 30

            retries: 3
        """;

    private const string BracketsText = """
        const value = f(g([1, 2, { a: (3 + [4, 5][0]) }]));
        const text = "a ( bracket in a string";
        // a ) bracket in a comment
        const deep = [[[[[[(((({ x: [1] }))))]]]]]];
        """;

    private const string LinesText = """
        line one
        line two
        line three
        line four
        line five
        """;

    private const string MultiCursorText = """
        var alpha = 1;
        var beta = alpha + 1;
        var gamma = alpha + beta;
        alpha++;
        """;

    private const string TabText = "public class T\n{\n\tpublic void A()\n\t{\n\t\tint x = 1;\n\t}\n\n\tpublic void B()\n\t{\n\t\tint y = 2;\n\t}\n}\n";
}
