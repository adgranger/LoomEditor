// Ported from microsoft/vscode: src/vs/editor/contrib/bracketMatching/browser/bracketMatching.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Events;
using LoomEditor.View.Features.Scheduling;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.BracketMatching;

/// <summary>Highlights the brackets around the cursors and adds the jump, select and remove bracket commands.</summary>
/// <remarks>
/// Not ported: the Go menu entry and
/// <c>onDidChangeModel</c> (the model of an editor never changes).
/// Threading: UI thread only.
/// </remarks>
internal sealed class BracketMatchingFeature : IEditorFeature
{
    private static readonly TimeSpan MaxMatchDuration = TimeSpan.FromMilliseconds(20);

    private static readonly ModelDecorationOptions DecorationOptionsWithOverviewRuler = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "bracket-match-overview",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        ClassName = "bracket-match",
        InlineClassName = "bracket-match-inline",
        OverviewRuler = new ModelDecorationOverviewRulerOptions(DecorationColor.FromThemeColor("editorOverviewRuler.bracketMatchForeground"), DecorationColor.FromThemeColor("editorOverviewRuler.bracketMatchForeground"), OverviewRulerLane.Center),
    });

    private static readonly ModelDecorationOptions DecorationOptionsWithoutOverviewRuler = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "bracket-match-no-overview",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        ClassName = "bracket-match",
        InlineClassName = "bracket-match-inline",
    });

    private sealed record BracketsData(Position Position, (Range Opening, Range Closing)? Brackets, ModelDecorationOptions Options);

    private readonly List<IDisposable> _registrations = [];
    private MatchBracketsMode _matchBrackets = MatchBracketsMode.Always;
    private List<BracketsData> _lastBracketsData = [];
    private int _lastVersionId;
    private IEditorContext? _context;
    private ViewModel? _viewModel;
    private EditorDecorationsCollection? _decorations;
    private RunOnceScheduler? _updateBracketsSoon;

    /// <inheritdoc />
    public string Id => "loom.bracketMatching";

    /// <summary>Which brackets are highlighted; setting it clears the highlight and computes it again (Monaco: <c>onDidChangeConfiguration</c>).</summary>
    internal MatchBracketsMode MatchBrackets
    {
        get => _matchBrackets;
        set
        {
            _matchBrackets = value;
            _decorations?.Clear();
            _lastBracketsData = [];
            _lastVersionId = 0;
            _updateBracketsSoon?.Schedule();
        }
    }

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not EditorContext editorContext)
        {
            return;
        }

        _context = context;
        _viewModel = editorContext.ViewModel;
        _decorations = editorContext.CreateDecorationsCollection();
        _updateBracketsSoon = new RunOnceScheduler(context.Scheduler, UpdateBrackets, TimeSpan.FromMilliseconds(50));

        Register(context, BracketMatchingCommands.JumpToBracket, JumpToBracket);
        Register(context, BracketMatchingCommands.SelectToBracket, () => SelectToBracket(true));
        Register(context, BracketMatchingCommands.RemoveBrackets, () => RemoveBrackets(BracketMatchingCommands.RemoveBrackets));

        _updateBracketsSoon.Schedule();
        context.SelectionChanged += OnSelectionChanged;
        context.Model.ContentChanged += OnScheduleEvent;
        context.Model.LanguageConfigurationChanged += OnLanguageConfigurationChanged;
        _viewModel.FocusChanged += OnScheduleEvent;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_context is not null)
        {
            _context.SelectionChanged -= OnSelectionChanged;
            _context.Model.ContentChanged -= OnScheduleEvent;
            _context.Model.LanguageConfigurationChanged -= OnLanguageConfigurationChanged;
            _viewModel!.FocusChanged -= OnScheduleEvent;
        }

        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
        _updateBracketsSoon?.Dispose();
        _decorations?.Clear();
    }

    private void Register(IEditorContext context, string id, Action run) =>
        _registrations.Add(context.Commands.Register(new EditorCommand(id, run)));

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        if (_matchBrackets == MatchBracketsMode.Never)
        {
            // Early exit if nothing needs to be done!
            return;
        }

        _updateBracketsSoon!.Schedule();
    }

    private void OnScheduleEvent(object? sender, EventArgs e) => _updateBracketsSoon!.Schedule();

    private void OnLanguageConfigurationChanged(object? sender, EventArgs e)
    {
        _lastBracketsData = [];
        _updateBracketsSoon!.Schedule();
    }

    // Monaco: jumpToBracket
    internal void JumpToBracket()
    {
        TextModel model = _context!.Model;
        var newSelections = new List<Selection>();
        foreach (Selection selection in _viewModel!.GetSelections())
        {
            Position position = selection.Range.Start;

            // find matching brackets if position is on a bracket
            Position? newCursorPosition = null;
            if (model.BracketPairs.MatchBracket(position) is { } brackets)
            {
                if (brackets.Opening.Contains(position) && !brackets.Closing.Contains(position))
                {
                    newCursorPosition = brackets.Closing.Start;
                }
                else if (brackets.Closing.Contains(position))
                {
                    newCursorPosition = brackets.Opening.Start;
                }
            }
            else if (model.BracketPairs.FindEnclosingBrackets(position) is { } enclosingBrackets)
            {
                // find the enclosing brackets if the position isn't on a matching bracket
                newCursorPosition = enclosingBrackets.Closing.Start;
            }
            else if (model.BracketPairs.FindNextBracket(position) is { } nextBracket)
            {
                // no enclosing brackets, try the very first next bracket
                newCursorPosition = nextBracket.Range.Start;
            }

            newSelections.Add(Selection.Collapsed(newCursorPosition ?? position));
        }

        SetSelectionsAndReveal(newSelections);
    }

    // Monaco: selectToBracket
    internal void SelectToBracket(bool selectBrackets)
    {
        TextModel model = _context!.Model;
        var newSelections = new List<Selection>();
        foreach (Selection selection in _viewModel!.GetSelections())
        {
            Position position = selection.Range.Start;
            (Range Opening, Range Closing)? brackets = model.BracketPairs.MatchBracket(position);
            if (brackets is null)
            {
                brackets = model.BracketPairs.FindEnclosingBrackets(position);
                if (brackets is null && model.BracketPairs.FindNextBracket(position) is { } nextBracket)
                {
                    brackets = model.BracketPairs.MatchBracket(nextBracket.Range.Start);
                }
            }

            if (brackets is null)
            {
                continue;
            }

            (Range open, Range close) = brackets.Value;
            if (Range.CompareUsingStarts(open, close) > 0)
            {
                (open, close) = (close, open);
            }

            Position selectFrom = selectBrackets ? open.Start : open.End;
            Position selectTo = selectBrackets ? close.End : close.Start;
            if (close.Contains(position))
            {
                // select backwards if the cursor was on the closing bracket
                (selectFrom, selectTo) = (selectTo, selectFrom);
            }

            newSelections.Add(new Selection(selectFrom, selectTo));
        }

        if (newSelections.Count > 0)
        {
            SetSelectionsAndReveal(newSelections);
        }
    }

    // Monaco: removeBrackets
    internal void RemoveBrackets(string editSource)
    {
        TextModel model = _context!.Model;
        foreach (Selection selection in _viewModel!.GetSelections())
        {
            Position position = selection.Position;
            (Range Opening, Range Closing)? brackets =
                model.BracketPairs.MatchBracket(position) ?? model.BracketPairs.FindEnclosingBrackets(position);
            if (brackets is null)
            {
                continue;
            }

            model.PushStackElement();
            _viewModel.ExecuteEdits(
                editSource,
                [new IdentifiedEditOperation(brackets.Value.Opening, string.Empty), new IdentifiedEditOperation(brackets.Value.Closing, string.Empty)],
                _ => null);
            model.PushStackElement();
        }
    }

    // Monaco: editor.setSelections + editor.revealRange (revealHorizontal, Simple, Smooth)
    private void SetSelectionsAndReveal(List<Selection> newSelections)
    {
        _viewModel!.SetSelections("api", newSelections);
        Range viewRange = _viewModel.CoordinatesConverter.ConvertModelRangeToViewRange(newSelections[0].Range);
        _viewModel.RevealRange("api", true, viewRange, VerticalRevealType.Simple, ScrollType.Smooth);
    }

    // Monaco: _updateBrackets
    private void UpdateBrackets()
    {
        if (_matchBrackets == MatchBracketsMode.Never)
        {
            return;
        }

        RecomputeBrackets();

        var newDecorations = new List<ModelDeltaDecoration>();
        foreach (BracketsData bracketData in _lastBracketsData)
        {
            if (bracketData.Brackets is { } brackets)
            {
                newDecorations.Add(new ModelDeltaDecoration(brackets.Opening, bracketData.Options));
                newDecorations.Add(new ModelDeltaDecoration(brackets.Closing, bracketData.Options));
            }
        }

        _decorations!.Set(newDecorations);
    }

    // Monaco: _recomputeBrackets
    private void RecomputeBrackets()
    {
        if (!_viewModel!.HasFocus)
        {
            // no focus => no brackets!
            _lastBracketsData = [];
            _lastVersionId = 0;
            return;
        }

        List<Selection> selections = _viewModel.GetSelections();
        if (selections.Count > 100)
        {
            // no bracket matching for high numbers of selections
            _lastBracketsData = [];
            _lastVersionId = 0;
            return;
        }

        TextModel model = _context!.Model;
        int versionId = model.VersionId;
        // use the previous data only if the model is at the same version id
        List<BracketsData> previousData = _lastVersionId == versionId ? _lastBracketsData : [];

        // will bracket match a cursor only if the selection is collapsed; sort positions for `previousData` cache hits
        // (OrderBy is stable, like Monaco's sort)
        List<Position> positions = [.. selections.Where(s => s.IsEmpty).Select(s => s.Range.Start).OrderBy(p => p)];

        var newData = new List<BracketsData>();
        int previousIndex = 0;
        foreach (Position position in positions)
        {
            while (previousIndex < previousData.Count && previousData[previousIndex].Position.IsBefore(position))
            {
                previousIndex++;
            }

            if (previousIndex < previousData.Count && previousData[previousIndex].Position == position)
            {
                newData.Add(previousData[previousIndex]);
            }
            else
            {
                // give at most 20ms to compute
                (Range Opening, Range Closing)? brackets = model.BracketPairs.MatchBracket(position, MaxMatchDuration);
                ModelDecorationOptions options = DecorationOptionsWithOverviewRuler;
                if (brackets is null && _matchBrackets == MatchBracketsMode.Always)
                {
                    brackets = model.BracketPairs.FindEnclosingBrackets(position, MaxMatchDuration);
                    options = DecorationOptionsWithoutOverviewRuler;
                }

                newData.Add(new BracketsData(position, brackets, options));
            }
        }

        _lastBracketsData = newData;
        _lastVersionId = versionId;
    }
}
