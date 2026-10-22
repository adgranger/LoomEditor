// Ported from microsoft/vscode: src/vs/editor/common/viewModel/modelLineProjection.ts (IModelLineProjection, createModelLineProjection, IdentityModelLineProjection, HiddenModelLineProjection)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;

namespace LoomEditor.View.Layout;

/// <summary>How one model line is shown as view lines (Monaco: <c>IModelLineProjection</c>).</summary>
/// <remarks>
/// Phase 6 cut point: <c>baseViewLineNumber</c> is dropped from <c>getViewLineData</c> and <c>getViewLinesData</c>
/// (it only feeds the inline decorations of injected text). <c>model</c> is the <see cref="TextModel"/>: there is no <c>ISimpleModel</c>.
/// Threading: implementations are stateless singletons; UI thread only.
/// </remarks>
internal interface IModelLineProjection
{
    bool IsVisible { get; }

    /// <summary>Invalidates the current instance (potentially reuses and returns it again).</summary>
    IModelLineProjection SetVisible(bool isVisible);

    /// <summary>The line-break and injected text data of the line; <c>null</c> for the identity and hidden projections.</summary>
    ModelLineProjectionData? GetProjectionData();

    int GetViewLineCount();

    string GetViewLineContent(TextModel model, int modelLineNumber, int outputLineIndex);

    int GetViewLineLength(TextModel model, int modelLineNumber, int outputLineIndex);

    int GetViewLineMinColumn(TextModel model, int modelLineNumber, int outputLineIndex);

    int GetViewLineMaxColumn(TextModel model, int modelLineNumber, int outputLineIndex);

    bool GetViewLineContinuesWithWrappedLine(int outputLineIndex);

    ViewLineData GetViewLineData(TextModel model, int modelLineNumber, int outputLineIndex);

    /// <summary>Fills <paramref name="result"/> at <paramref name="globalStartIndex"/> (and the following indexes) with the view lines that are <paramref name="needed"/>; the others get <c>null</c>.</summary>
    void GetViewLinesData(TextModel model, int modelLineNumber, int outputLineIdx, int lineCount, int globalStartIndex, bool[] needed, ViewLineData?[] result);

    int GetModelColumnOfViewPosition(int outputLineIndex, int outputColumn);

    Position GetViewPositionOfModelPosition(int deltaLineNumber, int inputColumn, PositionAffinity affinity = PositionAffinity.Left);

    int GetViewLineNumberOfModelPosition(int deltaLineNumber, int inputColumn);

    Position NormalizePosition(int outputLineIndex, Position outputPosition, PositionAffinity affinity);

    /// <summary>The injected text at a view column of an output line, if any.</summary>
    InjectedText? GetInjectedTextAt(int outputLineIndex, int outputColumn);
}

/// <summary>Creates the projection of a model line (Monaco: <c>createModelLineProjection</c>).</summary>
/// <remarks>Threading: stateless.</remarks>
internal static class ModelLineProjection
{
    public static IModelLineProjection Create(bool isVisible) =>
        // No mapping needed
        isVisible ? IdentityModelLineProjection.Instance : HiddenModelLineProjection.Instance;

    /// <summary>The projection of a line with line-break data (wrapped lines, injected text); <c>null</c> data gives the identity or hidden projection.</summary>
    public static IModelLineProjection Create(ModelLineProjectionData? lineBreakData, bool isVisible) =>
        lineBreakData is null ? Create(isVisible) : new WrappedModelLineProjection(lineBreakData, isVisible);
}

/// <summary>This projection does not change the model line.</summary>
/// <remarks>Threading: stateless singleton.</remarks>
internal sealed class IdentityModelLineProjection : IModelLineProjection
{
    public static readonly IdentityModelLineProjection Instance = new();

    private IdentityModelLineProjection()
    {
    }

    public bool IsVisible => true;

    public IModelLineProjection SetVisible(bool isVisible) => isVisible ? this : HiddenModelLineProjection.Instance;

    public ModelLineProjectionData? GetProjectionData() => null;

    public int GetViewLineCount() => 1;

    public string GetViewLineContent(TextModel model, int modelLineNumber, int outputLineIndex) => model.GetLineContent(modelLineNumber);

