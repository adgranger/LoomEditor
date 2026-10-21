namespace LoomEditor.Architecture.Tests;

/// <summary>Repository conventions from CONTRIBUTING.md and docs/ARCHITECTURE.md.</summary>
public class ConventionTests
{
    private static readonly DirectoryInfo FeaturesFolder =
        new(Path.Combine(RepositoryLayout.Source.FullName, "LoomEditor.View", "Features"));

    public static TheoryData<string> FeatureFolders =>
        // Scheduling is infrastructure, not a feature (ADR 0009, decision 2).
        // Folding only has its ranges until P5-88 adds the feature class: P5-88 removes this exemption.
        new(FeaturesFolder.EnumerateDirectories().Select(directory => directory.Name).Where(name => name is not ("Scheduling" or "Folding")));

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
            Assert.True(IsValidPortedHeader(header),
                $"{path}: the second line must be '// Upstream commit: <sha>  —  Copyright (c) Microsoft Corporation. MIT License.'");
        }
    }

    [Theory]
    [InlineData("vscode")]
    [InlineData("vscode-textmate")]
    [InlineData("vscode-oniguruma")]
    public void PortedHeader_WithoutUpstreamCommitLine_IsRejected(string repository)
    {
        Assert.False(IsValidPortedHeader([$"// Ported from microsoft/{repository}: src/rule.ts", "namespace X;"]));
        Assert.False(IsValidPortedHeader([$"// Inspired by microsoft/{repository}: src/rule.ts"]));
        Assert.True(IsValidPortedHeader([$"// Ported from microsoft/{repository}: src/rule.ts", "// Upstream commit: 25b68dad"]));
    }

    [Fact]
    public void PortedHeader_OtherFile_IsAccepted()
    {
        Assert.True(IsValidPortedHeader(["namespace X;"]));
        Assert.True(IsValidPortedHeader([]));
    }

    // A file is ported when its first line names one of the three upstream repositories (docs/tasks/phase-4/README.md).
    private static bool IsValidPortedHeader(string[] header)
    {
        bool isPorted = header.Length > 0
            && (StartsWithRepository(header[0], "// Ported from microsoft/")
                || StartsWithRepository(header[0], "// Inspired by microsoft/"));

        return !isPorted || (header.Length == 2 && header[1].StartsWith("// Upstream commit: ", StringComparison.Ordinal));
    }

    private static bool StartsWithRepository(string line, string prefix) =>
        line.StartsWith(prefix + "vscode:", StringComparison.Ordinal)
        || line.StartsWith(prefix + "vscode-textmate:", StringComparison.Ordinal)
        || line.StartsWith(prefix + "vscode-oniguruma:", StringComparison.Ordinal);
}
