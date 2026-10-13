namespace LoomEditor.Gallery;

/// <summary>Sample documents shown in the gallery.</summary>
internal static class SampleText
{
    /// <summary>A small C# file.</summary>
    public const string HelloWorld = """
        // Welcome to LoomEditor!
        // This text is drawn by LoomEditor.Rendering (Win2D / DirectWrite),
        // inside the LoomEditor WinUI control, in an app compiled with Native AOT.

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
}
