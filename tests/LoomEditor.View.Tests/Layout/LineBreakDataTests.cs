// Ported from microsoft/vscode: src/vs/editor/test/common/viewModel/lineBreakData.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Tests.Layout;

public class LineBreakDataTests
{
    private static int[] Sequence(int length) => [.. Enumerable.Range(0, length)];

    private static void TestInverse(ModelLineProjectionData data)
    {
        for (int i = 0; i < 100; i++)
        {
            OutputPosition output = data.TranslateToOutputPosition(i);
            Assert.Equal(i, data.TranslateToInputOffset(output.OutputLineIndex, output.OutputOffset));
        }
    }

    private static int[] GetInputOffsets(ModelLineProjectionData data, int outputLineIdx) =>
        [.. Sequence(20).Select(i => data.TranslateToInputOffset(outputLineIdx, i))];

    private static string[] GetOutputOffsets(ModelLineProjectionData data, PositionAffinity affinity) =>
        [.. Sequence(25).Select(i => data.TranslateToOutputPosition(i, affinity).ToString())];

    private static ModelDecorationInjectedTextOptions[] MapTextToInjectedTextOptions(params string[] arr) =>
        [.. arr.Select(e => ModelDecorationInjectedTextOptions.From(new InjectedTextOptions { Content = e }))];

    // Monaco: 'Basic'
    [Fact]
    public void Basic()
    {
        var data = new ModelLineProjectionData([], [], [100], [0], 10);

        Assert.Equal(50, data.TranslateToInputOffset(0, 50));
        Assert.Equal(150, data.TranslateToInputOffset(1, 60));
    }

    public class InjectedText1
    {
        private static readonly ModelLineProjectionData Data = new([2, 3, 10], MapTextToInjectedTextOptions("1", "22", "333"), [10, 100], [], 10);

        // Monaco: 'Injected Text 1' > 'getInputOffsetOfOutputPosition'
        [Fact]
        public void GetInputOffsetOfOutputPosition()
        {
            Assert.Equal([0, 1, 2, 2, 3, 3, 3, 4, 5, 6, 7, 8, 9, 10, 10, 10, 10, 11, 12, 13], GetInputOffsets(Data, 0));
            Assert.Equal([7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 9, 10, 10, 10, 10, 11, 12, 13], GetInputOffsets(Data, 1));
        }

        // Monaco: 'Injected Text 1' > 'getOutputPositionOfInputOffset'
        [Fact]
        public void GetOutputPositionOfInputOffset()
        {
            Data.TranslateToOutputPosition(20);
            Assert.Equal(
                [
                    "0:0", "0:1", "0:2", "0:4", "0:7", "0:8", "0:9", "1:10", "1:11", "1:12", "1:13", "1:17", "1:18",
                    "1:19", "1:20", "1:21", "1:22", "1:23", "1:24", "1:25", "1:26", "1:27", "1:28", "1:29", "1:30",
                ],
                GetOutputOffsets(Data, PositionAffinity.None));
            Assert.Equal(
                [
                    "0:0", "0:1", "0:2", "0:4", "0:7", "0:8", "0:9", "0:10", "1:11", "1:12", "1:13", "1:17", "1:18",
                    "1:19", "1:20", "1:21", "1:22", "1:23", "1:24", "1:25", "1:26", "1:27", "1:28", "1:29", "1:30",
                ],
                GetOutputOffsets(Data, PositionAffinity.Left));
            Assert.Equal(
                [
                    "0:0", "0:1", "0:3", "0:6", "0:7", "0:8", "0:9", "1:10", "1:11", "1:12", "1:16", "1:17", "1:18",
                    "1:19", "1:20", "1:21", "1:22", "1:23", "1:24", "1:25", "1:26", "1:27", "1:28", "1:29", "1:30",
                ],
                GetOutputOffsets(Data, PositionAffinity.Right));
        }

        // Monaco: 'Injected Text 1' > 'normalization'
        [Fact]
        public void Normalization()
        {
            Assert.Equal(
                [
                    "1:0", "1:1", "1:2", "1:3", "1:4", "1:5", "1:6", "1:7", "1:8", "1:9", "1:10", "1:11", "1:12",
                    "1:16", "1:16", "1:16", "1:16", "1:17", "1:18", "1:19", "1:20", "1:21", "1:22", "1:23", "1:24",
                ],
                Sequence(25).Select(v => Data.NormalizeOutputPosition(1, v, PositionAffinity.Right).ToString()));
        }

        // Monaco: 'Injected Text 1' > 'getInputOffsetOfOutputPosition is inverse of getOutputPositionOfInputOffset'
        [Fact]
        public void GetInputOffsetOfOutputPosition_IsInverseOfGetOutputPositionOfInputOffset() => TestInverse(Data);
    }

    public class InjectedText2
    {
        private static readonly ModelLineProjectionData Data = new([2, 2, 6], MapTextToInjectedTextOptions("1", "22", "333"), [10, 100], [], 0);

        // Monaco: 'Injected Text 2' > 'getInputOffsetOfOutputPosition'
        [Fact]
        public void GetInputOffsetOfOutputPosition()
        {
            Assert.Equal([0, 1, 2, 2, 2, 2, 3, 4, 5, 6, 6, 6, 6, 7, 8, 9, 10, 11, 12, 13], GetInputOffsets(Data, 0));
            Assert.Equal([6, 6, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23], GetInputOffsets(Data, 1));
        }

        // Monaco: 'Injected Text 2' > 'getInputOffsetOfOutputPosition is inverse of getOutputPositionOfInputOffset'
        [Fact]
        public void GetInputOffsetOfOutputPosition_IsInverseOfGetOutputPositionOfInputOffset() => TestInverse(Data);
    }

    public class InjectedText3
    {
        private static readonly ModelLineProjectionData Data = new([2, 2, 7], MapTextToInjectedTextOptions("1", "22", "333"), [10, 100], [], 0);

        // Monaco: 'Injected Text 3' > 'getInputOffsetOfOutputPosition'
        [Fact]
        public void GetInputOffsetOfOutputPosition()
        {
            Assert.Equal([0, 1, 2, 2, 2, 2, 3, 4, 5, 6, 7, 7, 7, 7, 8, 9, 10, 11, 12, 13], GetInputOffsets(Data, 0));
            Assert.Equal([7, 7, 7, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23], GetInputOffsets(Data, 1));
        }

        // Monaco: 'Injected Text 3' > 'getInputOffsetOfOutputPosition is inverse of getOutputPositionOfInputOffset'
        [Fact]
        public void GetInputOffsetOfOutputPosition_IsInverseOfGetOutputPositionOfInputOffset() => TestInverse(Data);
    }
}
