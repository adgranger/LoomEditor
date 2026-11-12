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
        "lua" => Lua,
        "yaml" => Yaml,
        "c" => C,
        "cpp" => Cpp,
        "java" => Java,
        "go" => Go,
        "rust" => Rust,
        "php" => Php,
        "ruby" => Ruby,
        "sql" => Sql,
        "shellscript" => Shell,
        "powershell" => PowerShell,
        "scss" => Scss,
        "less" => Less,
        "swift" => Swift,
        "dart" => Dart,
        "dockerfile" => Dockerfile,
        "ini" => Ini,
        "diff" => Diff,
        "javascriptreact" => JavaScriptReact,
        "jsonl" => JsonLines,
        "snippets" => CodeSnippets,
        "xsl" => Xsl,
        "dockercompose" => DockerCompose,
        "git-commit" => GitCommit,
        "git-rebase" => GitRebase,
        "ignore" => Ignore,
        "objective-c" => ObjectiveC,
        "objective-cpp" => ObjectiveCpp,
        "cuda-cpp" => Cuda,
        "r" => R,
        "perl" => Perl,
        "raku" => Raku,
        "fsharp" => FSharp,
        "vb" => VisualBasic,
        "razor" => Razor,
        "clojure" => Clojure,
        "coffeescript" => CoffeeScript,
        "groovy" => Groovy,
        "julia" => Julia,
        "handlebars" => Handlebars,
        "jade" => Pug,
        "hlsl" => Hlsl,
        "shaderlab" => ShaderLab,
        "tex" => Tex,
        "latex" => Latex,
        "bibtex" => Bibtex,
        "restructuredtext" => Rst,
        "makefile" => Makefile,
        "bat" => Batch,
        "dotenv" => Dotenv,
        "properties" => Properties,
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

    private const string Lua = """
        -- A small module
        local M = {}

        function M.fib(n)
          if n < 2 then return n end
          return M.fib(n - 1) + M.fib(n - 2)
        end

        for i = 1, 10 do
          print(("fib(%d) = %d"):format(i, M.fib(i)))
        end

        --[[ a block
        comment ]]
        return M
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

    private const string C = """
        #include <stdio.h>

        /* Prints the first squares. */
        int main(void) {
            for (int i = 1; i <= 5; i++) {
                printf("%d\n", i * i);
            }
            return 0;
        }
        """;

    private const string Cpp = """
        #include <iostream>
        #include <vector>

        // Sums a vector.
        template <typename T>
        T sum(const std::vector<T>& v) {
            T total{};
            for (const auto& x : v) total += x;
            return total;
        }

        int main() {
            std::cout << sum<int>({1, 2, 3}) << "\n";
        }
        """;

    private const string Java = """
        package demo;

        /** A tiny greeter. */
        public class Greeter {
            private final String name;

            public Greeter(String name) { this.name = name; }

            @Override
            public String toString() {
                return "Hello, " + name + "!"; // greeting
            }
        }
        """;

    private const string Go = """
        package main

        import "fmt"

        // fib returns the n-th Fibonacci number.
        func fib(n int) int {
        	if n < 2 {
        		return n
        	}
        	return fib(n-1) + fib(n-2)
        }

        func main() {
        	fmt.Println(fib(10))
        }
        """;

    private const string Rust = """
        use std::collections::HashMap;

        /// Counts the words of a text.
        fn count(text: &str) -> HashMap<&str, usize> {
            let mut map = HashMap::new();
            for word in text.split_whitespace() {
                *map.entry(word).or_insert(0) += 1;
            }
            map
        }

        fn main() {
            println!("{:?}", count("a b a"));
        }
        """;

    private const string Php = """
        <!DOCTYPE html>
        <html>
        <body>
        <?php
        // A short loop
        $names = ["Ada", "Linus"];
        foreach ($names as $name) {
            echo "<p>Hello, $name!</p>";
        }
        ?>
        </body>
        </html>
        """;

    private const string Ruby = """
        # A small class
        class Greeter
          def initialize(name)
            @name = name
          end

          def greet
            puts "Hello, #{@name}!"
          end
        end

        Greeter.new("Ada").greet
        """;

    private const string Sql = """
        -- Top customers
        SELECT c.name, COUNT(*) AS orders
        FROM customers AS c
        JOIN orders AS o ON o.customer_id = c.id
        WHERE o.created_at >= '2026-01-01'
        GROUP BY c.name
        ORDER BY orders DESC;
        """;

    private const string Shell = """
        #!/bin/bash
        # Prints the size of each file
        for f in *.txt; do
          size=$(wc -c < "$f")
          echo "$f: $size bytes"
        done
        """;

    private const string PowerShell = """
        # Lists the large files
        Get-ChildItem -Path . -Recurse |
            Where-Object { $_.Length -gt 1MB } |
            ForEach-Object { Write-Host "$($_.Name): $($_.Length)" }
        """;

    private const string Scss = """
        $primary: #0078d4;

        @mixin rounded($radius: 4px) {
          border-radius: $radius;
        }

        .button {
          color: $primary;
          @include rounded;
          &:hover { color: darken($primary, 10%); }
        }
        """;

    private const string Less = """
        @primary: #0078d4;

        .rounded(@radius: 4px) {
          border-radius: @radius;
        }

        .button {
          color: @primary;
          .rounded();
          &:hover { color: darken(@primary, 10%); }
        }
        """;

    private const string Swift = """
        import Foundation

        /// A tiny greeter.
        struct Greeter {
            let name: String

            func greet() -> String {
                return "Hello, \(name)!"
            }
        }

        print(Greeter(name: "Ada").greet())
        """;

    private const string Dart = """
        import 'dart:math';

        /// A point in the plane.
        class Point {
          final double x, y;
          const Point(this.x, this.y);

          double distanceTo(Point other) =>
              sqrt(pow(x - other.x, 2) + pow(y - other.y, 2));
        }

        void main() => print(Point(0, 0).distanceTo(Point(3, 4)));
        """;

    private const string Dockerfile = """
        # Build stage
        FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
        WORKDIR /src
        COPY . .
        RUN dotnet publish -c Release -o /app

        FROM mcr.microsoft.com/dotnet/runtime:10.0
        COPY --from=build /app /app
        ENTRYPOINT ["dotnet", "/app/App.dll"]
        """;

    private const string Ini = """
        ; Settings
        [editor]
        fontSize=14
        wordWrap = on

        [theme]
        name = "Dark Modern"
        """;

    private const string Diff = """
        diff --git a/a.txt b/a.txt
        index 83db48f..bf2a3c1 100644
        --- a/a.txt
        +++ b/a.txt
        @@ -1,3 +1,3 @@
         unchanged
        -removed
        +added
        """;

    private const string ObjectiveC = """
        #import <Foundation/Foundation.h>

        @interface Greeter : NSObject
        - (NSString *)greet:(NSString *)name;
        @end

        @implementation Greeter
        - (NSString *)greet:(NSString *)name {
            return [NSString stringWithFormat:@"Hello, %@!", name];
        }
        @end
        """;

    private const string ObjectiveCpp = """
        #import <Foundation/Foundation.h>
        #include <vector>

        @implementation Counter
        - (NSUInteger)count {
            std::vector<int> values = {1, 2, 3};
            return values.size();
        }
        @end
        """;

    private const string Cuda = """
        #include <cuda_runtime.h>

        __global__ void add(const float* a, const float* b, float* c, int n) {
            int i = blockIdx.x * blockDim.x + threadIdx.x;
            if (i < n) {
                c[i] = a[i] + b[i];
            }
        }

        int main() {
            add<<<16, 256>>>(nullptr, nullptr, nullptr, 0);
            return 0;
        }
        """;

    private const string R = """
        # Mean of a vector
        x <- c(1, 2, 3, 4.5)
        mean_of <- function(values, na.rm = FALSE) {
          sum(values, na.rm = na.rm) / length(values)
        }
        print(paste("mean:", mean_of(x)))
        """;

    private const string Perl = """
        #!/usr/bin/perl
        use strict;
        use warnings;

        my %ages = (alice => 30, bob => 25);
        foreach my $name (sort keys %ages) {
            print "$name is $ages{$name}\n";
        }
        """;

    private const string Raku = """
        use v6;

        my %ages = alice => 30, bob => 25;
        for %ages.keys.sort -> $name {
            say "$name is %ages{$name}";
        }
        """;

    private const string FSharp = """
        module Greeter

        // Greets someone
        let greet (name: string) =
            sprintf "Hello, %s!" name

        [<EntryPoint>]
        let main argv =
            printfn "%s" (greet "world")
            0
        """;

    private const string VisualBasic = """
        ' Greets someone
        Public Class Greeter
            Public Function Greet(name As String) As String
                Return "Hello, " & name & "!"
            End Function
        End Class
        """;

    private const string Razor = """
        @page "/counter"
        @{
            var count = 3;
        }
        <h1>Counter</h1>
        <p class="total">Current count: @count</p>
        @if (count > 1)
        {
            <p>More than one.</p>
        }
        """;

    private const string Clojure = """
        ; Greets someone
        (ns greeter.core)

        (defn greet [name]
          (str "Hello, " name "!"))

        (println (greet "world"))
        """;

    private const string CoffeeScript = """
        # Greets someone
        greet = (name) -> "Hello, #{name}!"

        class Greeter
          constructor: (@name) ->
          hello: -> greet @name

        console.log new Greeter("world").hello()
        """;

    private const string Groovy = """
        // Greets someone
        class Greeter {
            String name

            String greet() {
                "Hello, ${name}!"
            }
        }

        println new Greeter(name: 'world').greet()
        """;

    private const string Julia = """
        # Greets someone
        function greet(name::String)
            return "Hello, $(name)!"
        end

        println(greet("world"))
        """;

    private const string Handlebars = """
        <div class="entry">
          {{! A comment }}
          <h1>{{title}}</h1>
          {{#each items}}
            <li>{{this.name}}</li>
          {{/each}}
        </div>
        """;

    private const string Pug = """
        doctype html
        html
          head
            title= pageTitle
          body
            // A comment
            h1.title Hello
            ul
              each item in items
                li= item
        """;

    private const string Hlsl = """
        // Pixel shader
        Texture2D tex : register(t0);
        SamplerState samp : register(s0);

        float4 main(float2 uv : TEXCOORD0) : SV_Target
        {
            return tex.Sample(samp, uv) * 0.5f;
        }
        """;

    private const string ShaderLab = """
        Shader "Custom/Unlit"
        {
            Properties { _Color ("Color", Color) = (1, 1, 1, 1) }
            SubShader
            {
                Pass
                {
                    CGPROGRAM
                    #pragma vertex vert
                    ENDCG
                }
            }
        }
        """;

    private const string Tex = """
        % A package
        \ProvidesPackage{greeter}
        \newcommand{\greet}[1]{Hello, #1!}
        \def\answer{42}
        """;

    private const string Latex = """
        \documentclass{article}
        \begin{document}
        % A comment
        \section{Intro}
        Euler: $e^{i\pi} + 1 = 0$.
        \begin{equation}
          \int_0^1 x\,dx = \frac{1}{2}
        \end{equation}
        \end{document}
        """;

    private const string Bibtex = """
        @article{knuth84,
          author  = {Donald E. Knuth},
          title   = {Literate Programming},
          journal = {The Computer Journal},
          year    = 1984
        }
        """;

    private const string Rst = """
        Title
        =====

        A paragraph with *emphasis* and ``code``.

        .. note:: A directive.

        - first item
        - second item
        """;

    // A raw string cannot hold the tabs a recipe line needs once indented, so this one is escaped.
    private const string Makefile = "# Build\nCC = gcc\nCFLAGS := -O2 -Wall\n\nall: app\n\napp: main.o util.o\n\t$(CC) $(CFLAGS) -o $@ $^\n\n.PHONY: clean\nclean:\n\trm -f app *.o\n";

    private const string Batch = """
        @echo off
        REM Builds the project
        set CONFIG=Release
        if "%1"=="debug" set CONFIG=Debug
        echo Building %CONFIG%
        dotnet build -c %CONFIG% || exit /b 1
        """;

    private const string Dotenv = """
        # Application settings
        APP_NAME=LoomEditor
        DEBUG=true
        DATABASE_URL="postgres://user:pass@localhost/db"
        """;

    private const string Properties = """
        # Build settings
        app.name=LoomEditor
        app.version = 1.0
        path=C:\\tools\\bin
        message=Hello, \
            world!
        """;

    private const string JavaScriptReact = """
        import React from 'react';

        // A component
        export function Greeting({ name }) {
            return <h1 className="title">Hello, {name}!</h1>;
        }
        """;

    private const string JsonLines = """
        {"id": 1, "name": "alpha", "tags": ["a", "b"]}
        {"id": 2, "name": "beta", "ok": true, "value": null}
        {"id": 3, "score": 4.5}
        """;

    private const string CodeSnippets = """
        {
            // A snippet
            "Log": {
                "prefix": "log",
                "body": ["console.log($1);", "$0"],
                "description": "Log to the console"
            }
        }
        """;

    private const string Xsl = """
        <?xml version="1.0"?>
        <xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
          <!-- A template -->
          <xsl:template match="/">
            <xsl:for-each select="items/item">
              <li><xsl:value-of select="@name"/></li>
            </xsl:for-each>
          </xsl:template>
        </xsl:stylesheet>
        """;

    private const string DockerCompose = """
        # Compose file
        services:
          web:
            image: "nginx:latest"
            ports:
              - "8080:80"
            environment:
              DEBUG: true
        """;

    private const string GitCommit = """
        Fix the tokenizer for long lines

        The previous limit cut the last token.

        # Please enter the commit message for your changes.
        """;

    private const string GitRebase = """
        pick 1a2b3c4 Add the grammar
        squash 5d6e7f8 Fix a typo
        # Rebase 1a2b3c4..5d6e7f8 onto 9a0b1c2
        """;

    private const string Ignore = """
        # Build output
        bin/
        obj/
        *.user
        !keep.user
        /node_modules
        """;
}
