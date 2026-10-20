// Ported from microsoft/vscode: src/vs/workbench/services/textMate/test/browser/arrayOperation.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Tokens;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Tests.Model.Tokens;

public class ArrayOperationTests
{
    private static LengthEdit CreateEdit() => LengthEdit.Create(
    [
        LengthReplacement.Create(4, 7, 2),
        LengthReplacement.Create(8, 8, 2),
        LengthReplacement.Create(9, 11, 0),
    ]);

    // Monaco: 'array operation' > 'simple'
    [Fact]
    public void Simple()
    {
        LengthEdit edit = CreateEdit();

        string?[] arr = [.. Enumerable.Range(0, 15).Select(x => $"item{x}")];
        string?[] newArr = edit.ApplyArray(arr, null);
        Assert.Equal(
            ["item0", "item1", "item2", "item3", null, null, "item7", null, null, "item8", "item11", "item12", "item13", "item14"],
            newArr);

        var transformer = new MonotonousIndexTransformer(edit);
        string[] actual = [.. Enumerable.Range(0, 15).Select(x =>
        {
            int? t = transformer.Transform(x);
            string r = $"arr[{x}]: {arr[x]} -> ";
            return t is { } index ? r + $"newArr[{index}]: {newArr[index]}" : r + "undefined";
        })];
        Assert.Equal(
            [
                "arr[0]: item0 -> newArr[0]: item0",
                "arr[1]: item1 -> newArr[1]: item1",
                "arr[2]: item2 -> newArr[2]: item2",
                "arr[3]: item3 -> newArr[3]: item3",
                "arr[4]: item4 -> undefined",
                "arr[5]: item5 -> undefined",
                "arr[6]: item6 -> undefined",
                "arr[7]: item7 -> newArr[6]: item7",
                "arr[8]: item8 -> newArr[9]: item8",
                "arr[9]: item9 -> undefined",
                "arr[10]: item10 -> undefined",
                "arr[11]: item11 -> newArr[10]: item11",
                "arr[12]: item12 -> newArr[11]: item12",
                "arr[13]: item13 -> newArr[12]: item13",
                "arr[14]: item14 -> newArr[13]: item14",
            ],
            actual);
    }

    [Fact]
    public void MonotonousIndexTransformer_IndexInsideReplacedRange_IsUndefined()
    {
        var transformer = new MonotonousIndexTransformer(LengthEdit.Replace(new OffsetRange(2, 5), 1));

        Assert.Equal(1, transformer.Transform(1));
        Assert.Null(transformer.Transform(2));
        Assert.Null(transformer.Transform(4));
        Assert.Equal(3, transformer.Transform(5));
    }

    [Fact]
    public void MonotonousIndexTransformer_FromMany_AppliesTheTransformationsInOrder()
    {
        // 3 lines become 5, then line 0 is deleted: old line 1 -> 3, old line 0 -> undefined.
        IIndexTransformer transformer = MonotonousIndexTransformer.FromMany(
            [LengthEdit.Replace(new OffsetRange(2, 3), 3), LengthEdit.Delete(new OffsetRange(0, 1))]);

        Assert.Null(transformer.Transform(0));
        Assert.Equal(0, transformer.Transform(1));
    }

    [Fact]
    public void LengthEdit_ComposeAndInverse_BehaveLikeTheirParts()
    {
        LengthEdit first = LengthEdit.Insert(2, 3); // 10 items -> 13
        LengthEdit second = LengthEdit.Delete(new OffsetRange(0, 4)); // 13 items -> 9

        LengthEdit composed = first.Compose(second);

        Assert.Equal(-1, composed.GetLengthDelta());
        Assert.Equal(9, composed.GetNewDataLength(10));
        Assert.True(composed.Equals(LengthEdit.Replace(new OffsetRange(0, 2), 1)));
        Assert.True(first.Inverse().Equals(LengthEdit.Delete(new OffsetRange(2, 5))));
        Assert.Throws<ArgumentException>(() => LengthEdit.Create([LengthReplacement.Create(5, 8, 1), LengthReplacement.Create(6, 7, 1)]));
    }

    [Fact]
    public void LinesLengthEditFromModelContentChange_ExpandsToWholeLines()
    {
        LengthEdit edit = ModelContentChangeEdits.LinesLengthEditFromModelContentChange(
            [new ModelContentChange(new Range(2, 2, 3, 1), 0, 0, "a\nb\nc")]);

        // lines 2-3 (indices 1 and 2) become 3 lines
        Assert.True(edit.Equals(LengthEdit.Replace(new OffsetRange(1, 3), 3)));
        Assert.Equal(1, edit.GetLengthDelta());
    }
}
