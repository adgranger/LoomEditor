// Ported from microsoft/vscode: src/vs/editor/contrib/folding/test/browser/foldingModel.test.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;

namespace LoomEditor.View.Tests.Features.Folding.TestHelpers;

internal enum ExpectedDecorationType
{
    Hidden,
    Collapsed,
    Expanded,
}

internal readonly record struct ExpectedDecoration(int Line, ExpectedDecorationType Type);

/// <summary>Monaco: <c>TestDecorationProvider</c>.</summary>
internal sealed class TestDecorationProvider(TextModel model) : LoomEditor.View.Features.Folding.IDecorationProvider
{
    private static readonly ModelDecorationOptions CollapsedDecoration = Register();

    private static readonly ModelDecorationOptions ExpandedDecoration = Register();

    private static readonly ModelDecorationOptions HiddenDecoration = Register();

    private static ModelDecorationOptions Register() => ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "test",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        LinesDecorationsClassName = "folding",
    });

    public ModelDecorationOptions GetDecorationOption(bool isCollapsed, bool isHidden, bool isManual) =>
        isHidden ? HiddenDecoration : isCollapsed ? CollapsedDecoration : ExpandedDecoration;

    public T ChangeDecorations<T>(Func<IModelDecorationsChangeAccessor, T> callback) => model.ChangeDecorations(0, callback);

    public void RemoveDecorations(IReadOnlyList<string> decorationIds) =>
        model.ChangeDecorations(0, accessor => accessor.DeltaDecorations(decorationIds, []));

    public List<ExpectedDecoration> GetDecorations()
    {
        var res = new List<ExpectedDecoration>();
        foreach (ModelDecoration decoration in model.GetAllDecorations())
        {
            if (ReferenceEquals(decoration.Options, HiddenDecoration))
            {
                res.Add(new ExpectedDecoration(decoration.Range.StartLineNumber, ExpectedDecorationType.Hidden));
            }
            else if (ReferenceEquals(decoration.Options, CollapsedDecoration))
            {
                res.Add(new ExpectedDecoration(decoration.Range.StartLineNumber, ExpectedDecorationType.Collapsed));
            }
            else if (ReferenceEquals(decoration.Options, ExpandedDecoration))
            {
                res.Add(new ExpectedDecoration(decoration.Range.StartLineNumber, ExpectedDecorationType.Expanded));
            }
        }

        return res;
    }
}
