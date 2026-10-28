using LoomEditor.Core.Diff;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Diff;

public class RangeMappingTests
{
    // Monaco: 'Simple' (suite 'lineRangeMapping')
    [Fact]
    public void GetLineRangeMapping_Simple()
    {
        DetailedLineRangeMapping result = LineRangeMappingConversions.GetLineRangeMapping(
            new RangeMapping(new Range(2, 1, 3, 1), new Range(2, 1, 2, 1)),
            new ArrayText(["const abc = \"helloworld\".split(\"\");", "", ""]),
            new ArrayText(["const asciiLower = \"helloworld\".split(\"\");", ""]));

        Assert.Equal("{[2,3)->[2,2)}", result.ToString());
    }

    // Monaco: 'Empty Lines' (suite 'lineRangeMapping')
    [Fact]
    public void GetLineRangeMapping_EmptyLines()
    {
        DetailedLineRangeMapping result = LineRangeMappingConversions.GetLineRangeMapping(
            new RangeMapping(new Range(2, 1, 2, 1), new Range(2, 1, 4, 1)),
            new ArrayText(["", ""]),
            new ArrayText(["", "", "", ""]));

        Assert.Equal("{[2,2)->[2,4)}", result.ToString());
    }

    [Fact]
    public void ToString_RangeMapping_Format()
    {
        Assert.Equal("{[1,2 -> 1,3]->[4,5 -> 6,7]}", new RangeMapping(new Range(1, 2, 1, 3), new Range(4, 5, 6, 7)).ToString());
    }

    [Fact]
    public void Flip_SwapsOriginalAndModified()
    {
        var m = new RangeMapping(new Range(1, 1, 1, 2), new Range(3, 1, 3, 4));
        Assert.Equal(m.ModifiedRange, m.Flip().OriginalRange);
        Assert.Equal(m.OriginalRange, m.Flip().ModifiedRange);
        var l = new DetailedLineRangeMapping(new LineRange(1, 2), new LineRange(3, 5), [m]);
        Assert.Equal("{[3,5)->[1,2)}", l.Flip().ToString());
        Assert.Equal(m.ModifiedRange, l.Flip().InnerChanges![0].OriginalRange);
    }

    [Fact]
    public void Join_UnionOfBothSides()
    {
        RangeMapping joined = RangeMapping.Join([
            new RangeMapping(new Range(1, 1, 1, 2), new Range(1, 1, 1, 3)),
            new RangeMapping(new Range(4, 1, 4, 2), new Range(5, 1, 5, 3))]);
        Assert.Equal("{[1,1 -> 4,2]->[1,1 -> 5,3]}", joined.ToString());
    }

