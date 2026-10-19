namespace LoomEditor.TextMate.Tests.TestHelpers;

/// <summary>
/// Finds the vscode-textmate test cases (<c>TestCases/</c>, copied byte for byte from vscode-textmate's
/// <c>test-cases/</c>) next to the test assembly.
/// </summary>
internal static class TestCaseFiles
{
    /// <summary>The <c>TestCases</c> folder in the test output.</summary>
    public static string Root { get; } = Path.Combine(AppContext.BaseDirectory, "TestCases");

    /// <summary>The full path of a file under <c>TestCases/</c> (<c>/</c>-separated).</summary>
    public static string PathOf(string relativePath) =>
        Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>The text of a file under <c>TestCases/</c>.</summary>
    public static string ReadAllText(string relativePath) => File.ReadAllText(PathOf(relativePath));

    /// <summary>The names of the files of a folder under <c>TestCases/</c> that match <paramref name="pattern"/>, sorted.</summary>
    public static string[] FileNames(string relativeFolder, string pattern) =>
        [.. Directory.GetFiles(PathOf(relativeFolder), pattern).Select(path => Path.GetFileName(path)).Order(StringComparer.Ordinal)];
}
