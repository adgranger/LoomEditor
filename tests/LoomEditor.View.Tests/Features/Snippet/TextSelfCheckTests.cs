using LoomEditor.Gallery;

namespace LoomEditor.View.Tests.Features.Snippet;

// Runs the checks of the Gallery Native AOT self-check (samples/LoomEditor.Gallery/TextSelfCheck.cs, linked into this project) in the normal test run.
public class TextSelfCheckTests
{
    private static readonly IReadOnlyList<TextCheckResult> Results = TextSelfCheck.Run();

    public static TheoryData<string> CheckNames => new(TextSelfCheck.Names);

    [Theory]
    [MemberData(nameof(CheckNames))]
    public void Check_Passes(string name)
    {
        TextCheckResult result = Assert.Single(Results, r => r.Name == name);

        Assert.True(result.Passed, result.Detail);
    }

    [Fact]
    public void Run_ReportsEveryCheck()
    {
        Assert.Equal(TextSelfCheck.Names, Results.Select(r => r.Name));
    }

    [Fact]
    public void FormatReport_HasTheHeaderOneLinePerCheckAndTheSummary()
    {
        string[] lines = TextSelfCheck.FormatReport(Results).TrimEnd('\n').Split('\n');

        Assert.Equal("LoomEditor text self-check", lines[0]);
        Assert.Equal(Results.Count + 2, lines.Length);
        Assert.All(lines[1..^1], line => Assert.StartsWith("PASS ", line, StringComparison.Ordinal));
        Assert.Equal($"{Results.Count} of {Results.Count} checks passed", lines[^1]);
    }
}
