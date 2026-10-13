// Ported from microsoft/vscode: src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Model.PieceTree;
using LoomEditor.Core.Tests.Model.PieceTree.TestHelpers;

namespace LoomEditor.Core.Tests.Model.PieceTree;

// Monaco tests applyEdits through TextModel (editableTextModel.test.ts); these tests exercise the buffer directly.
public class PieceTreeTextBufferApplyEditsTests
{
    private static ValidAnnotatedEditOperation Op(Range range, string? text) =>
        new(null, range, text, false, false, false);

    private static string Text(PieceTreeTextBuffer buffer) =>
        buffer.GetValueInRange(new Range(1, 1, buffer.GetLineCount(), buffer.GetLineMaxColumn(buffer.GetLineCount())));

    [Fact]
    public void ApplyEdits_SingleInsert_ReturnsChangeAndReverse()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["abc\ndef"]);

        ApplyEditsResult result = buffer.ApplyEdits([Op(new Range(1, 2, 1, 2), "XY")], false, true);

        Assert.Equal("aXYbc\ndef", Text(buffer));
        InternalModelContentChange change = Assert.Single(result.Changes);
        Assert.Equal(new ModelContentChange(new Range(1, 2, 1, 2), 1, 0, "XY"), change.Change);
        ValidEditOperation reverse = Assert.Single(result.ReverseEdits!);
        Assert.Equal(new Range(1, 2, 1, 4), reverse.Range);
        Assert.Equal(string.Empty, reverse.Text);
    }

    [Fact]
    public void ApplyEdits_SingleDelete_ReturnsChangeAndReverse()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["abc\ndef"]);

        ApplyEditsResult result = buffer.ApplyEdits([Op(new Range(1, 3, 2, 2), null)], false, true);

        Assert.Equal("abef", Text(buffer));
        Assert.Equal(new ModelContentChange(new Range(1, 3, 2, 2), 2, 3, string.Empty), Assert.Single(result.Changes).Change);
        ValidEditOperation reverse = Assert.Single(result.ReverseEdits!);
        Assert.Equal(new Range(1, 3, 1, 3), reverse.Range);
        Assert.Equal("c\nd", reverse.Text);
    }

    [Fact]
    public void ApplyEdits_SingleReplace_ReturnsChangeAndReverse()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["abc\ndef"]);

        ApplyEditsResult result = buffer.ApplyEdits([Op(new Range(1, 2, 2, 3), "1\n2\n3")], false, true);

        Assert.Equal("a1\n2\n3f", Text(buffer));
        Assert.Equal(new ModelContentChange(new Range(1, 2, 2, 3), 1, 5, "1\n2\n3"), Assert.Single(result.Changes).Change);
        ValidEditOperation reverse = Assert.Single(result.ReverseEdits!);
        Assert.Equal(new Range(1, 2, 3, 2), reverse.Range);
        Assert.Equal("bc\nde", reverse.Text);
    }

    [Fact]
    public void ApplyEdits_TextWithOtherEol_IsNormalizedToBufferEol()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["a\nb"]);

        ApplyEditsResult result = buffer.ApplyEdits([Op(new Range(1, 2, 1, 2), "x\r\ny\rz")], false, false);

        Assert.Equal("ax\ny\nz\nb", Text(buffer));
        Assert.Equal("x\ny\nz", Assert.Single(result.Changes).Change.Text);
    }

    [Fact]
    public void ApplyEdits_ComputeUndoEditsFalse_ReverseEditsIsNull()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["abc"]);

        ApplyEditsResult result = buffer.ApplyEdits([Op(new Range(1, 1, 1, 2), "z")], false, false);

        Assert.Null(result.ReverseEdits);
        Assert.Equal("zbc", Text(buffer));
    }

    [Fact]
    public void ApplyEdits_OverlappingEdits_Throws()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["abcdef"]);

        var ex = Assert.Throws<ArgumentException>(() => buffer.ApplyEdits(
            [Op(new Range(1, 1, 1, 4), "x"), Op(new Range(1, 3, 1, 5), "y")], false, true));

        Assert.Contains("Overlapping ranges are not allowed!", ex.Message);
        Assert.Equal("abcdef", Text(buffer));
    }

    [Fact]
    public void ApplyEdits_TouchingEdits_AreAllowed()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["abcdef"]);

        ApplyEditsResult result = buffer.ApplyEdits([Op(new Range(1, 3, 1, 5), "Y"), Op(new Range(1, 1, 1, 3), "X")], false, true);

        Assert.Equal("XYef", Text(buffer));
        Assert.Equal(2, result.Changes.Count);
    }

    [Fact]
    public void ApplyEdits_NoOp_ProducesNoChange()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["abc"]);

        ApplyEditsResult result = buffer.ApplyEdits([Op(new Range(1, 2, 1, 2), null)], false, true);

        Assert.Empty(result.Changes);
        Assert.Equal("abc", Text(buffer));
    }

    [Fact]
    public void ApplyEdits_InsertRtlText_SetsMightContainRtlAndNonBasicAscii()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["abc"]);
        Assert.False(buffer.MightContainRtl());
        Assert.False(buffer.MightContainNonBasicAscii());

        buffer.ApplyEdits([Op(new Range(1, 1, 1, 1), "א")], false, false);

        Assert.True(buffer.MightContainRtl());
        Assert.True(buffer.MightContainNonBasicAscii());
    }

    [Fact]
    public void ApplyEdits_InsertNonAsciiText_SetsOnlyNonBasicAscii()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["abc"]);

        buffer.ApplyEdits([Op(new Range(1, 1, 1, 1), "é")], false, false);

        Assert.False(buffer.MightContainRtl());
        Assert.True(buffer.MightContainNonBasicAscii());
    }

    [Fact]
    public void ApplyEdits_AutoWhitespaceInsert_RecordsTrimCandidates()
    {
        PieceTreeTextBuffer buffer = TestTextBuffers.Create(["a\n"]);
        var op = new ValidAnnotatedEditOperation(null, new Range(2, 1, 2, 1), "    ", false, true, false);

        ApplyEditsResult result = buffer.ApplyEdits([op], true, false);

        Assert.Equal("a\n    ", Text(buffer));
        Assert.Equal([2], result.TrimAutoWhitespaceLineNumbers ?? []);

        // A line that gets content is not a candidate any more.
        PieceTreeTextBuffer buffer2 = TestTextBuffers.Create(["a\n"]);
        var op2 = new ValidAnnotatedEditOperation(null, new Range(2, 1, 2, 1), "  x\n  ", false, true, false);
        ApplyEditsResult result2 = buffer2.ApplyEdits([op2], true, false);
        Assert.Equal([3], result2.TrimAutoWhitespaceLineNumbers ?? []);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void ApplyEdits_RandomBatches_MatchNaiveStringAndReverseRestoresOriginal(int seed)
    {
        var random = new Random(seed);
        for (int iteration = 0; iteration < 200; iteration++)
        {
            string original = RandomLfText(random, random.Next(0, 60));
            PieceTreeTextBuffer buffer = TestTextBuffers.Create([original]);
            string context = $"seed {seed}, iteration {iteration}, text {original.Replace("\n", "\\n")}";

            // Non-overlapping offset edits, ascending.
            var edits = new List<(int Start, int End, string Text)>();
            int cursor = 0;
            while (cursor <= original.Length && edits.Count < 6 && random.Next(4) != 0)
            {
                int start = cursor + random.Next(0, Math.Min(5, original.Length - cursor) + 1);
                int end = start + random.Next(0, Math.Min(4, original.Length - start) + 1);
                string text = random.Next(3) == 0 ? string.Empty : RandomLfText(random, random.Next(1, 5));
                edits.Add((start, end, text));
                // Never two edits starting at the same offset after an empty range: their order would be ambiguous.
                cursor = end == start ? end + 1 : end;
            }

            string expected = original;
            for (int i = edits.Count - 1; i >= 0; i--)
            {
                expected = expected[..edits[i].Start] + edits[i].Text + expected[edits[i].End..];
            }

            var shuffled = edits.OrderBy(_ => random.Next()).ToList();
            var ops = shuffled
                .Select(e => Op(buffer.GetRangeAt(e.Start, e.End - e.Start), e.Text))
                .ToList();

            ApplyEditsResult result = buffer.ApplyEdits(ops, false, true);

            Assert.True(expected == Text(buffer), context);
            Assert.True(result.ReverseEdits is not null, context);
            Assert.Equal(edits.Count(e => e.Start != e.End || e.Text.Length > 0), result.Changes.Count);

            buffer.ApplyEdits(result.ReverseEdits!.Select(r => new ValidAnnotatedEditOperation(r.Identifier, r.Range, r.Text, false, false, false)).ToList(), false, false);
            Assert.True(original == Text(buffer), context);
        }
    }

    private static string RandomLfText(Random random, int length)
    {
        const string Alphabet = "ab\n c";
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = Alphabet[random.Next(Alphabet.Length)];
        }

        return new string(chars);
    }
}