    public int GetViewLineLength(TextModel model, int modelLineNumber, int outputLineIndex) => model.GetLineLength(modelLineNumber);

    public int GetViewLineMinColumn(TextModel model, int modelLineNumber, int outputLineIndex) => model.GetLineMinColumn(modelLineNumber);

    public int GetViewLineMaxColumn(TextModel model, int modelLineNumber, int outputLineIndex) => model.GetLineMaxColumn(modelLineNumber);

    public bool GetViewLineContinuesWithWrappedLine(int outputLineIndex) => false;

    public ViewLineData GetViewLineData(TextModel model, int modelLineNumber, int outputLineIndex)
    {
        LineTokens lineTokens = model.Tokenization.GetLineTokens(modelLineNumber);
        string lineContent = lineTokens.GetLineContent();
        return new ViewLineData(lineContent, false, 1, lineContent.Length + 1, 0, lineTokens.Inflate());
    }

    public void GetViewLinesData(TextModel model, int modelLineNumber, int outputLineIdx, int lineCount, int globalStartIndex, bool[] needed, ViewLineData?[] result)
    {
        result[globalStartIndex] = needed[globalStartIndex] ? GetViewLineData(model, modelLineNumber, 0) : null;
    }

    public int GetModelColumnOfViewPosition(int outputLineIndex, int outputColumn) => outputColumn;

    public Position GetViewPositionOfModelPosition(int deltaLineNumber, int inputColumn, PositionAffinity affinity = PositionAffinity.Left) =>
        new(deltaLineNumber, inputColumn);

    public int GetViewLineNumberOfModelPosition(int deltaLineNumber, int inputColumn) => deltaLineNumber;

    public Position NormalizePosition(int outputLineIndex, Position outputPosition, PositionAffinity affinity) => outputPosition;

    public InjectedText? GetInjectedTextAt(int outputLineIndex, int outputColumn) => null;
}

/// <summary>This projection hides the model line.</summary>
/// <remarks>Behavior difference: it throws <see cref="InvalidOperationException"/> where Monaco throws <c>Error('Not supported')</c>. Threading: stateless singleton.</remarks>
internal sealed class HiddenModelLineProjection : IModelLineProjection
{
    public static readonly HiddenModelLineProjection Instance = new();

    private HiddenModelLineProjection()
    {
    }

    private static InvalidOperationException NotSupported() => new("Not supported");

    public bool IsVisible => false;

    public IModelLineProjection SetVisible(bool isVisible) => isVisible ? IdentityModelLineProjection.Instance : this;

    public ModelLineProjectionData? GetProjectionData() => null;

    public int GetViewLineCount() => 0;

    public string GetViewLineContent(TextModel model, int modelLineNumber, int outputLineIndex) => throw NotSupported();

    public int GetViewLineLength(TextModel model, int modelLineNumber, int outputLineIndex) => throw NotSupported();

    public int GetViewLineMinColumn(TextModel model, int modelLineNumber, int outputLineIndex) => throw NotSupported();

    public int GetViewLineMaxColumn(TextModel model, int modelLineNumber, int outputLineIndex) => throw NotSupported();

    public bool GetViewLineContinuesWithWrappedLine(int outputLineIndex) => throw NotSupported();

    public ViewLineData GetViewLineData(TextModel model, int modelLineNumber, int outputLineIndex) => throw NotSupported();

    public void GetViewLinesData(TextModel model, int modelLineNumber, int outputLineIdx, int lineCount, int globalStartIndex, bool[] needed, ViewLineData?[] result) =>
        throw NotSupported();

    public int GetModelColumnOfViewPosition(int outputLineIndex, int outputColumn) => throw NotSupported();

    public Position GetViewPositionOfModelPosition(int deltaLineNumber, int inputColumn, PositionAffinity affinity = PositionAffinity.Left) => throw NotSupported();

    public int GetViewLineNumberOfModelPosition(int deltaLineNumber, int inputColumn) => throw NotSupported();

    public Position NormalizePosition(int outputLineIndex, Position outputPosition, PositionAffinity affinity) => throw NotSupported();

    public InjectedText? GetInjectedTextAt(int outputLineIndex, int outputColumn) => throw NotSupported();
}
