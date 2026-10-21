// Ported from microsoft/vscode: src/vs/editor/test/browser/viewModel/modelLineProjection.test.ts (suite 'Editor ViewModel - SplitLinesCollection': 'Invalid line numbers', 'issue #3662')
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;
using LoomEditor.View.Tests.TestHelpers;

namespace LoomEditor.View.Tests.Layout;

// Not ported: 'SplitLine', 'getViewLinesData - with wrapping' and 'getViewLinesData - with wrapping and injected text' (word wrap and
// injected text: phase 6).
public class ViewModelLinesConversionTests
{
    private static readonly string[] SixLines =
    [
        "int main() {",
        "\tprintf(\"Hello world!\");",
        "}",
        "int main() {",
        "\tprintf(\"Hello world!\");",
        "}",
    ];

    // Monaco: withSplitLinesCollection(text, callback)
    private static void WithSplitLinesCollection(string[] lines, Action<TextModel, ViewModelLinesFromProjectedModel> callback)
    {
        TextModel model = TestCodeEditor.CreateTextModel(string.Join('\n', lines));
        using var linesCollection = new ViewModelLinesFromProjectedModel(1, model, model.Options.TabSize);
        callback(model, linesCollection);
    }

    // Monaco: 'Invalid line numbers'
    [Fact]
    public void InvalidLineNumbers() => WithSplitLinesCollection(SixLines, (_, linesCollection) =>
    {
        Assert.Equal(6, linesCollection.GetViewLineCount());

        // Phase 6 cut point: the getViewLinesIndentGuides assertions are not ported (indent guides).

        // getOutputLineContent
        string[] expectedContent = [SixLines[0], SixLines[0], SixLines[0], SixLines[1], SixLines[2], SixLines[3], SixLines[4], SixLines[5], SixLines[5]];
        for (int i = 0; i < expectedContent.Length; i++)
        {
            Assert.Equal(expectedContent[i], linesCollection.GetViewLineContent(i - 1));
        }

        // getOutputLineMinColumn
        for (int line = -1; line <= 7; line++)
        {
            Assert.Equal(1, linesCollection.GetViewLineMinColumn(line));
        }

        // getOutputLineMaxColumn
        int[] expectedMaxColumn = [13, 13, 13, 25, 2, 13, 25, 2, 2];
        for (int i = 0; i < expectedMaxColumn.Length; i++)
        {
            Assert.Equal(expectedMaxColumn[i], linesCollection.GetViewLineMaxColumn(i - 1));
        }

        // convertOutputPositionToInputPosition
        int[] expectedModelLine = [1, 1, 1, 2, 3, 4, 5, 6, 6, 6];
        for (int i = 0; i < expectedModelLine.Length; i++)
        {
            Assert.Equal(new Position(expectedModelLine[i], 1), linesCollection.ConvertViewPositionToModelPosition(i - 1, 1));
        }
    });

    // Monaco: 'issue #3662'
    [Fact]
    public void Issue3662() => WithSplitLinesCollection(SixLines, (model, linesCollection) =>
    {
        linesCollection.SetHiddenAreas([new Range(1, 1, 3, 1), new Range(5, 1, 6, 1)]);

        int viewLineCount = linesCollection.GetViewLineCount();
        Assert.Equal(1, viewLineCount);

        int modelLineCount = model.LineCount;
        for (int lineNumber = 0; lineNumber <= modelLineCount + 1; lineNumber++)
        {
            int lineMinColumn = lineNumber >= 1 && lineNumber <= modelLineCount ? model.GetLineMinColumn(lineNumber) : 1;
            int lineMaxColumn = lineNumber >= 1 && lineNumber <= modelLineCount ? model.GetLineMaxColumn(lineNumber) : 1;
            for (int column = lineMinColumn - 1; column <= lineMaxColumn + 1; column++)
            {
                Position viewPosition = linesCollection.ConvertModelPositionToViewPosition(lineNumber, column);

                // validate view position
                int viewLineNumber = viewPosition.LineNumber;
                int viewColumn = viewPosition.Column;
                if (viewLineNumber < 1)
                {
                    viewLineNumber = 1;
                }

                int lineCount = linesCollection.GetViewLineCount();
                if (viewLineNumber > lineCount)
                {
                    viewLineNumber = lineCount;
                }

                int viewMinColumn = linesCollection.GetViewLineMinColumn(viewLineNumber);
                int viewMaxColumn = linesCollection.GetViewLineMaxColumn(viewLineNumber);
                if (viewColumn < viewMinColumn)
                {
                    viewColumn = viewMinColumn;
                }

                if (viewColumn > viewMaxColumn)
                {
                    viewColumn = viewMaxColumn;
                }

                Assert.True(viewPosition == new Position(viewLineNumber, viewColumn), $"model->view for {lineNumber}, {column}");
            }
        }

        for (int lineNumber = 0; lineNumber <= viewLineCount + 1; lineNumber++)
        {
            int lineMinColumn = linesCollection.GetViewLineMinColumn(lineNumber);
            int lineMaxColumn = linesCollection.GetViewLineMaxColumn(lineNumber);
            for (int column = lineMinColumn - 1; column <= lineMaxColumn + 1; column++)
            {
                Position modelPosition = linesCollection.ConvertViewPositionToModelPosition(lineNumber, column);
                Position validModelPosition = model.ValidatePosition(modelPosition);
                Assert.True(modelPosition == validModelPosition, $"view->model for {lineNumber}, {column}");
            }
        }
    });

