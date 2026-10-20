using System.Text;

namespace LoomEditor.Gallery;

/// <summary>Short samples, one per built-in language, and a large generated C# file for the background tokenizer.</summary>
/// <remarks>Threading: every member is pure, so it can run on a background thread.</remarks>
internal static class HighlightingSamples
{
    /// <summary>The sample of a language id (<c>BuiltInLanguages.Ids</c>); plain text for an unknown id.</summary>
    public static string For(string languageId) => languageId switch
    {
        "csharp" => CSharp,
        "typescript" => TypeScript,
        "javascript" => JavaScript,
        "json" => Json,
        "jsonc" => Jsonc,
        "html" => Html,
        "css" => Css,
        "xml" => Xml,
        "markdown" => Markdown,
        "python" => Python,
        "yaml" => Yaml,
        _ => "Plain text has no colors.\nLine two.",
    };

    /// <summary>About 20 000 lines of C#: classes of 8 lines, with comments and strings, so that the worker has work to do.</summary>
    public static string CreateLargeCSharp()
    {
        var builder = new StringBuilder(20_000 * 40);
        builder.Append("namespace Generated;\n\n");
        for (int i = 0; builder.Length < 20_000 * 40; i++)
        {
            builder.Append("/// <summary>Generated class ").Append(i).Append(".</summary>\n");
            builder.Append("public sealed class Item").Append(i).Append('\n');
            builder.Append("{\n");
            builder.Append("    private const string Name = \"item ").Append(i).Append("\";\n");
            builder.Append("    // The answer: ").Append(i % 42).Append('\n');
            builder.Append("    public int Value => ").Append(i).Append(" /* inline */ + 1;\n");
            builder.Append("    public override string ToString() => Name + Value;\n");
            builder.Append("}\n\n");
        }

        return builder.ToString();
    }

    private const string CSharp = """
        using System;
        using System.Collections.Generic;

        namespace Demo;

        /// <summary>A tiny counter.</summary>
        public sealed class Counter
        {
            private readonly Dictionary<string, int> _counts = new();

            public int Add(string key, int amount = 1)
            {
                // Strings, numbers and keywords get their own colors.
                _counts[key] = _counts.GetValueOrDefault(key) + amount;
                return _counts[key];
            }

            public static void Main()
            {
                var counter = new Counter();
                Console.WriteLine($"Total: {counter.Add("a", 3)}");
            }
        }
        """;

    private const string TypeScript = """
        interface Point { x: number; y: number }

        export class Shape<T extends Point> {
          constructor(private readonly points: T[]) {}

          /** The number of points. */
          get size(): number {
            return this.points.length; // a comment
          }
        }

        const origin: Point = { x: 0, y: 0 };
        console.log(new Shape([origin]).size, `template ${origin.x}`);
        """;

    private const string JavaScript = """
        // A small module
        import { readFile } from "node:fs/promises";

        export async function countLines(path) {
          const text = await readFile(path, "utf8");
          return text.split(/\r?\n/).length;
        }

        const total = [1, 2, 3].map((n) => n * 2).reduce((a, b) => a + b, 0);
        console.log(`total = ${total}`);
        """;

    private const string Json = """
        {
          "name": "loom",
          "version": "1.0.0",
          "private": true,
          "keywords": ["editor", "winui"],
          "nested": { "enabled": true, "ratio": 0.5, "nothing": null }
        }
        """;

    private const string Jsonc = """
        {
          // JSON with comments
          "editor.fontSize": 14, /* trailing commas are fine too */
          "files.exclude": {
            "**/bin": true,
          },
        }
        """;

    private const string Html = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <title>Loom</title>
          <style>
            body { font-family: sans-serif; color: #333; }
          </style>
        </head>
        <body>
          <h1 class="title">Hello</h1>
          <script>
            const greeting = "hi";
            document.title = greeting;
          </script>
        </body>
        </html>
        """;

    private const string Css = """
        :root { --accent: #0078d4; }

        .editor > .line:hover {
          color: var(--accent);
          margin: 0 auto;
          font: 14px/1.5 "Cascadia Mono", monospace;
        }

        @media (max-width: 600px) { .editor { display: none; } }
        """;

    private const string Xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <!-- A project file -->
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net11.0</TargetFramework>
            <Nullable>enable</Nullable>
          </PropertyGroup>
        </Project>
        """;

    private const string Markdown = """
        # LoomEditor

        A **native** editor with *syntax highlighting*.

        - Item one
        - Item two with `inline code`

        > A quote

        ```csharp
        Console.WriteLine("fenced code is highlighted too");
        ```

        [A link](https://example.com)
        """;

    private const string Python = """
        import os
        from dataclasses import dataclass


        @dataclass
        class Point:
            x: int = 0
            y: int = 0

            def distance(self) -> float:
                '''Distance to the origin.'''
                return (self.x ** 2 + self.y ** 2) ** 0.5


        # a comment
        print(f"{Point(3, 4).distance()}", os.getcwd())
        """;

    private const string Yaml = """
        # A workflow
        name: build
        on: [push, pull_request]
        jobs:
          test:
            runs-on: windows-latest
            steps:
              - uses: actions/checkout@v4
              - run: dotnet test --configuration Release
                env:
                  DEBUG: "false"
                  RETRIES: 3
        """;
}
