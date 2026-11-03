using LoomEditor.Architecture.Tests.TestHelpers;

namespace LoomEditor.Architecture.Tests;

/// <summary>Finds code copied between the WinUI and Avalonia hosts and between the two Galleries (phase 17, same rule as SonarCloud's copy detection).</summary>
public class HostTwinDuplicationTests
{
    // One "<Avalonia path> <-> <WinUI path>" per pair still copied (the last three of src/ are not reported by Sonar); each phase 17 card deletes its pairs.
    private static readonly HashSet<string> KnownDuplicates = new(StringComparer.Ordinal)
    {
        "src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.cs <-> src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.cs",
        "src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Theme.cs <-> src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Theme.cs",
        "src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Header.cs <-> src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Header.cs",
        "src/LoomEditor.Avalonia/Controls/Comments/CommentThreadWidget.Composer.cs <-> src/LoomEditor.WinUI/Controls/Comments/CommentThreadWidget.Composer.cs",
        "src/LoomEditor.Avalonia/Controls/Comments/CommentComposerToolbar.cs <-> src/LoomEditor.WinUI/Controls/Comments/CommentComposerToolbar.cs",
        "src/LoomEditor.Avalonia/Controls/Comments/SuggestionBlockView.cs <-> src/LoomEditor.WinUI/Controls/Comments/SuggestionBlockView.cs",
        "src/LoomEditor.Avalonia/Controls/Hover/HoverWidget.cs <-> src/LoomEditor.WinUI/Controls/Hover/HoverWidget.cs",
        "src/LoomEditor.Avalonia/Controls/Suggest/SuggestWidget.cs <-> src/LoomEditor.WinUI/Controls/Suggest/SuggestWidget.cs",
        "src/LoomEditor.Avalonia/Controls/Suggest/SuggestWidget.Rows.cs <-> src/LoomEditor.WinUI/Controls/Suggest/SuggestWidget.Rows.cs",
        "src/LoomEditor.Avalonia/Controls/LoomEditor.Pointer.Host.cs <-> src/LoomEditor.WinUI/Controls/LoomEditor.Pointer.Host.cs",
        "src/LoomEditor.Avalonia/Controls/Comments/CommentZoneHost.Host.cs <-> src/LoomEditor.WinUI/Controls/Comments/CommentZoneHost.Host.cs",
        "src/LoomEditor.Avalonia/Controls/DiffEditor/LoomDiffEditor.cs <-> src/LoomEditor.WinUI/Controls/DiffEditor/LoomDiffEditor.cs",
        "samples/LoomEditor.Gallery.Avalonia/MainWindow.axaml.cs <-> samples/LoomEditor.Gallery/MainWindow.xaml.cs",
    };

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
    public void HostTwins_HaveNoUnlistedCopiedBlock()
    {
        string[] unlisted = FindHostBlocks().Where(block => !KnownDuplicates.Contains(block.Pair)).Select(block => block.ToString()).ToArray();

        Assert.True(unlisted.Length == 0,
            "Code copied between the hosts (move it to src/LoomEditor.HostShared):" + Environment.NewLine + string.Join(Environment.NewLine, unlisted));
    }

    [Fact]
    public void KnownDuplicates_AreStillCopied()
    {
        HashSet<string> found = FindHostBlocks().Select(block => block.Pair).ToHashSet(StringComparer.Ordinal);

        foreach (string pair in KnownDuplicates)
        {
            Assert.True(found.Contains(pair), $"{pair} is no longer copied: delete it from KnownDuplicates");
        }
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
