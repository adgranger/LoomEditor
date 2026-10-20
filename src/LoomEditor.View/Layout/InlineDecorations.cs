// Ported from microsoft/vscode: src/vs/editor/common/viewModel/inlineDecorations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Layout;

/// <summary>How an inline decoration is drawn (Monaco: <c>InlineDecorationType</c>; <c>WidthOnly</c> is for injected text, not ported).</summary>
internal enum InlineDecorationType
{
    Regular = 0,
    Before = 1,
    After = 2,
    RegularAffectingLetterSpacing = 3,
}

/// <summary>A class name applied to a range of a line (Monaco: <c>InlineDecoration</c>).</summary>
/// <remarks>Threading: immutable.</remarks>
internal sealed record InlineDecoration(Range Range, string InlineClassName, InlineDecorationType Type);

/// <summary>
/// The decorations in a range of lines (Monaco: <c>IViewDecorationsCollection</c>): the decorations ungrouped, the
/// inline decorations one list per line of the range, and <paramref name="HasVariableFonts"/> (always <c>false</c>:
/// font decorations are not ported).
/// </summary>
internal sealed record ViewDecorationsCollection(
    IReadOnlyList<ViewModelDecoration> Decorations,
    IReadOnlyList<IReadOnlyList<InlineDecoration>> InlineDecorations,
    IReadOnlyList<bool> HasVariableFonts);

/// <summary>
/// Turns the model decorations of a view range into view decorations and inline decorations (Monaco:
/// <c>InlineModelDecorationsComputer</c>; the injected text computer is not ported).
/// </summary>
/// <remarks>
/// The context interface of Monaco is a delegate here, without the minimap and margin arguments (minimap: phase 6;
/// margin-only queries: glyph margin lanes, not ported). Threading: UI thread only.
/// </remarks>
internal sealed class InlineModelDecorationsComputer(
    Func<Range, IReadOnlyList<ModelDecoration>> getModelDecorations,
    TextModel model,
    ICoordinatesConverter coordinatesConverter)
{
    private Dictionary<string, ViewModelDecoration> _decorationsCache = [];

    /// <summary>The inline decorations of a model line, split by view line.</summary>
    public IReadOnlyList<IReadOnlyList<InlineDecoration>> GetInlineDecorations(int modelLineNumber)
    {
        var modelRange = new Range(modelLineNumber, 1, modelLineNumber, model.GetLineMaxColumn(modelLineNumber));
        Range viewRange = coordinatesConverter.ConvertModelRangeToViewRange(modelRange);
        return GetDecorations(viewRange).InlineDecorations;
    }

    public ViewDecorationsCollection GetDecorations(Range viewRange)
    {
        IReadOnlyList<ModelDecoration> modelDecorations = getModelDecorations(viewRange);
        int startLineNumber = viewRange.StartLineNumber;
        int endLineNumber = viewRange.EndLineNumber;

        var decorationsInViewport = new List<ViewModelDecoration>();
        var inlineDecorations = new List<InlineDecoration>[endLineNumber - startLineNumber + 1];
        var hasVariableFonts = new bool[inlineDecorations.Length];
        for (int j = 0; j < inlineDecorations.Length; j++)
        {
            inlineDecorations[j] = [];
        }

        foreach (ModelDecoration modelDecoration in modelDecorations)
        {
            ModelDecorationOptions decorationOptions = modelDecoration.Options;

            if (!ModelDecorationVisibility.IsVisible(model, modelDecoration))
            {
                continue;
            }

            ViewModelDecoration viewModelDecoration = GetOrCreateViewModelDecoration(modelDecoration);
            Range decorationViewRange = viewModelDecoration.Range;

            decorationsInViewport.Add(viewModelDecoration);

            if (decorationOptions.InlineClassName is { } inlineClassName)
            {
                var inlineDecoration = new InlineDecoration(
                    decorationViewRange,
                    inlineClassName,
                    decorationOptions.InlineClassNameAffectsLetterSpacing ? InlineDecorationType.RegularAffectingLetterSpacing : InlineDecorationType.Regular);
                int intersectedStartLineNumber = Math.Max(startLineNumber, decorationViewRange.StartLineNumber);
                int intersectedEndLineNumber = Math.Min(endLineNumber, decorationViewRange.EndLineNumber);
                for (int j = intersectedStartLineNumber; j <= intersectedEndLineNumber; j++)
                {
                    inlineDecorations[j - startLineNumber].Add(inlineDecoration);
                }
            }

            if (decorationOptions.BeforeContentClassName is { } beforeContentClassName
                && startLineNumber <= decorationViewRange.StartLineNumber
                && decorationViewRange.StartLineNumber <= endLineNumber)
            {
                var inlineDecoration = new InlineDecoration(
                    new Range(decorationViewRange.StartLineNumber, decorationViewRange.StartColumn, decorationViewRange.StartLineNumber, decorationViewRange.StartColumn),
                    beforeContentClassName,
                    InlineDecorationType.Before);
                inlineDecorations[decorationViewRange.StartLineNumber - startLineNumber].Add(inlineDecoration);
            }

            if (decorationOptions.AfterContentClassName is { } afterContentClassName
                && startLineNumber <= decorationViewRange.EndLineNumber
                && decorationViewRange.EndLineNumber <= endLineNumber)
            {
                var inlineDecoration = new InlineDecoration(
                    new Range(decorationViewRange.EndLineNumber, decorationViewRange.EndColumn, decorationViewRange.EndLineNumber, decorationViewRange.EndColumn),
                    afterContentClassName,
                    InlineDecorationType.After);
                inlineDecorations[decorationViewRange.EndLineNumber - startLineNumber].Add(inlineDecoration);
            }
        }

        return new ViewDecorationsCollection(decorationsInViewport, inlineDecorations, hasVariableFonts);
    }

    public void Reset() => _decorationsCache = [];

    public void OnModelDecorationsChanged() => Reset();

    public void OnLineMappingChanged() => Reset();

    private ViewModelDecoration GetOrCreateViewModelDecoration(ModelDecoration modelDecoration)
    {
        if (_decorationsCache.TryGetValue(modelDecoration.Id, out ViewModelDecoration? cached))
        {
            return cached;
        }

        Range modelRange = modelDecoration.Range;
        ModelDecorationOptions options = modelDecoration.Options;
        Range viewRange;
        if (options.IsWholeLine)
        {
            Position start = coordinatesConverter.ConvertModelPositionToViewPosition(
                new Position(modelRange.StartLineNumber, 1), PositionAffinity.Left, allowZeroLineNumber: false, belowHiddenRanges: true);
            Position end = coordinatesConverter.ConvertModelPositionToViewPosition(
                new Position(modelRange.EndLineNumber, model.GetLineMaxColumn(modelRange.EndLineNumber)), PositionAffinity.Right);
            viewRange = new Range(start.LineNumber, start.Column, end.LineNumber, end.Column);
        }
        else
        {
            // For backwards compatibility reasons, we want injected text before any decoration.
            // Thus, move decorations to the right.
            viewRange = coordinatesConverter.ConvertModelRangeToViewRange(modelRange, PositionAffinity.Right);
        }

        var viewModelDecoration = new ViewModelDecoration(viewRange, options);
        _decorationsCache[modelDecoration.Id] = viewModelDecoration;
        return viewModelDecoration;
    }
}
