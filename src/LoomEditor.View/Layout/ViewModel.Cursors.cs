// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelImpl.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Events;

namespace LoomEditor.View.Layout;

// The cursor part of the view model: it owns the CursorsController, is its "simple model" (identity mapping: view lines
// are model lines until phase 6) and wraps the cursor API in view event collection.
internal sealed partial class ViewModel
{
    /// <summary>Delivers the view events and the outgoing events (Monaco: <c>_eventDispatcher</c>).</summary>
    public ViewModelEventDispatcher EventDispatcher { get; }

    /// <summary>Converts between model and view coordinates; the identity until phase 6.</summary>
    public ICoordinatesConverter CoordinatesConverter { get; }

    /// <summary>What the cursor operations read; recreated when one of its inputs changes.</summary>
    public CursorConfiguration CursorConfig { get; private set; }

    /// <summary>The editor options the cursor reads.</summary>
    public EditorCursorOptions CursorOptions => _cursorOptions;

    /// <summary>Sets the editor options the cursor reads and recreates <see cref="CursorConfig"/>.</summary>
    public void SetCursorOptions(EditorCursorOptions options)
    {
        _cursorOptions = options;
        UpdateCursorConfiguration();
    }

    // Monaco: viewModelImpl.ts 313–316 (CursorConfiguration.shouldRecreate is not ported: always recreate)
    private void UpdateCursorConfiguration()
    {
        CursorConfig = CreateCursorConfiguration();
        _cursor.UpdateConfiguration(CursorConfig);
    }

    private CursorConfiguration CreateCursorConfiguration() =>
        new(_model.Options, _cursorOptions, _fontInfo, LayoutInfo);

    private void OnModelOptionsChanged(object? sender, ModelOptionsChangedEventArgs e) => UpdateCursorConfiguration();

    /// <summary>Tells the cursor whether the editor has the focus (Monaco: <c>setHasFocus</c>, the cursor part).</summary>
    public void SetHasFocus(bool hasFocus) => _cursor.SetHasFocus(hasFocus);

    // ----- ICursorSimpleModel (identity mapping)

    public int GetLineCount() => _model.LineCount;

    public string GetLineContent(int lineNumber) => _model.GetLineContent(lineNumber);

    public int GetLineMinColumn(int lineNumber) => _model.GetLineMinColumn(lineNumber);

    public int GetLineMaxColumn(int lineNumber) => _model.GetLineMaxColumn(lineNumber);

    public int GetLineFirstNonWhitespaceColumn(int lineNumber) => _model.GetLineFirstNonWhitespaceColumn(lineNumber);

    public int GetLineLastNonWhitespaceColumn(int lineNumber) => _model.GetLineLastNonWhitespaceColumn(lineNumber);

    public Position NormalizePosition(Position position, PositionAffinity affinity) => TextModel.NormalizePosition(position, affinity);

    public int GetLineIndentColumn(int lineNumber) => _model.GetLineIndentColumn(lineNumber);

    public int GetLineLength(int lineNumber) => _model.GetLineLength(lineNumber);

    // ----- cursor move support (Monaco: viewModelImpl.ts)

    /// <summary>Test hook: when not null, replaces <see cref="GetCompletelyVisibleViewRange"/> (the Monaco tests assign that method).</summary>
    internal Func<Range>? CompletelyVisibleViewRangeOverride { get; set; }

    public Range GetCompletelyVisibleViewRange()
    {
        if (CompletelyVisibleViewRangeOverride is { } rangeOverride)
        {
            return rangeOverride();
        }

        PartialViewLinesViewportData partialData = ViewLayout.GetLinesViewportData();
        int startViewLineNumber = partialData.CompletelyVisibleStartLineNumber;
        int endViewLineNumber = partialData.CompletelyVisibleEndLineNumber;

        return new Range(
            startViewLineNumber,
            GetLineMinColumn(startViewLineNumber),
            endViewLineNumber,
            GetLineMaxColumn(endViewLineNumber));
    }

    /// <summary>The hidden (folded) model ranges. Always empty before folding (phase 5).</summary>
    public static IReadOnlyList<Range> GetHiddenAreas() => [];

    /// <summary>The ranges of the characters the cursor auto-closed. Always empty before auto-closing (phase 4–5 cut point).</summary>
    public static IReadOnlyList<Range> GetCursorAutoClosedCharacters() => [];

    /// <summary>The text direction of a line. Always <see cref="TextDirection.LTR"/> in phase 3.</summary>
    public static TextDirection GetTextDirection(int lineNumber) => TextDirection.LTR;

