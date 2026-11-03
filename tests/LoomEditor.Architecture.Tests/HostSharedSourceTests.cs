using System.Text.RegularExpressions;

namespace LoomEditor.Architecture.Tests;

/// <summary>Rules of the shared host source folder src/LoomEditor.HostShared (ADR 0017 decision 10).</summary>
public partial class HostSharedSourceTests
{
    // The WinUI branch is the default (not "#if LOOM_WINUI") so that tools parsing without the host symbols, such as SonarCloud, still see a namespace.
    private const string Opener = "#if !LOOM_AVALONIA";

    private static readonly string[] ExampleHeader =
    [
        "#if !LOOM_AVALONIA",
        "using LoomEditor.WinUI.Controls.Overlay;",
        "namespace LoomEditor.WinUI.Controls.DiffEditor;",
        "#elif LOOM_AVALONIA",
        "using LoomEditor.Avalonia.Controls.Overlay;",
        "namespace LoomEditor.Avalonia.Controls.DiffEditor;",
        "#endif",
    ];

    [GeneratedRegex(@"(?<![\w.])(Avalonia|Microsoft\.UI|Windows)\.")]
    private static partial Regex FrameworkNamespace();

    [GeneratedRegex(@"^\s*#\s*(if|elif|define)\b", RegexOptions.Multiline)]
    private static partial Regex Conditional();

    [GeneratedRegex(@"^namespace\s+([\w.]+);", RegexOptions.Multiline)]
    private static partial Regex FileScopedNamespace();

    [GeneratedRegex(@"^\s*(public|internal|sealed|static|partial|abstract|class|struct|enum|interface|record)\b")]
    private static partial Regex TypeDeclaration();

    private static string HostSharedFolder => Path.Combine(RepositoryLayout.Source.FullName, "LoomEditor.HostShared");

    private static string[] SharedFiles()
    {
        string separator = Path.DirectorySeparatorChar.ToString();
        return Directory.EnumerateFiles(HostSharedFolder, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains(separator + "obj" + separator) && !file.Contains(separator + "bin" + separator))
            .ToArray();
    }

    [Fact]
    public void HostSharedFiles_HaveOneHeaderBlock()
    {
        foreach (string file in SharedFiles())
        {
            Assert.True(HasValidHeaderBlock(File.ReadAllLines(file)),
                $"{file}: needs exactly one '{Opener}' header block before the first type, with only usings and namespace lines.");
        }
    }

    [Fact]
    public void HostSharedFiles_HaveNoOtherConditionalCode()
    {
        foreach (string file in SharedFiles())
        {
            int count = Conditional().Count(File.ReadAllText(file));
            Assert.True(count == 2, $"{file}: only the header block may use #if / #elif / #define (found {count} directives, expected 2).");
        }
    }

    [Fact]
    public void HostSharedNamespaces_MatchTheFolder()
    {
        foreach (string file in SharedFiles())
        {
            string folder = Path.GetRelativePath(HostSharedFolder, Path.GetDirectoryName(file)!).Replace('\\', '/');
            string[] namespaces = FileScopedNamespace().Matches(File.ReadAllText(file)).Select(match => match.Groups[1].Value).ToArray();
            Assert.Equal(2, namespaces.Length);

            Assert.StartsWith("LoomEditor.WinUI.", namespaces[0], StringComparison.Ordinal);
            Assert.StartsWith("LoomEditor.Avalonia.", namespaces[1], StringComparison.Ordinal);
            string suffix = namespaces[0]["LoomEditor.WinUI.".Length..];
            Assert.Equal(suffix, namespaces[1]["LoomEditor.Avalonia.".Length..]);

            // The namespace is the folder path or one of its ancestors: the public types of Controls/DiffEditor/ live in LoomEditor.<Host>.Controls.
            string expected = folder.Replace('/', '.');
            Assert.True(expected == suffix || expected.StartsWith(suffix + ".", StringComparison.Ordinal),
                $"{file}: namespace '{namespaces[0]}' does not match the folder '{folder}'.");
        }
    }

    [Fact]
    public void HostSharedFiles_UseNoFrameworkNamespace()
    {
        foreach (string file in SharedFiles())
        {
            Match match = FrameworkNamespace().Match(File.ReadAllText(file));
            Assert.False(match.Success, $"{file}: shared files must not name a framework namespace ('{match.Value}').");
        }
    }

    [Theory]
    [InlineData("LoomEditor.WinUI", "LOOM_WINUI")]
    [InlineData("LoomEditor.Avalonia", "LOOM_AVALONIA")]
    public void HostProjects_ImportHostSharedProps(string project, string symbol)
    {
        var document = RepositoryLayout.SourceProjects[project];
        Assert.Contains(document.Descendants("Import"),
            import => import.Attribute("Project")!.Value.Replace('\\', '/').EndsWith("LoomEditor.HostShared/LoomEditor.HostShared.props", StringComparison.Ordinal));
        Assert.Contains(document.Descendants("DefineConstants"), constants => constants.Value.Contains(symbol, StringComparison.Ordinal));
    }

    [Fact]
    public void HeaderBlockChecker_RejectsAStatementInsideTheBlock()
    {
        string[] lines = [.. ExampleHeader.Take(2), "var x = 1;", .. ExampleHeader.Skip(2), "public class X { }"];
        Assert.False(HasValidHeaderBlock(lines));
        Assert.False(HasValidHeaderBlock(["public class X { }", .. ExampleHeader]));
    }

    [Fact]
    public void HeaderBlockChecker_AcceptsTheReadmeExample()
    {
        Assert.True(HasValidHeaderBlock(["using LoomEditor.View.DiffEditor;", .. ExampleHeader, "", "public class X { }"]));
    }

    // The block runs from '#if !LOOM_AVALONIA' to '#endif', before the first type; it holds only usings, namespace lines and the #elif line.
    private static bool HasValidHeaderBlock(string[] lines)
    {
        string opener = Opener;
        int start = Array.FindIndex(lines, line => line.Trim() == opener);
        if (start < 0 || Array.FindLastIndex(lines, line => line.Trim() == opener) != start)
        {
            return false;
        }

        int end = Array.FindIndex(lines, start, line => line.Trim() == "#endif");
        if (end < 0)
        {
            return false;
        }

        bool typeBeforeBlock = lines.Take(start).Any(line => TypeDeclaration().IsMatch(line));
        bool onlyAllowed = lines.Skip(start).Take(end - start + 1).All(line =>
        {
            string trimmed = line.Trim();
            return trimmed.StartsWith("using ", StringComparison.Ordinal)
                || trimmed.StartsWith("namespace ", StringComparison.Ordinal)
                || trimmed is "#elif LOOM_AVALONIA" or "#endif" || trimmed == opener;
        });
        return !typeBeforeBlock && onlyAllowed;
    }
}
