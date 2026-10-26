using LoomEditor.Gallery.Lsp;

namespace LoomEditor.Lsp.Tests.Gallery;

public sealed class LspServerLocatorTests
{
    private static readonly string Tool = Path.Combine("home", ".dotnet", "tools", OperatingSystem.IsWindows() ? "csharp-ls.exe" : "csharp-ls");

    private static LspServerChoice? Find(
        Dictionary<string, string>? env = null,
        Func<string, bool>? fileExists = null,
        string[]? pathFolders = null) =>
        LspServerLocator.Find(
            name => env is not null && env.TryGetValue(name, out string? value) ? value : null,
            fileExists ?? (_ => false),
            "home",
            pathFolders ?? []);

    [Fact]
    public void EnvVar_Wins_OverFiles()
    {
        LspServerChoice? choice = Find(new() { ["LOOMEDITOR_LSP_COMMAND"] = "myserver --stdio" }, _ => true);

        Assert.NotNull(choice);
        Assert.Equal("myserver", choice.FileName);
        Assert.Equal(["--stdio"], choice.Arguments);
        Assert.Equal("csharp", choice.LanguageId);
        Assert.Equal("LOOMEDITOR_LSP_COMMAND", choice.Description);
    }

    [Fact]
    public void EnvVar_QuotedPathWithSpaces()
    {
        LspServerChoice? choice = Find(new() { ["LOOMEDITOR_LSP_COMMAND"] = "\"C:\\Program Files\\x\\server.exe\" --stdio" });

        Assert.NotNull(choice);
        Assert.Equal("C:\\Program Files\\x\\server.exe", choice.FileName);
        Assert.Equal(["--stdio"], choice.Arguments);
    }

    [Fact]
    public void EnvVar_Language_FromSecondVariable()
    {
        LspServerChoice? choice = Find(new() { ["LOOMEDITOR_LSP_COMMAND"] = "srv", ["LOOMEDITOR_LSP_LANGUAGE"] = "python" });

        Assert.Equal("python", choice?.LanguageId);
    }

    [Fact]
    public void EnvVar_Empty_Ignored() => Assert.Null(Find(new() { ["LOOMEDITOR_LSP_COMMAND"] = string.Empty }));

    [Fact]
    public void EnvVar_OnlyWhitespace_Ignored() => Assert.Null(Find(new() { ["LOOMEDITOR_LSP_COMMAND"] = "   " }));

    [Fact]
    public void DotnetToolsFolder_Found()
    {
        LspServerChoice? choice = Find(fileExists: path => path == Tool);

        Assert.NotNull(choice);
        Assert.Equal(Tool, choice.FileName);
        Assert.Empty(choice.Arguments);
        Assert.Equal("csharp", choice.LanguageId);
        Assert.Equal("csharp-ls (dotnet tool)", choice.Description);
    }

    [Fact]
    public void Path_Found_WhenNotInDotnetTools()
    {
        string expected = Path.Combine("other", OperatingSystem.IsWindows() ? "csharp-ls.exe" : "csharp-ls");

        LspServerChoice? choice = Find(fileExists: path => path == expected, pathFolders: ["first", "other"]);

        Assert.Equal(expected, choice?.FileName);
    }

    [Fact]
    public void NothingFound_Null() => Assert.Null(Find(pathFolders: ["first"]));

    [Fact]
    public void Find_InvalidPathFolder_Skipped() => Assert.Null(Find(pathFolders: ["bad\0folder", "a\0b"]));

    [Theory]
    [InlineData("", new string[0])]
    [InlineData("one", new[] { "one" })]
    [InlineData("one two", new[] { "one", "two" })]
    [InlineData("\"a b\" c", new[] { "a b", "c" })]
    [InlineData("\"a\"\"b\" c", new[] { "ab", "c" })]
    [InlineData("one two ", new[] { "one", "two" })]
    public void SplitCommandLine_Cases(string commandLine, string[] expected) =>
        Assert.Equal(expected, LspServerLocator.SplitCommandLine(commandLine));
}
