// Ported from microsoft/vscode: src/vs/editor/common/viewModel/modelLineProjection.ts (ModelLineProjection)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

/// <summary>
/// The projection that wraps a model line and injects text into it (Monaco: class <c>ModelLineProjection</c>, renamed because
/// <see cref="ModelLineProjection"/> is the factory here).
/// </summary>
/// <remarks>
/// Behavior difference: it throws <see cref="InvalidOperationException"/> where Monaco throws <c>Error('Not supported')</c>.
/// Threading: UI thread only; the instance is owned by one slot of the view model lines.
/// </remarks>
internal sealed partial class WrappedModelLineProjection(ModelLineProjectionData projectionData, bool isVisible) : IModelLineProjection
{
    private static readonly List<string> Spaces = [""];

    private bool _isVisible = isVisible;

    public bool IsVisible => _isVisible;

    public IModelLineProjection SetVisible(bool isVisible)
    {
        _isVisible = isVisible;
        return this;
    }

    public ModelLineProjectionData? GetProjectionData() => projectionData;

    public int GetViewLineCount() => _isVisible ? projectionData.GetOutputLineCount() : 0;

    public string GetViewLineContent(TextModel model, int modelLineNumber, int outputLineIndex)
    {
        AssertVisible();

        int startOffsetInInputWithInjections = outputLineIndex > 0 ? projectionData.BreakOffsets[outputLineIndex - 1] : 0;
        int endOffsetInInputWithInjections = projectionData.BreakOffsets[outputLineIndex];

        string r;
        if (projectionData.InjectionOffsets is { } injectionOffsets)
        {
            var injectedTexts = new List<LineInjectedText>(injectionOffsets.Length);
            for (int idx = 0; idx < injectionOffsets.Length; idx++)
            {
                injectedTexts.Add(new LineInjectedText(0, 0, injectionOffsets[idx] + 1, projectionData.InjectionOptions![idx], 0));
            }

            string lineWithInjections = LineInjectedText.ApplyInjectedText(model.GetLineContent(modelLineNumber), injectedTexts);
            r = lineWithInjections[startOffsetInInputWithInjections..endOffsetInInputWithInjections];
        }
        else
        {
            r = model.GetValueInRange(new Range(modelLineNumber, startOffsetInInputWithInjections + 1, modelLineNumber, endOffsetInInputWithInjections + 1));
        }

        if (outputLineIndex > 0)
        {
            r = GetSpaces(projectionData.WrappedTextIndentLength) + r;
        }

        return r;
    }

    public int GetViewLineLength(TextModel model, int modelLineNumber, int outputLineIndex)
    {
        AssertVisible();
        return projectionData.GetLineLength(outputLineIndex);
    }

    public int GetViewLineMinColumn(TextModel model, int modelLineNumber, int outputLineIndex)
    {
        AssertVisible();
        return projectionData.GetMinOutputOffset(outputLineIndex) + 1;
    }

    public int GetViewLineMaxColumn(TextModel model, int modelLineNumber, int outputLineIndex)
    {
        AssertVisible();
        return projectionData.GetMaxOutputOffset(outputLineIndex) + 1;
    }

    public bool GetViewLineContinuesWithWrappedLine(int outputLineIndex) => outputLineIndex + 1 < GetViewLineCount();

    public ViewLineData GetViewLineData(TextModel model, int modelLineNumber, int outputLineIndex) => throw new NotImplementedException("P6-16");

    public void GetViewLinesData(TextModel model, int modelLineNumber, int outputLineIdx, int lineCount, int globalStartIndex, bool[] needed, ViewLineData?[] result) =>
        throw new NotImplementedException("P6-16");

    public int GetModelColumnOfViewPosition(int outputLineIndex, int outputColumn)
    {
        AssertVisible();
        return projectionData.TranslateToInputOffset(outputLineIndex, outputColumn - 1) + 1;
    }

    public Position GetViewPositionOfModelPosition(int deltaLineNumber, int inputColumn, PositionAffinity affinity = PositionAffinity.None)
    {
        AssertVisible();
        OutputPosition r = projectionData.TranslateToOutputPosition(inputColumn - 1, affinity);
        return r.ToPosition(deltaLineNumber);
    }

    public int GetViewLineNumberOfModelPosition(int deltaLineNumber, int inputColumn)
    {
        AssertVisible();
        OutputPosition r = projectionData.TranslateToOutputPosition(inputColumn - 1);
        return deltaLineNumber + r.OutputLineIndex;
    }

    public Position NormalizePosition(int outputLineIndex, Position outputPosition, PositionAffinity affinity)
    {
        int baseViewLineNumber = outputPosition.LineNumber - outputLineIndex;
        OutputPosition normalizedOutputPosition = projectionData.NormalizeOutputPosition(outputLineIndex, outputPosition.Column - 1, affinity);
        return normalizedOutputPosition.ToPosition(baseViewLineNumber);
    }

    public InjectedText? GetInjectedTextAt(int outputLineIndex, int outputColumn) => projectionData.GetInjectedText(outputLineIndex, outputColumn - 1);

    private void AssertVisible()
    {
        if (!_isVisible)
        {
            throw new InvalidOperationException("Not supported");
        }
    }

    private static string GetSpaces(int count)
    {
        while (Spaces.Count <= count)
        {
            Spaces.Add(new string(' ', Spaces.Count));
        }

        return Spaces[count];
    }
}
