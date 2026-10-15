using System.Text.RegularExpressions;

namespace LoomEditor.Architecture.Tests;

/// <summary>
/// The four UI-independent projects must stay portable (for example to an Avalonia backend on macOS and Linux):
/// their source files do not use Windows or WinUI namespaces or APIs. See docs/roadmap/phase-10-avalonia.md.
/// </summary>
public partial class PlatformNeutralSourceTests
{
    public static TheoryData<string> PortableProjects => new()
    {
        "LoomEditor.Core",
        "LoomEditor.TextMate",
        "LoomEditor.Themes",
        "LoomEditor.View",
    };

    [GeneratedRegex(@"\b(using\s+(static\s+)?(Windows|Microsoft\.UI|Microsoft\.Windows|Microsoft\.Graphics|WinRT|System\.Runtime\.InteropServices\.WindowsRuntime)\b|SupportedOSPlatform\s*\(\s*""windows)")]
    private static partial Regex WindowsSpecificApi();

    [Theory]
    [MemberData(nameof(PortableProjects))]
    public void PortableProject_SourceFiles_DoNotUseWindowsApis(string projectName)
    {
        string folder = Path.Combine(RepositoryLayout.Source.FullName, projectName);
        string separator = Path.DirectorySeparatorChar.ToString();

        string[] offenders = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains(separator + "obj" + separator) && !file.Contains(separator + "bin" + separator))
            .Where(file => WindowsSpecificApi().IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(RepositoryLayout.Root.FullName, file))
            .ToArray();

        Assert.True(offenders.Length == 0,
            $"{projectName} must stay platform neutral, but these files use Windows APIs: {string.Join(", ", offenders)}.");
    }
}
