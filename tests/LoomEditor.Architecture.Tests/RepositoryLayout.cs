using System.Xml.Linq;

namespace LoomEditor.Architecture.Tests;

/// <summary>Finds the repository folders and reads project files.</summary>
internal static class RepositoryLayout
{
    /// <summary>The folder that contains LoomEditor.slnx.</summary>
    public static DirectoryInfo Root { get; } = FindRoot();

    /// <summary>The src/ folder.</summary>
    public static DirectoryInfo Source { get; } = new(Path.Combine(Root.FullName, "src"));

    /// <summary>Every project under src/, by project name (e.g. "LoomEditor.Core").</summary>
    public static IReadOnlyDictionary<string, XDocument> SourceProjects { get; } =
        Source.EnumerateFiles("*.csproj", SearchOption.AllDirectories)
            .ToDictionary(file => Path.GetFileNameWithoutExtension(file.Name), file => XDocument.Load(file.FullName));

    /// <summary>Names of the projects referenced with &lt;ProjectReference&gt;.</summary>
    public static IEnumerable<string> GetProjectReferences(XDocument project) =>
        project.Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension(reference.Attribute("Include")!.Value));

    /// <summary>Ids of the NuGet packages referenced with &lt;PackageReference&gt;.</summary>
    public static IEnumerable<string> GetPackageReferences(XDocument project) =>
        project.Descendants("PackageReference").Select(reference => reference.Attribute("Include")!.Value);

    /// <summary>The value of a property, or null if the project does not set it.</summary>
    public static string? GetProperty(XDocument project, string name) =>
        project.Descendants(name).FirstOrDefault()?.Value;

    private static DirectoryInfo FindRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "LoomEditor.slnx")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new InvalidOperationException("LoomEditor.slnx not found above " + AppContext.BaseDirectory);
    }
}
