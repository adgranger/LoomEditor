using System.Text;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.BracketPairs.Tree;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.BracketPairs.Tree;

// Monaco: suite 'combineTextEditInfos'
public class CombineTextEditInfosTests
{
    public static TheoryData<int> Seeds()
    {
        var data = new TheoryData<int>();
        for (var seed = 0; seed < 50; seed++)
        {
            data.Add(seed);
        }

        return data;
    }

    // Monaco: 'combineTextEditInfos' > 'test' + seed (0…49)
    [Theory]
    [MemberData(nameof(Seeds))]
    public void Combine_RandomEdits_RoundTripsToTheOriginalText(int seed)
    {
        RunTest(seed);
    }

    [Fact]
    public void Combine_EmptyFirst_ReturnsSecond()
    {
        List<TextEditInfo> second = [new(Length.Of(0, 1), Length.Of(0, 2), Length.Of(0, 3))];

        Assert.Same(second, TextEditInfoCombiner.Combine([], second));
        Assert.Same(second, TextEditInfoCombiner.Combine(second, []));
    }

    [Fact]
    public void Combine_AdjacentEdits_AreMerged()
    {
        List<TextEditInfo> first = [new(Length.Of(0, 1), Length.Of(0, 2), Length.Of(0, 3))];
        List<TextEditInfo> second = [new(Length.Of(0, 4), Length.Of(0, 5), Length.Of(0, 1))];

        var combined = TextEditInfoCombiner.Combine(first, second);

        var edit = Assert.Single(combined);
        Assert.Equal(Length.Of(0, 1), edit.StartOffset);
        Assert.Equal(Length.Of(0, 3), edit.EndOffset);
        Assert.Equal(Length.Of(0, 4), edit.NewLength);
    }

    // Monaco: runTest
    private static void RunTest(int seed)
    {
        var rng = new Random(seed);
        var message = $"seed {seed}";

        const string Str = "abcde\nfghij\nklmno\npqrst\n";
        var textModelS0 = new TextModel(Str);

        var edits1 = GetRandomEditInfos(textModelS0, rng.Next(1, 4), rng);

        var textModelS1 = new TextModel(textModelS0.GetValue());
        textModelS1.ApplyEdits(ToEdits(edits1));

        var edits2 = GetRandomEditInfos(textModelS1, rng.Next(1, 4), rng);

        var textModelS2 = new TextModel(textModelS1.GetValue());
        textModelS2.ApplyEdits(ToEdits(edits2));

        var combinedEdits = TextEditInfoCombiner.Combine(edits1, edits2);

        foreach (var edit in combinedEdits)
        {
            var range = new Range(
                edit.StartOffset.ToPosition(),
                Length.Add(edit.StartOffset, edit.NewLength).ToPosition());
            var value = textModelS2.GetValueInRange(range);
            foreach (var c in value)
            {
                if (c != 'L' && c != 'C' && c != '\n')
                {
                    throw new InvalidOperationException($"Invalid edit: {value} ({message})");
                }
            }

            textModelS2.ApplyEdits(
            [
                new TextEdit(
                    range,
                    textModelS0.GetValueInRange(new Range(edit.StartOffset.ToPosition(), edit.EndOffset.ToPosition()))),
            ]);
        }

        Assert.True(textModelS0.GetValue() == textModelS2.GetValue(), message);
    }

    // Monaco: getRandomEditInfos
    private static List<TextEditInfo> GetRandomEditInfos(TextModel textModel, int count, Random rng, bool disjoint = false)
    {
        var edits = new List<TextEditInfo>();
        var i = 0;
        for (var j = 0; j < count; j++)
        {
            edits.Add(GetRandomEdit(textModel, i, rng));
            i = textModel.GetOffsetAt(edits[j].EndOffset.ToPosition()) + (disjoint ? 1 : 0);
        }

        return edits;
    }

    // Monaco: getRandomEdit
    private static TextEditInfo GetRandomEdit(TextModel textModel, int rangeOffsetStart, Random rng)
    {
        var textModelLength = textModel.GetValueLength();
        var offsetStart = rng.Next(rangeOffsetStart, textModelLength);
        var offsetEnd = rng.Next(offsetStart, textModelLength);

        var lineCount = rng.Next(0, 3);
        var columnCount = rng.Next(0, 5);

        return new TextEditInfo(
            Length.FromPosition(textModel.GetPositionAt(offsetStart)),
            Length.FromPosition(textModel.GetPositionAt(offsetEnd)),
            Length.Of(lineCount, columnCount));
    }

    // Monaco: toEdit
    private static TextEdit[] ToEdits(List<TextEditInfo> editInfos)
    {
        var result = new TextEdit[editInfos.Count];
        for (var k = 0; k < editInfos.Count; k++)
        {
            var editInfo = editInfos[k];
            var l = editInfo.NewLength.ToObj();
            var text = new StringBuilder();
            for (var i = 0; i < l.LineCount; i++)
            {
                text.Append("LLL\n");
            }

            for (var i = 0; i < l.ColumnCount; i++)
            {
                text.Append('C');
            }

            result[k] = new TextEdit(
                new Range(editInfo.StartOffset.ToPosition(), editInfo.EndOffset.ToPosition()),
                text.ToString());
        }

        return result;
    }
}
