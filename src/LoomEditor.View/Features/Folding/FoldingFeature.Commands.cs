// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/folding.ts (the FoldingAction classes)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using static LoomEditor.View.Features.Folding.FoldingModelFunctions;

namespace LoomEditor.View.Features.Folding;

// Not ported: editor.toggleImportFold (needs the imports range kind of syntax providers) and the FoldingArguments of executeCommand.
internal sealed partial class FoldingFeature
{
    private const int FoldLevelCount = 7;

    private readonly List<IDisposable> _registrations = [];

    private void RegisterCommands(IEditorContext context)
    {
        void Register(string id, Action<FoldingModel> invoke) =>
            _registrations.Add(context.Commands.Register(new EditorCommand(id, () => Run(invoke))));

        Register(FoldingCommands.Unfold, model => SetCollapseStateLevelsDown(model, false, 1, SelectedLineNumbers()));
        Register(FoldingCommands.UnfoldRecursively, model => SetCollapseStateLevelsDown(model, false, int.MaxValue, SelectedLineNumbers()));

        // fold the region at the location or, if already collapsed, the first uncollapsed parent instead
        Register(FoldingCommands.Fold, model => SetCollapseStateUp(model, true, SelectedLineNumbers()));
        Register(FoldingCommands.ToggleFold, model => ToggleCollapseState(model, 1, SelectedLineNumbers()));
        Register(FoldingCommands.FoldRecursively, model => SetCollapseStateLevelsDown(model, true, int.MaxValue, SelectedLineNumbers()));
        Register(FoldingCommands.ToggleFoldRecursively, model => ToggleCollapseState(model, int.MaxValue, SelectedLineNumbers()));
        Register(FoldingCommands.FoldAllBlockComments, FoldAllBlockComments);
        Register(FoldingCommands.FoldAllMarkerRegions, model => SetCollapseStateForMarkerRegions(model, true));
        Register(FoldingCommands.UnfoldAllMarkerRegions, model => SetCollapseStateForMarkerRegions(model, false));
        Register(FoldingCommands.FoldAllExcept, model => SetCollapseStateForRest(model, true, SelectedLineNumbers()));
        Register(FoldingCommands.UnfoldAllExcept, model => SetCollapseStateForRest(model, false, SelectedLineNumbers()));
        Register(FoldingCommands.FoldAll, model => SetCollapseStateLevelsDown(model, true));
        Register(FoldingCommands.UnfoldAll, model => SetCollapseStateLevelsDown(model, false));
        for (int level = 1; level <= FoldLevelCount; level++)
        {
            int foldLevel = level;
            Register(FoldingCommands.FoldLevel(level), model => SetCollapseStateAtLevel(model, foldLevel, true, SelectedLineNumbers()));
        }

        Register(FoldingCommands.GotoParentFold, model => GotoFold(model, GetParentFoldLine));
        Register(FoldingCommands.GotoPreviousFold, model => GotoFold(model, GetPreviousFoldLine));
        Register(FoldingCommands.GotoNextFold, model => GotoFold(model, GetNextFoldLine));
        Register(FoldingCommands.CreateFoldingRangeFromSelection, CreateFoldingRangeFromSelection);
        Register(FoldingCommands.RemoveManualFoldingRanges, RemoveManualFoldingRanges);
    }

    // Monaco: FoldingAction.runEditorCommand
    private void Run(Action<FoldingModel> invoke)
    {
        if (!_options.Folding || GetFoldingModel() is not { } foldingModel)
        {
            return;
        }

        invoke(foldingModel);
        Reveal(_viewModel!.GetSelection().Range.Start);
    }

    // Monaco: FoldingAction.getSelectedLines
    private List<int> SelectedLineNumbers() => [.. _viewModel!.GetSelections().Select(s => s.Range.StartLineNumber)];

    private string LanguageId => _model!.Tokenization.GetLanguageId();

    private void FoldAllBlockComments(FoldingModel foldingModel)
    {
        if (foldingModel.Regions.HasTypes())
        {
            SetCollapseStateForType(foldingModel, "comment", true);
        }
        else if (_model!.LanguageConfigurationRegistry.GetLanguageConfiguration(LanguageId).Comments?.BlockCommentStartToken is { Length: > 0 } token)
        {
            SetCollapseStateForMatchingLines(foldingModel, new Regex(@"^\s*" + Strings.EscapeRegExpCharacters(token)), true);
        }
    }

    private void SetCollapseStateForMarkerRegions(FoldingModel foldingModel, bool doCollapse)
    {
        if (foldingModel.Regions.HasTypes())
        {
            SetCollapseStateForType(foldingModel, "region", doCollapse);
        }
        else if (_model!.LanguageConfigurationRegistry.GetLanguageConfiguration(LanguageId).FoldingRules.Markers?.Start is { } start)
        {
            SetCollapseStateForMatchingLines(foldingModel, new Regex(start.ToString(), start.Options), doCollapse);
        }
    }

    private void GotoFold(FoldingModel foldingModel, Func<int, FoldingModel, int?> findLine)
    {
        List<int> selectedLines = SelectedLineNumbers();
        if (selectedLines.Count > 0 && findLine(selectedLines[0], foldingModel) is { } startLineNumber)
        {
            _viewModel!.SetSelections("api", [new Selection(startLineNumber, 1, startLineNumber, 1)]);
        }
    }

    private void CreateFoldingRangeFromSelection(FoldingModel foldingModel)
    {
        var collapseRanges = new List<FoldRange>();
        foreach (Selection selection in _viewModel!.GetSelections())
        {
            Range range = selection.Range;
            int endLineNumber = range.EndLineNumber;
            if (range.EndColumn == 1)
            {
                --endLineNumber;
            }

            if (endLineNumber > range.StartLineNumber)
            {
                collapseRanges.Add(new FoldRange
                {
                    StartLineNumber = range.StartLineNumber,
                    EndLineNumber = endLineNumber,
                    IsCollapsed = true,
                    Source = FoldSource.UserDefined,
                });
                _viewModel.SetSelections("api", [new Selection(range.StartLineNumber, 1, range.StartLineNumber, 1)]);
            }
        }

        if (collapseRanges.Count > 0)
        {
            List<FoldRange> sorted = [.. collapseRanges.OrderBy(r => r.StartLineNumber)];
            List<FoldRange> newRanges = FoldingRegions.SanitizeAndMerge(foldingModel.Regions, sorted, _model!.LineCount);
            foldingModel.UpdatePost(FoldingRegions.FromFoldRanges(newRanges));
        }
    }

    private void RemoveManualFoldingRanges(FoldingModel foldingModel)
    {
        foldingModel.RemoveManualRanges([.. _viewModel!.GetSelections().Select(s => s.Range)]);
        TriggerFoldingModelChanged();
    }
}
