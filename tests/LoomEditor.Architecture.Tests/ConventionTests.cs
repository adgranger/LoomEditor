namespace LoomEditor.Architecture.Tests;

/// <summary>Repository conventions from CONTRIBUTING.md and docs/ARCHITECTURE.md.</summary>
public class ConventionTests
{
    private static readonly DirectoryInfo FeaturesFolder =
        new(Path.Combine(RepositoryLayout.Source.FullName, "LoomEditor.View", "Features"));

    public static TheoryData<string> FeatureFolders =>
        new(FeaturesFolder.EnumerateDirectories().Select(directory => directory.Name));

    [Theory]
    [MemberData(nameof(FeatureFolders))]
    public void FeatureFolder_HasTheStandardFiles(string featureName)
    {
        string folder = Path.Combine(FeaturesFolder.FullName, featureName);

        Assert.True(File.Exists(Path.Combine(folder, $"{featureName}Feature.cs")), $"Missing {featureName}Feature.cs");
        Assert.True(File.Exists(Path.Combine(folder, $"{featureName}Commands.cs")), $"Missing {featureName}Commands.cs");
        Assert.True(File.Exists(Path.Combine(folder, "README.md")), $"Missing README.md in Features/{featureName}");
    }

    [Theory]
    [MemberData(nameof(FeatureFolders))]
    public void FeatureFolder_IsRegisteredInEditorFeatures(string featureName)
    {
        string registry = File.ReadAllText(Path.Combine(FeaturesFolder.FullName, "EditorFeatures.cs"));

        Assert.Contains($"new {featureName}Feature()", registry, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceProjects_TrackTheirPublicApi()
    {
        foreach (string projectName in RepositoryLayout.SourceProjects.Keys)
        {
            string folder = Path.Combine(RepositoryLayout.Source.FullName, projectName);
            Assert.True(File.Exists(Path.Combine(folder, "PublicAPI.Shipped.txt")), $"{projectName}: missing PublicAPI.Shipped.txt");
            Assert.True(File.Exists(Path.Combine(folder, "PublicAPI.Unshipped.txt")), $"{projectName}: missing PublicAPI.Unshipped.txt");
        }
    }

    [Fact]
    public void PortedFiles_StateTheUpstreamCommit()
    {
        string[] csharpFiles = Directory.GetFiles(RepositoryLayout.Root.FullName, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        foreach (string path in csharpFiles)
        {
            string[] header = File.ReadLines(path).Take(2).ToArray();
            bool isPorted = header.Length > 0
                && (header[0].StartsWith("// Ported from microsoft/vscode:", StringComparison.Ordinal)
                    || header[0].StartsWith("// Inspired by microsoft/vscode:", StringComparison.Ordinal));

            if (isPorted)
            {
                Assert.True(header.Length == 2 && header[1].StartsWith("// Upstream commit: ", StringComparison.Ordinal),
                    $"{path}: the second line must be '// Upstream commit: <sha>  —  Copyright (c) Microsoft Corporation. MIT License.'");
            }
        }
    }
}
