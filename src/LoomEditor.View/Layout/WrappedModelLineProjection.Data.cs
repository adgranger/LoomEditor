// Ported from microsoft/vscode: src/vs/editor/common/viewModel/modelLineProjection.ts (ModelLineProjection)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Tokens;

namespace LoomEditor.View.Layout;

internal sealed partial class WrappedModelLineProjection
{
    /// <summary>Try using <see cref="GetViewLinesData"/> instead.</summary>
    public ViewLineData GetViewLineData(TextModel model, int modelLineNumber, int outputLineIndex, int baseViewLineNumber)
    {
        var arr = new ViewLineData?[1];
        GetViewLinesData(model, modelLineNumber, outputLineIndex, 1, baseViewLineNumber, 0, [true], arr);
        return arr[0]!;
    }

    public void GetViewLinesData(
        TextModel model, int modelLineNumber, int outputLineIdx, int lineCount, int baseViewLineNumber, int globalStartIndex, bool[] needed, ViewLineData?[] result)
    {
        AssertVisible();

        var context = new InjectedTextContext(projectionData, baseViewLineNumber);
        var computer = new InjectedTextInlineDecorationsComputer(context);
        IReadOnlyList<IReadOnlyList<InlineDecoration>> lineInlineDecorations = computer.GetInlineDecorations(modelLineNumber);
        LineTokens lineTokens = model.Tokenization.GetLineTokens(modelLineNumber);
        LineTokens lineWithInjections = LineTokensWithInjections.GetLineTokensWithInjections(lineTokens, projectionData.InjectionOptions, projectionData.InjectionOffsets);

        for (int outputLineIndex = outputLineIdx; outputLineIndex < outputLineIdx + lineCount; outputLineIndex++)
        {
            int globalIndex = globalStartIndex + outputLineIndex - outputLineIdx;
            if (!needed[globalIndex])
            {
                result[globalIndex] = null;
                continue;
            }

            result[globalIndex] = GetViewLineDataCore(
                lineWithInjections,
                outputLineIndex < lineInlineDecorations.Count ? lineInlineDecorations[outputLineIndex] : null,
                outputLineIndex);
        }
    }

    private ViewLineData GetViewLineDataCore(LineTokens lineWithInjections, IReadOnlyList<InlineDecoration>? inlineDecorations, int outputLineIndex)
    {
        AssertVisible();
        int deltaStartIndex = outputLineIndex > 0 ? projectionData.WrappedTextIndentLength : 0;

        int lineStartOffsetInInputWithInjections = outputLineIndex > 0 ? projectionData.BreakOffsets[outputLineIndex - 1] : 0;
        int lineEndOffsetInInputWithInjections = projectionData.BreakOffsets[outputLineIndex];
        IViewLineTokens tokens = lineWithInjections.SliceAndInflate(lineStartOffsetInInputWithInjections, lineEndOffsetInInputWithInjections, deltaStartIndex);

        string lineContent = tokens.GetLineContent();
        if (outputLineIndex > 0)
        {
            lineContent = GetSpaces(projectionData.WrappedTextIndentLength) + lineContent;
        }

        int minColumn = projectionData.GetMinOutputOffset(outputLineIndex) + 1;
        int maxColumn = lineContent.Length + 1;
        bool continuesWithWrappedLine = GetViewLineContinuesWithWrappedLine(outputLineIndex);
        // integral for monospace fonts with an integer full-width ratio
        int startVisibleColumn = outputLineIndex == 0 ? 0 : (int)projectionData.BreakOffsetsVisibleColumn[outputLineIndex - 1];

        return new ViewLineData(lineContent, continuesWithWrappedLine, minColumn, maxColumn, startVisibleColumn, tokens, inlineDecorations);
    }

    private sealed class InjectedTextContext(ModelLineProjectionData data, int baseViewLineNumber) : IInjectedTextInlineDecorationsComputerContext
    {
        public IReadOnlyList<ModelDecorationInjectedTextOptions>? GetInjectionOptions(int modelLineNumber) => data.InjectionOptions;

        public IReadOnlyList<int>? GetInjectionOffsets(int modelLineNumber) => data.InjectionOffsets;

        public IReadOnlyList<int> GetBreakOffsets(int modelLineNumber) => data.BreakOffsets;

        public int GetWrappedTextIndentLength(int modelLineNumber) => data.WrappedTextIndentLength;

        public int GetBaseViewLineNumber(int modelLineNumber) => baseViewLineNumber;
    }
}