    // ----- cursor operations (Monaco: viewModelImpl.ts 1154–1262)

    public CursorState GetPrimaryCursorState() => _cursor.GetPrimaryCursorState();

    public int GetLastAddedCursorIndex() => _cursor.GetLastAddedCursorIndex();

    public List<CursorState> GetCursorStates() => _cursor.GetCursorStates();

    public bool SetCursorStates(string? source, CursorChangeReason reason, IReadOnlyList<PartialCursorState>? states) =>
        WithViewEventsCollector(eventsCollector => _cursor.SetStates(eventsCollector, source, reason, states));

    public ColumnSelectData GetCursorColumnSelectData() => _cursor.GetCursorColumnSelectData();

    public void SetCursorColumnSelectData(ColumnSelectData columnSelectData) => _cursor.SetCursorColumnSelectData(columnSelectData);

    public EditOperationType GetPrevEditOperationType() => _cursor.GetPrevEditOperationType();

    public void SetPrevEditOperationType(EditOperationType type) => _cursor.SetPrevEditOperationType(type);

    public Selection GetSelection() => _cursor.GetSelection();

    public List<Selection> GetSelections() => _cursor.GetSelections();

    public Position GetPosition() => _cursor.GetPrimaryCursorState().ModelState.Position;

    public void SetSelections(string? source, IReadOnlyList<Selection> selections, CursorChangeReason reason = CursorChangeReason.NotSet) =>
        WithViewEventsCollector(eventsCollector =>
        {
            _cursor.SetSelections(eventsCollector, source, selections, reason);
            return true;
        });

    public List<SavedCursorState> SaveCursorState() => _cursor.SaveState();

    public void RestoreCursorState(IReadOnlyList<SavedCursorState> states) =>
        WithViewEventsCollector(eventsCollector =>
        {
            _cursor.RestoreState(eventsCollector, states);
            return true;
        });

    // Monaco: _executeCursorEdit
    private void ExecuteCursorEdit(Action<ViewModelEventsCollector> callback)
    {
        if (_cursor.Context.CursorConfig.ReadOnly)
        {
            // we cannot edit when read only...
            EventDispatcher.EmitOutgoingEvent(new ReadOnlyEditAttemptEvent());
            return;
        }

        WithViewEventsCollector(eventsCollector =>
        {
            callback(eventsCollector);
            return true;
        });
    }

    public void ExecuteEdits(
        string? source,
        IReadOnlyList<IdentifiedEditOperation> edits,
        Func<IReadOnlyList<ValidEditOperation>, IReadOnlyList<Selection>?> cursorStateComputer) =>
        ExecuteCursorEdit(eventsCollector => _cursor.ExecuteEdits(eventsCollector, source, edits, cursorStateComputer));

    public void Type(string text, string? source = null) =>
        ExecuteCursorEdit(eventsCollector => _cursor.Type(eventsCollector, text, source));

    public void Paste(string text, bool pasteOnNewLine, IReadOnlyList<string>? multicursorText = null, string? source = null, bool isBlock = false) =>
        ExecuteCursorEdit(eventsCollector => _cursor.Paste(eventsCollector, text, pasteOnNewLine, multicursorText, source, isBlock));

    public void Cut(string? source = null) =>
        ExecuteCursorEdit(eventsCollector => _cursor.Cut(eventsCollector, source));

