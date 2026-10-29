using LoomEditor.Gallery;

namespace LoomEditor.View.Tests.Comments;

// Runs the checks of the Gallery Native AOT self-check (samples/LoomEditor.Gallery/CommentsSelfCheck.cs, linked into this project) in the normal test run.
public class CommentsSelfCheckTests
{
    private static readonly IReadOnlyList<CommentsCheckResult> Results = CommentsSelfCheck.Run();

    public static TheoryData<string> CheckNames => new(CommentsSelfCheck.Names);

    [Theory]
    [MemberData(nameof(CheckNames))]
    public void Check_Passes(string name)
    {
        CommentsCheckResult result = Assert.Single(Results, r => r.Name == name);

        Assert.True(result.Passed, result.Detail);
    }

    [Fact]
    public void Run_ReportsEveryCheck()
    {
        Assert.Equal(CommentsSelfCheck.Names, Results.Select(r => r.Name));
    }

    [Fact]
    public void FormatReport_HasTheHeaderOneLinePerCheckAndTheSummary()
    {
        string[] lines = CommentsSelfCheck.FormatReport(Results).TrimEnd('\n').Split('\n');

        Assert.Equal("LoomEditor comments self-check", lines[0]);
        Assert.Equal(Results.Count + 2, lines.Length);
        Assert.All(lines[1..^1], line => Assert.StartsWith("PASS ", line, StringComparison.Ordinal));
        Assert.Equal($"{Results.Count} of {Results.Count} checks passed", lines[^1]);
    }
}
