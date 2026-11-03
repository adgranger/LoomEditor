using LoomEditor.Architecture.Tests.TestHelpers;

namespace LoomEditor.Architecture.Tests;

/// <summary>Finds code copied between the WinUI and Avalonia hosts and between the two Galleries (phase 17, same rule as SonarCloud's copy detection).</summary>
public class HostTwinDuplicationTests
{
    private static List<CopiedBlock> FindHostBlocks() =>
    [
        .. CopyDetector.FindBlocks(Files("src/LoomEditor.Avalonia", skipHostFiles: false), Files("src/LoomEditor.WinUI", skipHostFiles: false)),
        .. CopyDetector.FindBlocks(Files("samples/LoomEditor.Gallery.Avalonia", skipHostFiles: true), Files("samples/LoomEditor.Gallery", skipHostFiles: true)),
    ];

    private static List<TokenizedFile> Files(string folder, bool skipHostFiles)
    {
        string root = Path.Combine(RepositoryLayout.Root.FullName, folder);
        string separator = Path.DirectorySeparatorChar.ToString();
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains(separator + "obj" + separator) && !file.Contains(separator + "bin" + separator))
            .Where(file => !skipHostFiles || !file.EndsWith(".Host.cs", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(file => CopyDetector.Tokenize(Path.GetRelativePath(RepositoryLayout.Root.FullName, file).Replace('\\', '/'), File.ReadAllText(file)))
            .ToList();
    }

    [Fact]
    public void HostTwins_HaveNoCopiedBlock()
    {
        string[] blocks = FindHostBlocks().Select(block => block.ToString()).ToArray();

        Assert.True(blocks.Length == 0,
            "Code copied between the hosts (move it to src/LoomEditor.HostShared):" + Environment.NewLine + string.Join(Environment.NewLine, blocks));
    }

    [Fact]
    public void Tokenize_NormalizesLiteralsAndSkipsCommentsAndUsings()
    {
        const string Source = """
            using System;
            using static System.Math;
            #pragma warning disable
            // comment
            /* block
               comment */
            var s = $"a{b}" + @"x""y" + "z"; /// doc
            var n = 0x33 + 0.7 + 1_000 + 2d; char c = 'q'; int @name = a >> b;
            using var f = Open();
            """;

        TokenizedFile file = CopyDetector.Tokenize("a.cs", Source);

        Assert.Equal(
            "var s = $str + $str + $str ; var n = $num + $num + $num + $num ; char c = $char ; int name = a > > b ; using var f = Open ( ) ;",
            string.Join(' ', file.Tokens));
        Assert.Equal(7, file.Lines[0]);
    }

    [Fact]
    public void FindBlocks_ReportsA100TokenRun()
    {
        TokenizedFile left = CopyDetector.Tokenize("l.cs", Run(100, "a"));
        TokenizedFile right = CopyDetector.Tokenize("r.cs", "x y\n" + Run(100, "a"));

        CopiedBlock block = Assert.Single(CopyDetector.FindBlocks([left], [right]));
        Assert.Equal("l.cs:1-1 <-> r.cs:2-2", block.ToString());
    }

    [Fact]
    public void FindBlocks_IgnoresA99TokenRun()
    {
        TokenizedFile left = CopyDetector.Tokenize("l.cs", Run(99, "a"));
        TokenizedFile right = CopyDetector.Tokenize("r.cs", Run(99, "a"));

        Assert.Empty(CopyDetector.FindBlocks([left], [right]));
    }

    // "count" distinct identifier tokens on one line.
    private static string Run(int count, string prefix) => string.Join(' ', Enumerable.Range(0, count).Select(i => prefix + i));
}