    /// <summary>
    /// The text a copy of <paramref name="modelRanges"/> puts on the clipboard (Monaco: <c>getPlainTextToCopy</c>). Monaco returns a
    /// string or an array of strings: exactly one of <c>SourceText</c> and <c>SourceTexts</c> is not <see langword="null"/>.
    /// </summary>
    public (IReadOnlyList<Range> SourceRanges, string? SourceText, IReadOnlyList<string>? SourceTexts) GetPlainTextToCopy(
        IReadOnlyList<Range> modelRanges,
        bool emptySelectionClipboard,
        bool forceCrlf)
    {
        string newLineCharacter = forceCrlf ? "\r\n" : _model.Eol;

        // Monaco copies the array and sorts it (JavaScript sorts are stable).
        List<Range> sortedRanges = modelRanges.OrderBy(r => r, Comparer<Range>.Create(Range.CompareUsingStarts)).ToList();

        bool hasEmptyRange = false;
        bool hasNonEmptyRange = false;
        foreach (Range range in sortedRanges)
        {
            if (range.IsEmpty)
            {
                hasEmptyRange = true;
            }
            else
            {
                hasNonEmptyRange = true;
            }
        }

        if (!hasNonEmptyRange && !emptySelectionClipboard)
        {
            // all ranges are empty
            return ([], string.Empty, null);
        }

        var ranges = new List<Range>();
        var result = new List<string>();
        EndOfLinePreference preference = forceCrlf ? EndOfLinePreference.CRLF : EndOfLinePreference.TextDefined;

        void PushRange(Range modelRange, string append = "")
        {
            ranges.Add(modelRange);
            result.Add(_model.GetValueInRange(modelRange, preference) + append);
        }

        if (hasEmptyRange && emptySelectionClipboard)
        {
            // some (maybe all) empty selections
            int prevModelLineNumber = 0;
            foreach (Range modelRange in sortedRanges)
            {
                int modelLineNumber = modelRange.StartLineNumber;
                if (modelRange.IsEmpty)
                {
                    if (modelLineNumber != prevModelLineNumber)
                    {
                        PushRange(new Range(modelLineNumber, _model.GetLineMinColumn(modelLineNumber), modelLineNumber, _model.GetLineMaxColumn(modelLineNumber)), newLineCharacter);
                    }
                }
                else
                {
                    PushRange(modelRange);
                }

                prevModelLineNumber = modelLineNumber;
            }
        }
        else
        {
            foreach (Range modelRange in sortedRanges)
            {
                if (!modelRange.IsEmpty)
                {
                    PushRange(modelRange);
                }
            }
        }

        return result.Count == 1 ? (ranges, result[0], null) : (ranges, null, result);
    }

    public void ExecuteCommand(IEditCommand command, string? source = null) =>
        ExecuteCursorEdit(eventsCollector => _cursor.ExecuteCommand(eventsCollector, command, source));

    public void ExecuteCommands(IReadOnlyList<IEditCommand?> commands, string? source = null) =>
        ExecuteCursorEdit(eventsCollector => _cursor.ExecuteCommands(eventsCollector, commands, source));

    public void RevealAllCursors(string? source, bool revealHorizontal, bool minimalReveal = false) =>
        WithViewEventsCollector(eventsCollector =>
        {
            _cursor.RevealAll(eventsCollector, source, minimalReveal, VerticalRevealType.Simple, revealHorizontal, ScrollType.Smooth);
            return true;
        });

    public void RevealPrimaryCursor(string? source, bool revealHorizontal, bool minimalReveal = false) =>
        WithViewEventsCollector(eventsCollector =>
        {
            _cursor.RevealPrimary(eventsCollector, source, minimalReveal, VerticalRevealType.Simple, revealHorizontal, ScrollType.Smooth);
            return true;
        });

    public void RevealTopMostCursor(string? source) =>
        RevealRange(source, true, Range.Collapsed(_cursor.GetTopMostViewPosition()), VerticalRevealType.Simple, ScrollType.Smooth);

    public void RevealBottomMostCursor(string? source) =>
        RevealRange(source, true, Range.Collapsed(_cursor.GetBottomMostViewPosition()), VerticalRevealType.Simple, ScrollType.Smooth);

    public void RevealRange(string? source, bool revealHorizontal, Range viewRange, VerticalRevealType verticalType, ScrollType scrollType) =>
        WithViewEventsCollector(eventsCollector =>
        {
            eventsCollector.EmitViewEvent(new ViewRevealRangeRequestEvent(source, false, viewRange, null, verticalType, revealHorizontal, scrollType));
            return true;
        });

    /// <summary>Runs <paramref name="callback"/> and delivers the events it produced once, at the end.</summary>
    public void BatchEvents(Action callback) =>
        WithViewEventsCollector(_ =>
        {
            callback();
            return true;
        });

    // Monaco: _withViewEventsCollector (the transactional target is not ported: there is no view to batch)
    private T WithViewEventsCollector<T>(Func<ViewModelEventsCollector, T> callback) => EmitViewEvent(callback);

    // Monaco: _emitViewEvent
    private T EmitViewEvent<T>(Func<ViewModelEventsCollector, T> callback)
    {
        try
        {
            ViewModelEventsCollector eventsCollector = EventDispatcher.BeginEmitViewEvents();
            return callback(eventsCollector);
        }
        finally
        {
            EventDispatcher.EndEmitViewEvents();
        }
    }

    private void EmitViewEvent(Action<ViewModelEventsCollector> callback) =>
        EmitViewEvent(eventsCollector =>
        {
            callback(eventsCollector);
            return true;
        });
}
