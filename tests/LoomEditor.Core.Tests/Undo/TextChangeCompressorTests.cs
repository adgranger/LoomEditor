using System.Text;
using LoomEditor.Core.Undo;

namespace LoomEditor.Core.Tests.Undo;

// Monaco has no test file for textChange.ts (it is tested through undo); these are LoomEditor additions.
public class TextChangeCompressorTests
{
    [Fact]
    public void Compress_NullPrevious_ReturnsCurrentList()
    {
        var curr = new List<TextChange> { new(0, "", 0, "a") };

        Assert.Same(curr, TextChangeCompressor.CompressConsecutiveTextChanges(null, curr));
    }

    [Fact]
    public void Compress_EmptyPrevious_ReturnsCurrentList()
    {
        var curr = new List<TextChange> { new(0, "", 0, "a") };

        Assert.Same(curr, TextChangeCompressor.CompressConsecutiveTextChanges([], curr));
    }

    [Fact]
    public void Compress_TypingOneHundredCharacters_ProducesOneChange()
    {
        IReadOnlyList<TextChange>? compressed = null;
        for (var i = 0; i < 100; i++)
        {
            IReadOnlyList<TextChange> typed = [new TextChange(i, "", i, "x")];
            compressed = TextChangeCompressor.CompressConsecutiveTextChanges(compressed, typed);
        }

        var change = Assert.Single(compressed!);
        Assert.Equal(0, change.OldPosition);
        Assert.Equal("", change.OldText);
        Assert.Equal(new string('x', 100), change.NewText);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(42)]
    [InlineData(1234)]
    public void Compress_RandomBatches_MatchesApplyingBothBatches(int seed)
    {
        var random = new Random(seed);
        for (var iteration = 0; iteration < 500; iteration++)
        {
            var s0 = RandomString(random, random.Next(0, 30));
            var a = RandomBatch(random, s0);
            var s1 = Apply(s0, a);
            var b = RandomBatch(random, s1);
            var s2 = Apply(s1, b);

            var compressed = TextChangeCompressor.CompressConsecutiveTextChanges(a, b);

            Assert.True(
                s2 == Apply(s0, compressed),
                $"seed={seed} iteration={iteration} s0='{s0}' A=[{string.Join(", ", a)}] B=[{string.Join(", ", b)}] "
                + $"compressed=[{string.Join(", ", compressed)}]");
        }
    }

    private static string RandomString(Random random, int length)
    {
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            sb.Append((char)('a' + random.Next(0, 6)));
        }

        return sb.ToString();
    }

    // Sorted, non-overlapping, non-touching changes; old offsets are relative to `text`, new offsets to the result.
    private static List<TextChange> RandomBatch(Random random, string text)
    {
        var result = new List<TextChange>();
        var cursor = 0;
        var delta = 0;
        while (cursor <= text.Length && random.Next(0, 4) != 0)
        {
            var start = random.Next(cursor, text.Length + 1);
            var end = random.Next(start, Math.Min(text.Length, start + 5) + 1);
            var oldText = text[start..end];
            var newText = RandomString(random, random.Next(0, 5));
            result.Add(new TextChange(start, oldText, start + delta, newText));
            delta += newText.Length - oldText.Length;
            cursor = end + 1;
        }

        return result;
    }

    // Applies changes (sorted, non-overlapping, offsets on the old text) to `text`.
    private static string Apply(string text, IReadOnlyList<TextChange> changes)
    {
        var sb = new StringBuilder();
        var cursor = 0;
        foreach (var change in changes)
        {
            sb.Append(text, cursor, change.OldPosition - cursor);
            Assert.Equal(change.OldText, text.Substring(change.OldPosition, change.OldLength));
            sb.Append(change.NewText);
            cursor = change.OldEnd;
        }

        sb.Append(text, cursor, text.Length - cursor);
        return sb.ToString();
    }
}
