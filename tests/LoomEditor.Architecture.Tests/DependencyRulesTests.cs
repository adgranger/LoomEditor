using System.Reflection;
using System.Xml.Linq;

namespace LoomEditor.Architecture.Tests;

/// <summary>
/// Rules 1 and 2 of docs/ARCHITECTURE.md: the core does not know WinUI, and dependencies go one way only:
/// WinUI → Rendering → View → (TextMate, Themes) → Core.
/// </summary>
public class DependencyRulesTests
{
    /// <summary>
    /// For each project, the ONLY projects it may reference directly. If you need to change this table,
    /// you are changing the architecture: update docs/ARCHITECTURE.md and write an ADR (ADR 0011 added LoomEditor.Lsp).
    /// </summary>
    private static readonly Dictionary<string, string[]> AllowedReferences = new()
    {
        ["LoomEditor.Core"] = [],
        ["LoomEditor.TextMate"] = ["LoomEditor.Core"],
        ["LoomEditor.Themes"] = ["LoomEditor.Core"],
        ["LoomEditor.View"] = ["LoomEditor.Core", "LoomEditor.TextMate", "LoomEditor.Themes"],
        ["LoomEditor.Rendering"] = ["LoomEditor.View"],
        ["LoomEditor.WinUI"] = ["LoomEditor.Rendering"],
        ["LoomEditor.Lsp"] = ["LoomEditor.Core"],
    };

    public static TheoryData<string> SourceProjectNames => new(AllowedReferences.Keys);

    /// <summary>Projects that must run anywhere .NET runs: no Windows, no UI.</summary>
    public static TheoryData<string> UiIndependentProjects => new()
    {
        "LoomEditor.Core",
        "LoomEditor.TextMate",
        "LoomEditor.Themes",
        "LoomEditor.View",
        "LoomEditor.Lsp",
    };

    private static readonly string[] UiPackages = ["Microsoft.WindowsAppSDK", "Microsoft.Graphics.Win2D", "Microsoft.Windows.CsWinRT"];

    [Fact]
    public void EverySourceProject_IsListedInTheRules()
    {
        Assert.Equal(AllowedReferences.Keys.Order(), RepositoryLayout.SourceProjects.Keys.Order());
    }

    [Theory]
    [MemberData(nameof(SourceProjectNames))]
    public void Project_ReferencesOnlyAllowedProjects(string projectName)
    {
        XDocument project = RepositoryLayout.SourceProjects[projectName];

        string[] forbidden = RepositoryLayout.GetProjectReferences(project).Except(AllowedReferences[projectName]).ToArray();

        Assert.True(forbidden.Length == 0,
            $"{projectName} must not reference {string.Join(", ", forbidden)}. See docs/ARCHITECTURE.md (dependency rules).");
    }

    [Theory]
    [MemberData(nameof(UiIndependentProjects))]
    public void UiIndependentProject_TargetsPlainDotNet(string projectName)
    {
        XDocument project = RepositoryLayout.SourceProjects[projectName];

        Assert.Equal("$(LoomNetTargetFramework)", RepositoryLayout.GetProperty(project, "TargetFramework"));
        Assert.Null(RepositoryLayout.GetProperty(project, "UseWinUI"));
    }

    [Theory]
    [MemberData(nameof(UiIndependentProjects))]
    public void UiIndependentProject_DoesNotReferenceUiPackages(string projectName)
    {
        XDocument project = RepositoryLayout.SourceProjects[projectName];

        Assert.Empty(RepositoryLayout.GetPackageReferences(project).Intersect(UiPackages));
    }

    [Theory]
    [MemberData(nameof(UiIndependentProjects))]
    public void UiIndependentAssembly_ReferencesOnlyDotNetAndLoomEditor(string assemblyName)
    {
        Assembly assembly = Assembly.Load(assemblyName);

        string[] unexpected = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => !name.StartsWith("System", StringComparison.Ordinal)
                && !name.StartsWith("LoomEditor.", StringComparison.Ordinal)
                && name != "netstandard")
            .ToArray();

        Assert.True(unexpected.Length == 0, $"{assemblyName} references {string.Join(", ", unexpected)}.");
    }
}