    [Fact]
    public void Join_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => RangeMapping.Join([]));
    }

    [Fact]
    public void RangeMapping_AssertSorted_Unsorted_Asserts()
    {
        RangeMapping a = new(new Range(1, 1, 1, 2), new Range(1, 1, 1, 2));
        RangeMapping b = new(new Range(2, 1, 2, 2), new Range(2, 1, 2, 2));
        RangeMapping.AssertSorted([a, b]);
#if DEBUG
        Assert.ThrowsAny<Exception>(() => RangeMapping.AssertSorted([b, a]));
#endif
    }

    [Fact]
    public void FromRangeMappings_JoinsLineRanges()
    {
        DetailedLineRangeMapping d = DetailedLineRangeMapping.FromRangeMappings([
            new RangeMapping(new Range(1, 1, 1, 2), new Range(1, 1, 1, 2)),
            new RangeMapping(new Range(3, 1, 3, 2), new Range(3, 1, 4, 2))]);
        Assert.Equal("{[1,4)->[1,5)}", d.ToString());
    }

    [Fact]
    public void LineRangeMappingFromRangeMappings_GroupsByLine()
    {
        var original = new ArrayText(["aaa", "bbb", "ccc", "ddd", "eee"]);
        var modified = new ArrayText(["aXa", "bYb", "ccc", "ddd", "eZe"]);
        List<DetailedLineRangeMapping> result = LineRangeMappingConversions.LineRangeMappingFromRangeMappings([
            new RangeMapping(new Range(1, 2, 1, 3), new Range(1, 2, 1, 3)),
            new RangeMapping(new Range(2, 2, 2, 3), new Range(2, 2, 2, 3)),
            new RangeMapping(new Range(5, 2, 5, 3), new Range(5, 2, 5, 3))], original, modified);

        Assert.Equal(["{[1,3)->[1,3)}", "{[5,6)->[5,6)}"], result.Select(r => r.ToString()));
        Assert.Equal(2, result[0].InnerChanges!.Count);
        Assert.Single(result[1].InnerChanges!);
    }

    [Fact]
    public void ToRangeMapping_NonEmpty_UsesInclusiveRange()
    {
        RangeMapping m = new LineRangeMapping(new LineRange(2, 4), new LineRange(2, 3)).ToRangeMapping();
        Assert.Equal(new Range(2, 1, 3, 1), m.OriginalRange.WithEnd(new Position(3, 1)));
        Assert.Equal(2, m.ModifiedRange.StartLineNumber);
    }

    [Fact]
    public void ToRangeMapping_BothStartAtOne_UsesLineStarts()
    {
        RangeMapping m = new LineRangeMapping(new LineRange(1, 1), new LineRange(1, 3)).ToRangeMapping();
        Assert.Equal(new Range(1, 1, 1, 1), m.OriginalRange);
        Assert.Equal(new Range(1, 1, 3, 1), m.ModifiedRange);
    }

    [Fact]
    public void ToRangeMapping_OneStartsAtOne_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new LineRangeMapping(new LineRange(1, 1), new LineRange(2, 2)).ToRangeMapping());
    }

    [Fact]
    public void ToRangeMapping_EmptyAfterFirstLine_UsesEndOfPreviousLine()
    {
        RangeMapping m = new LineRangeMapping(new LineRange(3, 3), new LineRange(3, 5)).ToRangeMapping();
        Assert.Equal(new Range(2, int.MaxValue, 2, int.MaxValue), m.OriginalRange);
        Assert.Equal(new Range(2, int.MaxValue, 4, int.MaxValue), m.ModifiedRange);
    }

    [Fact]
    public void ToRangeMapping2_ClampsToLineLengths()
    {
        string[] original = ["ab", "cd"];
        string[] modified = ["ab", "cdef"];
        RangeMapping m = new LineRangeMapping(new LineRange(2, 3), new LineRange(2, 3)).ToRangeMapping2(original, modified);
        Assert.Equal(new Range(2, 1, 2, 3), m.OriginalRange);
        Assert.Equal(new Range(2, 1, 2, 5), m.ModifiedRange);
    }

    [Fact]
    public void ToRangeMapping2_ValidEnd_UsesLineStarts()
    {
        string[] lines = ["a", "b", "c"];
        RangeMapping m = new LineRangeMapping(new LineRange(1, 2), new LineRange(1, 3)).ToRangeMapping2(lines, lines);
        Assert.Equal(new Range(1, 1, 2, 1), m.OriginalRange);
        Assert.Equal(new Range(1, 1, 3, 1), m.ModifiedRange);
    }

    [Fact]
    public void ToRangeMapping2_EmptyOriginal_UsesEndOfPreviousLines()
    {
        string[] original = ["a", "b"];
        string[] modified = ["a", "b", "c"];
        RangeMapping m = new LineRangeMapping(new LineRange(3, 3), new LineRange(3, 4)).ToRangeMapping2(original, modified);
        Assert.Equal(new Range(2, 2, 2, 2), m.OriginalRange);
        Assert.Equal(new Range(2, 2, 3, 2), m.ModifiedRange);
    }

    [Fact]
    public void WithInnerChangesFromLineRanges_AddsOneInnerChange()
    {
        DetailedLineRangeMapping d = new DetailedLineRangeMapping(new LineRange(2, 3), new LineRange(2, 4), null).WithInnerChangesFromLineRanges();
        Assert.Single(d.InnerChanges!);
    }

    [Fact]
    public void Flip_MovedText_SwapsMapping()
    {
        var moved = new MovedText(new LineRangeMapping(new LineRange(1, 2), new LineRange(5, 6)), []);
        Assert.Equal("{[5,6)->[1,2)}", moved.Flip().LineRangeMapping.ToString());
    }

    [Fact]
    public void ArrayText_GetValueOfRange_MultiLine()
    {
        var text = new ArrayText(["hello", "big", "world"]);
        Assert.Equal("llo\nbig\nwo", text.GetValueOfRange(new Range(1, 3, 3, 3)));
        Assert.Equal("ell", text.GetValueOfRange(new Range(1, 2, 1, 5)));
        Assert.Equal("hello\nbig\nworld", text.GetValue());
        Assert.Equal(["hello", "big", "world"], text.GetLines());
        Assert.Equal(5, text.GetLineLength(3));
        Assert.Equal("big", text.GetLineAt(2));
    }

    [Fact]
    public void ArrayText_Length()
    {
        var text = new ArrayText(["hello", "big", "world"]);
        Assert.Equal(new TextLength(2, 5), text.Length);
        Assert.Equal(new Position(3, 6), text.EndPositionExclusive);
        Assert.Equal(new LineRange(1, 3), text.LineRange);
    }
}
