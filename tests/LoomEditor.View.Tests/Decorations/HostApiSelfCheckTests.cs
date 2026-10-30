using LoomEditor.Gallery;

namespace LoomEditor.View.Tests.Decorations;

// Runs the checks of the Gallery Native AOT self-check (samples/LoomEditor.Gallery/HostApiSelfCheck.cs, linked into this project) in the normal test run.
public class HostApiSelfCheckTests
{
    private static readonly IReadOnlyList<HostApiCheckResult> Results = HostApiSelfCheck.Run();

    public static TheoryData<string> CheckNames => new(HostApiSelfCheck.Names);

    [Theory]
    [MemberData(nameof(CheckNames))]
    public void Check_Passes(string name)
    {
        HostApiCheckResult result = Assert.Single(Results, r => r.Name == name);

        Assert.True(result.Passed, result.Detail);
    }

    [Fact]
    public void Run_ReportsEveryCheck()
    {
        Assert.Equal(HostApiSelfCheck.Names, Results.Select(r => r.Name));
    }

    [Fact]
    public void FormatReport_HasTheHeaderOneLinePerCheckAndTheSummary()
    {
        string[] lines = HostApiSelfCheck.FormatReport(Results).TrimEnd('\n').Split('\n');

        Assert.Equal("LoomEditor host API self-check", lines[0]);
        Assert.Equal(Results.Count + 2, lines.Length);
        Assert.All(lines[1..^1], line => Assert.StartsWith("PASS ", line, StringComparison.Ordinal));
        Assert.Equal($"{Results.Count} of {Results.Count} checks passed", lines[^1]);
    }
}
