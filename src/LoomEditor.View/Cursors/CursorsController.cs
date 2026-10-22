// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursor.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Decorations;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Cursors;

/// <summary>The cursor of one editor: its <see cref="CursorCollection"/>, how it follows the model, and its events (Monaco: <c>CursorsController</c>).</summary>
/// <remarks>
/// <para>This part holds the state, the reaction to model changes, reveal requests and events; the editing part comes with P3-19.</para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed partial class CursorsController : IDisposable
{
    // Monaco builds equal dynamic options each time; no DecorationStyles entry: the CSS rule of auto-closed-character is commented out upstream.
    private static readonly ModelDecorationOptions AutoClosedCharacterOptions = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "auto-closed-character",
        InlineClassName = "auto-closed-character",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
    });

    private static readonly ModelDecorationOptions AutoClosedEnclosingOptions = ModelDecorationOptions.Register(new ModelDecorationOptionsData
    {
        Description = "auto-closed-enclosing",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
    });

    private readonly TextModel _model;
    private int _knownModelVersionId;
    private readonly ICursorSimpleModel _viewModel;
    private readonly ICoordinatesConverter _coordinatesConverter;
    private CursorCollection _cursors;

    private bool _hasFocus;
    private bool _isHandling;
    private ColumnSelectData? _columnSelectData;
    private List<AutoClosedAction> _autoClosedActions;
    private EditOperationType _prevEditOperationType;

    public CursorsController(
        TextModel model,
        ICursorSimpleModel viewModel,
        ICoordinatesConverter coordinatesConverter,
        CursorConfiguration cursorConfig)
    {
        _model = model;
        _knownModelVersionId = _model.VersionId;
        _viewModel = viewModel;
        _coordinatesConverter = coordinatesConverter;
        Context = new CursorContext(_model, _viewModel, _coordinatesConverter, cursorConfig);
        _cursors = new CursorCollection(Context);

        _hasFocus = false;
        _isHandling = false;
        _columnSelectData = null;
        _autoClosedActions = [];
        _prevEditOperationType = EditOperationType.Other;
    }

    public CursorContext Context { get; private set; }

    public void Dispose()
    {
        _cursors.Dispose();
        foreach (AutoClosedAction autoClosedAction in _autoClosedActions)
        {
            autoClosedAction.Dispose();
        }

        _autoClosedActions = [];
    }

    public void UpdateConfiguration(CursorConfiguration cursorConfig)
    {
        Context = new CursorContext(_model, _viewModel, _coordinatesConverter, cursorConfig);
        _cursors.UpdateContext(Context);
    }

    public void OnLineMappingChanged(ViewModelEventsCollector eventsCollector)
    {
        if (_knownModelVersionId != _model.VersionId)
        {
            // There are model change events that were not received yet: the model change handler will ensure a
            // valid cursor state.
            return;
        }

        // Ensure valid state
        SetStates(eventsCollector, "viewModel", CursorChangeReason.NotSet, GetCursorStates());
    }

    public void SetHasFocus(bool hasFocus) => _hasFocus = hasFocus;

    private void ValidateAutoClosedActions()
    {
        if (_autoClosedActions.Count > 0)
        {
            List<Selection> selections = _cursors.GetSelections();
            var validActions = new List<AutoClosedAction>();
            var invalidActions = new List<AutoClosedAction>();
            foreach (AutoClosedAction autoClosedAction in _autoClosedActions)
            {
                (autoClosedAction.IsValid(selections) ? validActions : invalidActions).Add(autoClosedAction);
            }

            // Disposal fires decoration events whose listeners may re-enter the cursor.
            _autoClosedActions = validActions;
            foreach (AutoClosedAction invalidAction in invalidActions)
            {
                invalidAction.Dispose();
            }
        }
    }

    /// <summary>The ranges of the characters the cursor auto-closed (Monaco: <c>getAutoClosedCharacters</c>).</summary>
    public List<Range> GetAutoClosedCharacters() => AutoClosedAction.GetAllAutoClosedCharacters(_autoClosedActions);

    // Monaco: _pushAutoClosedAction
    private void PushAutoClosedAction(List<Range> autoClosedCharactersRanges, List<Range> autoClosedEnclosingRanges)
    {
        var autoClosedCharactersDeltaDecorations = new List<ModelDeltaDecoration>();
        var autoClosedEnclosingDeltaDecorations = new List<ModelDeltaDecoration>();

        for (int i = 0; i < autoClosedCharactersRanges.Count; i++)
        {
            autoClosedCharactersDeltaDecorations.Add(new ModelDeltaDecoration(autoClosedCharactersRanges[i], AutoClosedCharacterOptions));
            autoClosedEnclosingDeltaDecorations.Add(new ModelDeltaDecoration(autoClosedEnclosingRanges[i], AutoClosedEnclosingOptions));
        }

        IReadOnlyList<string> autoClosedCharactersDecorations = _model.DeltaDecorations(0, [], autoClosedCharactersDeltaDecorations);
        IReadOnlyList<string> autoClosedEnclosingDecorations = _model.DeltaDecorations(0, [], autoClosedEnclosingDeltaDecorations);
        _autoClosedActions.Add(new AutoClosedAction(_model, [.. autoClosedCharactersDecorations], [.. autoClosedEnclosingDecorations]));
    }

    // ------ some getters/setters

    public CursorState GetPrimaryCursorState() => _cursors.GetPrimaryCursor();

    public int GetLastAddedCursorIndex() => _cursors.GetLastAddedCursorIndex();

    public List<CursorState> GetCursorStates() => _cursors.GetAll();

    /// <summary>Sets the states of the cursors, keeping at most <c>MultiCursorLimit</c> of them.</summary>
    /// <returns>True when the state of the cursors changed.</returns>
    public bool SetStates(
        ViewModelEventsCollector eventsCollector,
        string? source,
        CursorChangeReason reason,
        IReadOnlyList<PartialCursorState>? states)
    {
        bool reachedMaxCursorCount = false;
        int multiCursorLimit = Context.CursorConfig.MultiCursorLimit;
        if (states is not null && states.Count > multiCursorLimit)
        {
            states = states.Take(multiCursorLimit).ToList();
            reachedMaxCursorCount = true;
        }

        CursorModelState oldState = CursorModelState.From(_model, this);

        _cursors.SetStates(states);
        _cursors.Normalize();
        _columnSelectData = null;

        ValidateAutoClosedActions();

        return EmitStateChangedIfNecessary(eventsCollector, source, reason, oldState, reachedMaxCursorCount);
    }

    public void SetCursorColumnSelectData(ColumnSelectData columnSelectData) => _columnSelectData = columnSelectData;

    public void RevealAll(
        ViewModelEventsCollector eventsCollector,
        string? source,
        bool minimalReveal,
        VerticalRevealType verticalType,
        bool revealHorizontal,
        ScrollType scrollType)
    {
        List<Position> viewPositions = _cursors.GetViewPositions();

        Range? revealViewRange = null;
        List<Selection>? revealViewSelections = null;
        if (viewPositions.Count > 1)
        {
            revealViewSelections = _cursors.GetViewSelections();
        }
        else
        {
            revealViewRange = Range.Collapsed(viewPositions[0]);
        }

        eventsCollector.EmitViewEvent(new ViewRevealRangeRequestEvent(
            source, minimalReveal, revealViewRange, revealViewSelections, verticalType, revealHorizontal, scrollType));
    }

    public void RevealPrimary(
        ViewModelEventsCollector eventsCollector,
        string? source,
        bool minimalReveal,
        VerticalRevealType verticalType,
        bool revealHorizontal,
        ScrollType scrollType)
    {
        CursorState primaryCursor = _cursors.GetPrimaryCursor();
        Selection[] revealViewSelections = [primaryCursor.ViewState.Selection];
        eventsCollector.EmitViewEvent(new ViewRevealRangeRequestEvent(
            source, minimalReveal, null, revealViewSelections, verticalType, revealHorizontal, scrollType));
    }

    public List<SavedCursorState> SaveState()
    {
        var result = new List<SavedCursorState>();

        foreach (Selection selection in _cursors.GetSelections())
        {
            result.Add(new SavedCursorState(
                !selection.IsEmpty,
                new SavedCursorPosition(selection.SelectionStartLineNumber, selection.SelectionStartColumn),
                new SavedCursorPosition(selection.PositionLineNumber, selection.PositionColumn)));
        }

        return result;
    }

    public void RestoreState(ViewModelEventsCollector eventsCollector, IReadOnlyList<SavedCursorState> states)
    {
        var desiredSelections = new List<Selection>();

        foreach (SavedCursorState state in states)
        {
            // Monaco tests the truthiness of each value: a missing or 0 value keeps the default (JavaScript trap 5).
            // Plain ints are used because 0 cannot be held by a Position.
            int positionLineNumber = Truthy(state.Position?.LineNumber) ?? 1;
            int positionColumn = Truthy(state.Position?.Column) ?? 1;
            int selectionStartLineNumber = Truthy(state.SelectionStart?.LineNumber) ?? positionLineNumber;
            int selectionStartColumn = Truthy(state.SelectionStart?.Column) ?? positionColumn;

            desiredSelections.Add(new Selection(selectionStartLineNumber, selectionStartColumn, positionLineNumber, positionColumn));
        }

        SetStates(eventsCollector, "restoreState", CursorChangeReason.NotSet, CursorState.FromModelSelections(desiredSelections));
        RevealAll(eventsCollector, "restoreState", false, VerticalRevealType.Simple, true, ScrollType.Immediate);
    }

    private static int? Truthy(int? value) => value is null or 0 ? null : value;

    /// <summary>Lets the cursors follow a change of injected text (Monaco: the <c>ModelInjectedTextChangedEvent</c> branch of <c>onModelContentChanged</c>).</summary>
    public void OnModelInjectedTextChanged(ViewModelEventsCollector eventsCollector)
    {
        if (_isHandling)
        {
            return;
        }

        _isHandling = true;
        try
        {
            SetStates(eventsCollector, "modelChange", CursorChangeReason.NotSet, GetCursorStates());
        }
        finally
        {
            _isHandling = false;
        }
    }

    /// <summary>Lets the cursors follow a change of the model.</summary>
    /// <remarks>
    /// An undo or a redo restores the selections of the undo stack only when the editor has the focus; otherwise the cursors read
    /// their tracked ranges.
    /// </remarks>
    public void OnModelContentChanged(ViewModelEventsCollector eventsCollector, ModelRawContentChangedEventArgs e)
    {
        _knownModelVersionId = e.VersionId;
        if (_isHandling)
        {
            return;
        }

        bool hadFlushEvent = e.ContainsEvent(RawContentChangedType.Flush);
        _prevEditOperationType = EditOperationType.Other;

        if (hadFlushEvent)
        {
            // a model.SetValue() was called
            _cursors.Dispose();
            _cursors = new CursorCollection(Context);
            ValidateAutoClosedActions();
            EmitStateChangedIfNecessary(eventsCollector, "model", CursorChangeReason.ContentFlush, null, false);
        }
        else if (_hasFocus && e.ResultingSelection is { Count: > 0 } resultingSelection)
        {
            PartialModelCursorState[] cursorState = CursorState.FromModelSelections(resultingSelection);
            CursorChangeReason reason = e.IsUndoing
                ? CursorChangeReason.Undo
                : e.IsRedoing ? CursorChangeReason.Redo : CursorChangeReason.RecoverFromMarkers;
            if (SetStates(eventsCollector, "modelChange", reason, cursorState))
            {
                RevealAll(eventsCollector, "modelChange", false, VerticalRevealType.Simple, true, ScrollType.Smooth);
            }
        }
        else
        {
            List<Selection> selectionsFromMarkers = _cursors.ReadSelectionFromMarkers();
            SetStates(
                eventsCollector,
                "modelChange",
                CursorChangeReason.RecoverFromMarkers,
                CursorState.FromModelSelections(selectionsFromMarkers));
        }
    }

    public Selection GetSelection() => _cursors.GetPrimaryCursor().ModelState.Selection;

    public Position GetTopMostViewPosition() => _cursors.GetTopMostViewPosition();

    public Position GetBottomMostViewPosition() => _cursors.GetBottomMostViewPosition();

    public ColumnSelectData GetCursorColumnSelectData()
    {
        if (_columnSelectData is { } columnSelectData)
        {
            return columnSelectData;
        }

        CursorState primaryCursor = _cursors.GetPrimaryCursor();
        Position viewSelectionStart = primaryCursor.ViewState.SelectionStart.Start;
        Position viewPosition = primaryCursor.ViewState.Position;
        return new ColumnSelectData(
            IsReal: false,
            FromViewLineNumber: viewSelectionStart.LineNumber,
            FromViewVisualColumn: Context.CursorConfig.VisibleColumnFromColumn(_viewModel, viewSelectionStart),
            ToViewLineNumber: viewPosition.LineNumber,
            ToViewVisualColumn: Context.CursorConfig.VisibleColumnFromColumn(_viewModel, viewPosition));
    }

    public List<Selection> GetSelections() => _cursors.GetSelections();

    /// <summary>The selections of the cursors in view coordinates (Monaco: <c>getViewSelections</c>).</summary>
    public List<Selection> GetViewSelections() => _cursors.GetViewSelections();

    public Position GetPosition() => _cursors.GetPrimaryCursor().ModelState.Position;

    public void SetSelections(
        ViewModelEventsCollector eventsCollector,
        string? source,
        IReadOnlyList<Selection> selections,
        CursorChangeReason reason) =>
        SetStates(eventsCollector, source, reason, CursorState.FromModelSelections(selections));

    public EditOperationType GetPrevEditOperationType() => _prevEditOperationType;

    public void SetPrevEditOperationType(EditOperationType type) => _prevEditOperationType = type;

    // ------ auxiliary handling logic

    // Monaco: _interpretCommandResult
    private void InterpretCommandResult(IReadOnlyList<Selection>? cursorState)
    {
        if (cursorState is null || cursorState.Count == 0)
        {
            cursorState = _cursors.ReadSelectionFromMarkers();
        }

        _columnSelectData = null;
        _cursors.SetSelections(cursorState);
        _cursors.Normalize();
    }

    // ----- emitting events

    private bool EmitStateChangedIfNecessary(
        ViewModelEventsCollector eventsCollector,
        string? source,
        CursorChangeReason reason,
        CursorModelState? oldState,
        bool reachedMaxCursorCount)
    {
        CursorModelState newState = CursorModelState.From(_model, this);
        if (newState.Equals(oldState))
        {
            return false;
        }

        List<Selection> selections = _cursors.GetSelections();
        List<Selection> viewSelections = _cursors.GetViewSelections();

        // Let the view get the event first.
        eventsCollector.EmitViewEvent(new ViewCursorStateChangedEvent(viewSelections, selections, reason));

        // Only after the view has been notified, let the rest of the world know...
        if (oldState is null
            || oldState.CursorState.Count != newState.CursorState.Count
            || HasModelStateChange(oldState, newState))
        {
            List<Selection>? oldSelections = oldState?.CursorState.Select(s => s.ModelState.Selection).ToList();
            int oldModelVersionId = oldState?.ModelVersionId ?? 0;
            eventsCollector.EmitOutgoingEvent(new CursorStateChangedEvent(
                oldSelections,
                selections,
                oldModelVersionId,
                newState.ModelVersionId,
                string.IsNullOrEmpty(source) ? "keyboard" : source,
                reason,
                reachedMaxCursorCount));
        }

        return true;
    }

    private static bool HasModelStateChange(CursorModelState oldState, CursorModelState newState)
    {
        for (int i = 0; i < newState.CursorState.Count; i++)
        {
            if (!newState.CursorState[i].ModelState.Equals(oldState.CursorState[i].ModelState))
            {
                return true;
            }
        }

        return false;
    }
}
