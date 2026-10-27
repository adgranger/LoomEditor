using System.Text;
using System.Text.Json;
using LoomEditor.Core.Diff;

namespace LoomEditor.Core.Tests.Diff.TestHelpers;

/// <summary>Turns a <see cref="LinesDiff"/> and Monaco's <c>advanced.expected.diff.json</c> into the same flat list of <c>path = value</c> entries.</summary>
internal static class DiffFixtureFormat
{
    /// <summary>Monaco: <c>formatRange</c>.</summary>
    public static string FormatRange(Range range, string[] lines)
    {
        bool toLastChar = range.EndColumn == lines[range.EndLineNumber - 1].Length + 1;
        return $"[{range.StartLineNumber},{range.StartColumn} -> {range.EndLineNumber},{range.EndColumn}{(toLastChar ? " EOL" : "")}]";
    }

    public static List<string> Flatten(LinesDiff diff, string[] original, string[] modified)
    {
        List<string> result = [];
        AddDiffs(result, "diffs", diff.Changes, original, modified);
        for (int i = 0; i < diff.Moves.Count; i++)
        {
            MovedText move = diff.Moves[i];
            result.Add($"moves[{i}].originalRange = {move.LineRangeMapping.Original}");
            result.Add($"moves[{i}].modifiedRange = {move.LineRangeMapping.Modified}");
            AddDiffs(result, $"moves[{i}].changes", move.Changes, original, modified);
        }

        return result;
    }

    public static List<string> Flatten(string expectedJson)
    {
        using JsonDocument doc = JsonDocument.Parse(expectedJson);
        List<string> result = [];
        AddDiffs(result, "diffs", doc.RootElement.GetProperty("diffs"));
        if (doc.RootElement.TryGetProperty("moves", out JsonElement moves))
        {
            int i = 0;
            foreach (JsonElement move in moves.EnumerateArray())
            {
                result.Add($"moves[{i}].originalRange = {move.GetProperty("originalRange").GetString()}");
                result.Add($"moves[{i}].modifiedRange = {move.GetProperty("modifiedRange").GetString()}");
                AddDiffs(result, $"moves[{i}].changes", move.GetProperty("changes"));
                i++;
            }
        }

        return result;
    }

    /// <summary>The first difference between two flat lists, or null when they are equal.</summary>
    public static string? FirstDifference(List<string> expected, List<string> actual)
    {
        for (int i = 0; i < Math.Max(expected.Count, actual.Count); i++)
        {
            string? e = i < expected.Count ? expected[i] : null;
            string? a = i < actual.Count ? actual[i] : null;
            if (e != a)
            {
                return $"entry {i}: expected <{e ?? "(missing)"}> but was <{a ?? "(missing)"}>";
            }
        }

        return null;
    }

    /// <summary>Monaco: <c>assertDiffCorrectness</c>: applying all inner changes to the original gives the modified text.</summary>
    public static string ApplyInnerChanges(LinesDiff diff, string[] original, string[] modified)
    {
        string originalText = string.Join('\n', original);
        string modifiedText = string.Join('\n', modified);
        StringBuilder sb = new();
        int last = 0;
        foreach (RangeMapping m in diff.Changes.SelectMany(c => c.InnerChanges!))
        {
            int start = Offset(original, m.OriginalRange.StartLineNumber, m.OriginalRange.StartColumn);
            int end = Offset(original, m.OriginalRange.EndLineNumber, m.OriginalRange.EndColumn);
            int mStart = Offset(modified, m.ModifiedRange.StartLineNumber, m.ModifiedRange.StartColumn);
            int mEnd = Offset(modified, m.ModifiedRange.EndLineNumber, m.ModifiedRange.EndColumn);
            sb.Append(originalText, last, start - last).Append(modifiedText, mStart, mEnd - mStart);
            last = end;
        }

        return sb.Append(originalText, last, originalText.Length - last).ToString();
    }

    private static int Offset(string[] lines, int line, int column)
    {
        int offset = 0;
        for (int i = 0; i < line - 1; i++)
        {
            offset += lines[i].Length + 1;
        }

        return offset + column - 1;
    }

    private static void AddDiffs(List<string> result, string path, IReadOnlyList<DetailedLineRangeMapping> changes, string[] original, string[] modified)
    {
        for (int i = 0; i < changes.Count; i++)
        {
            DetailedLineRangeMapping c = changes[i];
            RangeMapping.AssertSorted(c.InnerChanges ?? []);
            result.Add($"{path}[{i}].originalRange = {c.Original}");
            result.Add($"{path}[{i}].modifiedRange = {c.Modified}");
            if (c.InnerChanges is null)
            {
                result.Add($"{path}[{i}].innerChanges = null");
                continue;
            }

            for (int j = 0; j < c.InnerChanges.Count; j++)
            {
                result.Add($"{path}[{i}].innerChanges[{j}].originalRange = {FormatRange(c.InnerChanges[j].OriginalRange, original)}");
                result.Add($"{path}[{i}].innerChanges[{j}].modifiedRange = {FormatRange(c.InnerChanges[j].ModifiedRange, modified)}");
            }
        }
    }

    private static void AddDiffs(List<string> result, string path, JsonElement diffs)
    {
        int i = 0;
        foreach (JsonElement d in diffs.EnumerateArray())
        {
            result.Add($"{path}[{i}].originalRange = {d.GetProperty("originalRange").GetString()}");
            result.Add($"{path}[{i}].modifiedRange = {d.GetProperty("modifiedRange").GetString()}");
            JsonElement inner = d.GetProperty("innerChanges");
            if (inner.ValueKind == JsonValueKind.Null)
            {
                result.Add($"{path}[{i}].innerChanges = null");
            }
            else
            {
                int j = 0;
                foreach (JsonElement c in inner.EnumerateArray())
                {
                    result.Add($"{path}[{i}].innerChanges[{j}].originalRange = {c.GetProperty("originalRange").GetString()}");
                    result.Add($"{path}[{i}].innerChanges[{j}].modifiedRange = {c.GetProperty("modifiedRange").GetString()}");
                    j++;
                }
            }

            i++;
        }
    }
}
