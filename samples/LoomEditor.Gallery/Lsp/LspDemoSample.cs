namespace LoomEditor.Gallery.Lsp;

/// <summary>The text of the language server demo and of the self-check. Line 14 column 17 is right after <c>Console.</c>; <c>Program</c> of line 13 is at column 9.</summary>
/// <remarks>Threading: constants only.</remarks>
internal static class LspDemoSample
{
    /// <summary>A real type error for <c>csharp-ls</c>, the word <c>ERROR</c> for the fake server, a class and a use of it, and an open <c>Console.</c>.</summary>
    internal const string Text =
        "using System;\n\nclass Program\n{\n    static void Greet()\n    {\n        Console.WriteLine(\"Hello\"); // ERROR\n    }\n\n"
        + "    static void Main()\n    {\n        int number = \"text\";\n        Program.Greet();\n        Console.\n    }\n}\n";
}
