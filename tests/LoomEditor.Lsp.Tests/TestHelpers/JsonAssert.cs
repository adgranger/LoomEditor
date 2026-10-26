using System.Text.Json;

namespace LoomEditor.Lsp.Tests.TestHelpers;

/// <summary>Deep JSON comparison: object members in any order, numbers by value.</summary>
internal static class JsonAssert
{
    public static void Equal(string expectedJson, string actualJson)
    {
        using JsonDocument expected = JsonDocument.Parse(expectedJson);
        using JsonDocument actual = JsonDocument.Parse(actualJson);
        Assert.True(Same(expected.RootElement, actual.RootElement), $"Expected JSON: {expectedJson}{Environment.NewLine}Actual JSON:   {actualJson}");
    }

    private static bool Same(JsonElement a, JsonElement b)
    {
        if (a.ValueKind != b.ValueKind)
        {
            return false;
        }

        switch (a.ValueKind)
        {
            case JsonValueKind.Object:
                Dictionary<string, JsonElement> bm = b.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                int count = 0;
                foreach (JsonProperty p in a.EnumerateObject())
                {
                    count++;
                    if (!bm.TryGetValue(p.Name, out JsonElement other) || !Same(p.Value, other))
                    {
                        return false;
                    }
                }

                return count == bm.Count;
            case JsonValueKind.Array:
                JsonElement[] aa = a.EnumerateArray().ToArray();
                JsonElement[] ba = b.EnumerateArray().ToArray();
                return aa.Length == ba.Length && aa.Zip(ba).All(t => Same(t.First, t.Second));
            case JsonValueKind.Number:
                return a.GetDouble() == b.GetDouble();
            case JsonValueKind.String:
                return a.GetString() == b.GetString();
            default:
                return true;
        }
    }
}
