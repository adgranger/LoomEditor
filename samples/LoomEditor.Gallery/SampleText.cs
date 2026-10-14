namespace LoomEditor.Gallery;

/// <summary>Sample documents shown in the gallery.</summary>
internal static class SampleText
{
    /// <summary>A small C# file.</summary>
    private const string Header = """
        // Welcome to LoomEditor!
        // This text is drawn by LoomEditor.Rendering (Win2D / DirectWrite),
        // inside the LoomEditor WinUI control, in an app compiled with Native AOT.
        // Tabs use tab stops, and CJK characters their real width: 你好，世界
        // Tab stop:	x	y	z

        using System;

        namespace Hello;

        public static class Program
        {
            public static void Main()
            {
                Console.WriteLine("Hello from LoomEditor!");
            }
        }
        """;

    /// <summary>The small C# file, followed by 200 numbered lines and one 300-character line, so that it scrolls.</summary>
    public static string HelloWorld { get; } = Header + Environment.NewLine + string.Join(
        Environment.NewLine,
        Enumerable.Range(1, 200).Select(i => $"// Numbered line {i}"))
        + Environment.NewLine + "// " + new string('x', 297);
}
