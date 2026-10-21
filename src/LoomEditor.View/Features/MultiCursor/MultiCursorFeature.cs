// Ported from microsoft/vscode: src/vs/editor/contrib/multicursor/browser/multicursor.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features.Find;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.MultiCursor;

/// <summary>Adds the multi-cursor commands: add cursor above/below, at line ends, focus next/previous cursor, and, when the Find feature is attached, the occurrence commands (Ctrl+D...).</summary>
/// <remarks>
/// Cut points: menus and the screen reader announcements (<c>announceCursorChange</c>) are not ported.
/// Threading: UI thread only.
/// </remarks>
public sealed class MultiCursorFeature : IEditorFeature
{
    private readonly List<IDisposable> _registrations = [];
    private MultiCursorSelectionController? _controller;
    private SelectionHighlighter? _highlighter;

    /// <inheritdoc />
    public string Id => "loom.multiCursor";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ViewModel? viewModel = context.GetViewModel();
        if (viewModel is null)
        {
            return;
        }

        Register(context, MultiCursorCommands.InsertCursorAbove, () => InsertCursor(viewModel, up: true));
        Register(context, MultiCursorCommands.InsertCursorBelow, () => InsertCursor(viewModel, up: false));
        Register(context, MultiCursorCommands.InsertCursorAtEndOfEachLineSelected, () => InsertCursorAtEndOfEachLineSelected(viewModel));
        Register(context, MultiCursorCommands.AddCursorsToBottom, () => AddCursorsToBottom(viewModel));
        Register(context, MultiCursorCommands.AddCursorsToTop, () => AddCursorsToTop(viewModel));
        Register(context, MultiCursorCommands.FocusNextCursor, () => FocusCursor(viewModel, next: true));
        Register(context, MultiCursorCommands.FocusPreviousCursor, () => FocusCursor(viewModel, next: false));

        // The Find-based commands need the find controller (Monaco: CommonFindController.get(editor)).
        FindController? findController = context.GetFindController();
        if (findController is null)
        {
            return;
        }

        var controller = new MultiCursorSelectionController(viewModel, MultiCursorOptions.Default);
        _controller = controller;
        var editorContext = context as EditorContext;
        Register(context, MultiCursorCommands.AddSelectionToNextFindMatch, () => controller.AddSelectionToNextFindMatch(findController));
        Register(context, MultiCursorCommands.AddSelectionToPreviousFindMatch, () => controller.AddSelectionToPreviousFindMatch(findController));
        Register(context, MultiCursorCommands.MoveSelectionToNextFindMatch, () => controller.MoveSelectionToNextFindMatch(findController));
        Register(context, MultiCursorCommands.MoveSelectionToPreviousFindMatch, () => controller.MoveSelectionToPreviousFindMatch(findController));
        Register(context, MultiCursorCommands.SelectHighlights, () => controller.SelectAll(findController));
        Register(context, MultiCursorCommands.ChangeAll, () => controller.SelectAll(findController), () => !viewModel.CursorConfig.ReadOnly);

        if (editorContext is not null)
        {
            _highlighter = new SelectionHighlighter(
                viewModel, controller, findController, editorContext.CreateDecorationsCollection(), context.Scheduler, controller.Options);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();

        _highlighter?.Dispose();
        _highlighter = null;
        _controller?.Dispose();
        _controller = null;
    }

    private void Register(IEditorContext context, string id, Action run, Func<bool>? canExecute = null) =>
        _registrations.Add(context.Commands.Register(new EditorCommand(id, run, canExecute)));

    // Monaco: InsertCursorAbove.run and InsertCursorBelow.run (logicalLine is always true: no arguments in phase 3)
    private static void InsertCursor(ViewModel viewModel, bool up)
    {
        if (viewModel.CursorConfig.ReadOnly)
        {
            return;
        }

        viewModel.Model.PushStackElement();
        List<CursorState> previousCursorState = viewModel.GetCursorStates();
        viewModel.SetCursorStates(
            null,
            CursorChangeReason.Explicit,
            up
                ? CursorMoveCommands.AddCursorUp(viewModel, previousCursorState, true)
                : CursorMoveCommands.AddCursorDown(viewModel, previousCursorState, true));
        if (up)
        {
            viewModel.RevealTopMostCursor(null);
        }
        else
        {
            viewModel.RevealBottomMostCursor(null);
        }
    }

    private static void InsertCursorAtEndOfEachLineSelected(ViewModel viewModel)
    {
        var newSelections = new List<Selection>();
        foreach (Selection selection in viewModel.GetSelections())
        {
            if (selection.IsEmpty)
            {
                continue;
            }

            Range range = selection.Range;
            for (int i = range.StartLineNumber; i < range.EndLineNumber; i++)
            {
                int currentLineMaxColumn = viewModel.Model.GetLineMaxColumn(i);
                newSelections.Add(new Selection(i, currentLineMaxColumn, i, currentLineMaxColumn));
            }

            if (range.EndColumn > 1)
            {
                newSelections.Add(new Selection(range.EndLineNumber, range.EndColumn, range.EndLineNumber, range.EndColumn));
            }
        }

        if (newSelections.Count > 0)
        {
            viewModel.SetSelections(null, newSelections);
        }
    }

    private static void AddCursorsToBottom(ViewModel viewModel)
    {
        Range first = viewModel.GetSelections()[0].Range;
        int lineCount = viewModel.Model.LineCount;

        var newSelections = new List<Selection>();
        for (int i = first.StartLineNumber; i <= lineCount; i++)
        {
            newSelections.Add(new Selection(i, first.StartColumn, i, first.EndColumn));
        }

        if (newSelections.Count > 0)
        {
            viewModel.SetSelections(null, newSelections);
        }
    }

    private static void AddCursorsToTop(ViewModel viewModel)
    {
        Range first = viewModel.GetSelections()[0].Range;

        var newSelections = new List<Selection>();
        for (int i = first.StartLineNumber; i >= 1; i--)
        {
            newSelections.Add(new Selection(i, first.StartColumn, i, first.EndColumn));
        }

        if (newSelections.Count > 0)
        {
            viewModel.SetSelections(null, newSelections);
        }
    }

    // Monaco: FocusNextCursor.run and FocusPreviousCursor.run
    private static void FocusCursor(ViewModel viewModel, bool next)
    {
        if (viewModel.CursorConfig.ReadOnly)
        {
            return;
        }

        viewModel.Model.PushStackElement();
        List<CursorState> states = viewModel.GetCursorStates();
        if (states.Count == 0)
        {
            return;
        }

        if (next)
        {
            CursorState first = states[0];
            states.RemoveAt(0);
            states.Add(first);
        }
        else
        {
            CursorState last = states[^1];
            states.RemoveAt(states.Count - 1);
            states.Insert(0, last);
        }

        viewModel.SetCursorStates(null, CursorChangeReason.Explicit, states.ConvertAll(s => (PartialCursorState)s));
        viewModel.RevealPrimaryCursor(null, true);
    }
}
