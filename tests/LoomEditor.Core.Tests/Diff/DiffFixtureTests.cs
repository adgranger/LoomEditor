using LoomEditor.Core.Diff;
using LoomEditor.Core.Tests.Diff.TestHelpers;

namespace LoomEditor.Core.Tests.Diff;

public class DiffFixtureTests
{
    private const int ExpectedFixtureCount = 58;

    // folder -> reason; never skip silently. Expected to stay empty.
    private static readonly Dictionary<string, string> KnownDifferences = [];

    private static string FixturesDir => Path.Combine(AppContext.BaseDirectory, "Diff", "Fixtures");

    public static TheoryData<string> Folders()
    {
        TheoryData<string> data = [];
        foreach (string dir in Directory.GetDirectories(FixturesDir).OrderBy(d => d, StringComparer.Ordinal))
        {
            data.Add(Path.GetFileName(dir));
        }

        return data;
    }

    [Fact]
    public void Fixtures_AreAllPresent()
    {
        Assert.Equal(ExpectedFixtureCount, Directory.GetDirectories(FixturesDir).Length);
        Assert.Empty(KnownDifferences);
    }

    [Fact]
    public void FormatRange_ToLastChar_AddsEol()
    {
        string[] lines = ["abc", "de"];
        Assert.Equal("[1,2 -> 1,3]", DiffFixtureFormat.FormatRange(new Range(1, 2, 1, 3), lines));
        Assert.Equal("[1,2 -> 2,3 EOL]", DiffFixtureFormat.FormatRange(new Range(1, 2, 2, 3), lines));
    }

    // Monaco: 'diffing fixtures' > '<folder>-advanced'
    [Theory]
    [MemberData(nameof(Folders))]
    public void Fixture_MatchesMonacoAdvancedExpectedDiff(string folder)
    {
        if (KnownDifferences.TryGetValue(folder, out string? reason))
        {
            Assert.Fail("Known difference not asserted: " + reason);
        }

        string path = Path.Combine(FixturesDir, folder);
        string[] original = ReadLines(Directory.GetFiles(path, "1.*").Single());
        string[] modified = ReadLines(Directory.GetFiles(path, "2.*").Single());
        bool ignoreTrimWhitespace = folder.Contains("trimws", StringComparison.Ordinal);

        LinesDiff diff = LinesDiffComputers.GetDefault().ComputeDiff(
            original, modified, new LinesDiffComputerOptions(ignoreTrimWhitespace, int.MaxValue, ComputeMoves: true));

        if (!ignoreTrimWhitespace)
        {
            Assert.Equal(string.Join('\n', modified), DiffFixtureFormat.ApplyInnerChanges(diff, original, modified));
        }

        List<string> expected = DiffFixtureFormat.Flatten(File.ReadAllText(Path.Combine(path, "advanced.expected.diff.json")));
        string? difference = DiffFixtureFormat.FirstDifference(expected, DiffFixtureFormat.Flatten(diff, original, modified));
        Assert.True(difference is null, $"{folder}: {difference}");
    }

    private static string[] ReadLines(string file) =>
        File.ReadAllText(file).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
}
