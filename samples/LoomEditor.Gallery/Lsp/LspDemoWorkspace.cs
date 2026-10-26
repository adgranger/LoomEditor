using System.Text;

namespace LoomEditor.Gallery.Lsp;

/// <summary>Where the demo workspace was written.</summary>
/// <param name="Folder">The workspace folder.</param>
/// <param name="FolderUri">Its file URI.</param>
/// <param name="ProgramPath">The path of <c>Program.cs</c>.</param>
/// <param name="ProgramUri">Its file URI.</param>
internal sealed record LspDemoWorkspaceInfo(string Folder, string FolderUri, string ProgramPath, string ProgramUri);

/// <summary>Writes the tiny project a real C# language server needs to load the demo document.</summary>
/// <remarks>Threading: file work only, safe from any thread.</remarks>
internal static class LspDemoWorkspace
{
    private const string ProjectText =
        "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>"
        + "<Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>";

    /// <summary>The default parent folder: <c>%LOCALAPPDATA%\LoomEditor.Gallery</c>.</summary>
    internal static string DefaultBaseFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LoomEditor.Gallery");

    /// <summary>Writes <c>Sample.csproj</c> and <c>Program.cs</c> (overwriting both) under <c>lsp-workspace</c> in <paramref name="baseFolder"/>.</summary>
    /// <exception cref="IOException">The files could not be written.</exception>
    internal static LspDemoWorkspaceInfo Create(string baseFolder, string programText)
    {
        string folder = Path.Combine(baseFolder, "lsp-workspace");
        Directory.CreateDirectory(folder);
        UTF8Encoding utf8 = new(false);
        File.WriteAllText(Path.Combine(folder, "Sample.csproj"), ProjectText, utf8);
        string program = Path.Combine(folder, "Program.cs");
        File.WriteAllText(program, programText, utf8);
        return new LspDemoWorkspaceInfo(folder, new Uri(folder).AbsoluteUri, program, new Uri(program).AbsoluteUri);
    }
}
