using LoomEditor.Gallery.Lsp;

namespace LoomEditor.Lsp.Tests.Gallery;

public sealed class LspDemoWorkspaceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "loom-lsp-ws-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Create_WritesBothFiles()
    {
        LspDemoWorkspaceInfo info = LspDemoWorkspace.Create(_folder, "x");

        Assert.True(File.Exists(Path.Combine(info.Folder, "Sample.csproj")));
        Assert.True(File.Exists(info.ProgramPath));
        Assert.Contains("net8.0", File.ReadAllText(Path.Combine(info.Folder, "Sample.csproj")));
    }

    [Fact]
    public void Create_ProgramTextIsExact_NoBom()
    {
        LspDemoWorkspaceInfo info = LspDemoWorkspace.Create(_folder, "class A { }\r\né");

        byte[] bytes = File.ReadAllBytes(info.ProgramPath);

        Assert.Equal("class A { }\r\né", System.Text.Encoding.UTF8.GetString(bytes));
        Assert.NotEqual(0xEF, bytes[0]);
    }

    [Fact]
    public async Task RestoreAsync_AssetsFileExists_DoesNothing()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "obj"));
        File.WriteAllText(Path.Combine(_folder, "obj", "project.assets.json"), "{}");

        Assert.Null(await LspDemoWorkspace.RestoreAsync(_folder));
    }

    [Fact]
    public async Task RestoreAsync_NotRestored_WritesTheAssetsFile()
    {
        LspDemoWorkspaceInfo info = LspDemoWorkspace.Create(_folder, "class A { }");

        string? error = await LspDemoWorkspace.RestoreAsync(info.Folder);

        Assert.Null(error);
        Assert.True(File.Exists(Path.Combine(info.Folder, "obj", "project.assets.json")));
    }

    [Fact]
    public void Create_Overwrites()
    {
        LspDemoWorkspace.Create(_folder, "first");
        LspDemoWorkspaceInfo info = LspDemoWorkspace.Create(_folder, "second");

        Assert.Equal("second", File.ReadAllText(info.ProgramPath));
    }

    [Fact]
    public void Create_UrisAreFileUris()
    {
        LspDemoWorkspaceInfo info = LspDemoWorkspace.Create(_folder, "x");

        Assert.StartsWith("file:///", info.FolderUri, StringComparison.Ordinal);
        Assert.StartsWith("file:///", info.ProgramUri, StringComparison.Ordinal);
        Assert.EndsWith("Program.cs", info.ProgramUri, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_FolderWithSpaces_UriEncoded()
    {
        LspDemoWorkspaceInfo info = LspDemoWorkspace.Create(Path.Combine(_folder, "with space"), "x");

        Assert.Contains("with%20space", info.ProgramUri, StringComparison.Ordinal);
        Assert.DoesNotContain(' ', info.ProgramUri);
    }
}