    [Fact]
    public void ConvertModelPositionToViewPosition_InHiddenArea_GoesToTheLineAbove() => WithSplitLinesCollection(SixLines, (_, lines) =>
    {
        lines.SetHiddenAreas([new Range(3, 1, 4, 1)]);

        // the end of the previous visible line (model line 2 is view line 2)
        Assert.Equal(new Position(2, 25), lines.ConvertModelPositionToViewPosition(4, 2));
    });

    [Fact]
    public void ConvertModelPositionToViewPosition_BelowHiddenRanges_GoesToTheLineBelow() => WithSplitLinesCollection(SixLines, (_, lines) =>
    {
        lines.SetHiddenAreas([new Range(3, 1, 4, 1)]);

        // column 1 of the next visible line (model line 5 is view line 3)
        Assert.Equal(new Position(3, 1), lines.ConvertModelPositionToViewPosition(4, 2, belowHiddenRanges: true));
    });

    [Fact]
    public void ConvertModelPositionToViewPosition_FirstLinesHidden_AllowZeroLineNumberIsIgnored_ReturnsLineOne() => WithSplitLinesCollection(SixLines, (_, lines) =>
    {
        lines.SetHiddenAreas([new Range(1, 1, 2, 1)]);

        // Monaco returns (0, 1) here; Position cannot hold line 0 (card P5-80, option A).
        Assert.Equal(new Position(1, 1), lines.ConvertModelPositionToViewPosition(1, 3, allowZeroLineNumber: true));
        Assert.Equal(new Position(1, 1), lines.ConvertModelPositionToViewPosition(1, 3));
    });

    [Fact]
    public void ValidateViewPosition_ViewPositionOfAnotherModelPosition_ConvertsTheExpectedPosition() => WithSplitLinesCollection(SixLines, (_, lines) =>
    {
        lines.SetHiddenAreas([new Range(2, 1, 3, 1)]);

        // view line 2 is model line 4
        Assert.Equal(new Position(2, 3), lines.ValidateViewPosition(2, 3, new Position(4, 3)));
        Assert.Equal(new Position(1, 1), lines.ValidateViewPosition(2, 3, new Position(1, 1)));
    });

    [Fact]
    public void GetDecorationsInRange_AcrossHiddenLines_SkipsTheHiddenDecorationsOnce() => WithSplitLinesCollection(SixLines, (model, lines) =>
    {
        const int OwnerId = 7;
        lines.SetHiddenAreas([new Range(2, 1, 3, 1)]);
        Range visibleOnly = new(1, 1, 1, 3);
        Range insideHidden = new(2, 1, 2, 3);
        Range spanning = new(1, 5, 4, 3);
        model.DeltaDecorations(
            OwnerId,
            [],
            [
                new ModelDeltaDecoration(spanning, ModelDecorationOptions.Empty),
                new ModelDeltaDecoration(insideHidden, ModelDecorationOptions.Empty),
                new ModelDeltaDecoration(visibleOnly, ModelDecorationOptions.Empty),
            ]);

        IReadOnlyList<ModelDecoration> decorations = lines.GetDecorationsInRange(new Range(1, 1, 4, 3), OwnerId, false, false);

        Assert.Equal([visibleOnly, spanning], decorations.Select(d => d.Range));
    });

    [Fact]
    public void CoordinatesConverter_ForwardsToTheLines() => WithSplitLinesCollection(SixLines, (_, lines) =>
    {
        lines.SetHiddenAreas([new Range(2, 1, 3, 1)]);
        ICoordinatesConverter converter = lines.CreateCoordinatesConverter();

        Assert.True(converter.ModelPositionIsVisible(new Position(1, 1)));
        Assert.False(converter.ModelPositionIsVisible(new Position(2, 1)));
        Assert.Equal(1, converter.GetModelLineViewLineCount(1));
        Assert.Equal(0, converter.GetModelLineViewLineCount(2));
        Assert.Equal(2, converter.GetViewLineNumberOfModelPosition(4, 1));
        Assert.Equal(1, converter.GetViewLineNumberOfModelPosition(3, 1));
        Assert.Equal(new Position(2, 5), converter.ConvertModelPositionToViewPosition(new Position(4, 5)));
        Assert.Equal(new Position(4, 5), converter.ConvertViewPositionToModelPosition(new Position(2, 5)));
        Assert.Equal(new Range(2, 1, 3, 2), converter.ConvertModelRangeToViewRange(new Range(4, 1, 5, 2)));
    });
}
