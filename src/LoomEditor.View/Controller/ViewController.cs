// Ported from microsoft/vscode: src/vs/editor/browser/view/viewController.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Controller;

/// <summary>
/// Turns a click into a cursor command (Monaco: the mouse half of <c>ViewController</c>): click moves, Shift+click extends,
/// Alt+click adds or removes a cursor, double-click selects a word, triple-click a line, four clicks everything.
/// </summary>
/// <remarks>
/// <para>
/// Monaco's <c>commandService.executeCommand(id, args)</c> becomes a direct call to
/// <c>CoreNavigationCommands.X.RunCoreEditorCommand</c>. Phase 4–5 cut points: <c>_trySelectBracketContent</c> (needs bracket
/// pairs) and <c>_trySelectStringContent</c> (needs tokens) always find nothing, so a double-click selects the word.
/// There is no Linux selection clipboard (<c>isLinux</c> is <c>false</c>), no <c>scrollOnMiddleClick</c> (default
/// <c>false</c>) and no injected text.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class ViewController(ViewModel viewModel)
{
    /// <summary>Monaco: <c>dispatchMouse</c>, the whole decision table.</summary>
    public void DispatchMouse(MouseDispatchData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        EditorCursorOptions options = viewModel.CursorOptions;
        // Monaco: selectionClipboardIsOn = isLinux && selectionClipboard; scrollOnMiddleClick defaults to false.
        if (data.MiddleButton)
        {
            ColumnSelect(data.Position, data.MouseColumn, data.InSelectionMode);
        }
        else if (data.StartedOnLineNumbers)
        {
            // If the dragging started on the gutter, then have operations work on the entire line.
            if (HasMulticursorModifier(data))
            {
                if (data.InSelectionMode)
                {
                    RunMove(CoreNavigationCommands.LastCursorLineSelect, data.Position, data.RevealType);
                }
                else
                {
                    CreateCursor(data.Position, true);
                }
            }
            else if (data.InSelectionMode)
            {
                RunMove(CoreNavigationCommands.LineSelectDrag, data.Position, data.RevealType);
            }
            else
            {
                RunMove(CoreNavigationCommands.LineSelect, data.Position, data.RevealType);
            }
        }
        else if (data.MouseDownCount >= 4)
        {
            CoreNavigationCommands.SelectAll.RunCoreEditorCommand(viewModel, new BaseCommandArgs("mouse"));
        }
        else if (data.MouseDownCount == 3)
        {
            if (HasMulticursorModifier(data))
            {
                RunMove(
                    data.InSelectionMode ? CoreNavigationCommands.LastCursorLineSelectDrag : CoreNavigationCommands.LastCursorLineSelect,
                    data.Position,
                    data.RevealType);
            }
            else
            {
                RunMove(
                    data.InSelectionMode ? CoreNavigationCommands.LineSelectDrag : CoreNavigationCommands.LineSelect,
                    data.Position,
                    data.RevealType);
            }
        }
        else if (data.MouseDownCount == 2)
        {
            if (!data.OnInjectedText)
            {
                DispatchDoubleClick(data, options);
            }
        }
        else if (HasMulticursorModifier(data))
        {
            if (!HasNonMulticursorModifier(data))
            {
                if (data.ShiftKey)
                {
                    ColumnSelect(data.Position, data.MouseColumn, true);
                }
                else if (data.InSelectionMode)
                {
                    // Do multi-cursor operations only when purely alt is pressed.
                    RunMove(CoreNavigationCommands.LastCursorMoveToSelect, data.Position, data.RevealType);
                }
                else
                {
                    CreateCursor(data.Position, false);
                }
            }
        }
        else if (data.InSelectionMode)
        {
            if (data.AltKey || options.ColumnSelection)
            {
                ColumnSelect(data.Position, data.MouseColumn, true);
            }
            else
            {
                RunMove(CoreNavigationCommands.MoveToSelect, data.Position, data.RevealType);
            }
        }
        else
        {
            MoveTo(data.Position, data.RevealType);
        }
    }

    /// <summary>Monaco: <c>moveTo</c>.</summary>
    public void MoveTo(Position viewPosition, NavigationCommandRevealType revealType) =>
        RunMove(CoreNavigationCommands.MoveTo, viewPosition, revealType);

    /// <summary>Monaco: <c>setSelection</c>.</summary>
    public void SetSelection(Selection modelSelection) =>
        CoreNavigationCommands.SetSelection.RunCoreEditorCommand(viewModel, new SetSelectionCommandArgs(modelSelection, "keyboard"));

    private void DispatchDoubleClick(MouseDispatchData data, EditorCursorOptions options)
    {
        if (HasMulticursorModifier(data))
        {
            RunMove(CoreNavigationCommands.LastCursorWordSelect, data.Position, data.RevealType);
        }
        else if (data.InSelectionMode)
        {
            RunMove(CoreNavigationCommands.WordSelectDrag, data.Position, data.RevealType);
        }
        else
        {
            Selection? selection = null;
            if (options.DoubleClickSelectsBlock)
            {
                Position modelPos = ConvertViewToModelPosition(data.Position);
                selection = TrySelectBracketContent(modelPos) ?? TrySelectStringContent(modelPos);
            }

            if (selection is { } block)
            {
                CoreNavigationCommands.SetSelection.RunCoreEditorCommand(viewModel, new SetSelectionCommandArgs(block, "mouse"));
            }
            else
            {
                RunMove(CoreNavigationCommands.WordSelect, data.Position, data.RevealType);
            }
        }
    }

    // Phase 4–5 cut point: Monaco matches the bracket pair around `pos` (model.bracketPairs.matchBracket).
    private static Selection? TrySelectBracketContent(Position pos)
    {
        _ = pos;
        return null;
    }

    // Phase 4–5 cut point: Monaco expands over the string tokens around `pos` (model.tokenization.getLineTokens).
    private static Selection? TrySelectStringContent(Position pos)
    {
        _ = pos;
        return null;
    }

    // Monaco: _validateViewColumn.
    private Position ValidateViewColumn(Position viewPosition)
    {
        int minColumn = viewModel.GetLineMinColumn(viewPosition.LineNumber);
        return viewPosition.Column < minColumn ? new Position(viewPosition.LineNumber, minColumn) : viewPosition;
    }

    // Monaco: _hasMulticursorModifier.
    private bool HasMulticursorModifier(MouseDispatchData data) => viewModel.CursorOptions.MultiCursorModifier switch
    {
        MultiCursorModifier.Alt => data.AltKey,
        MultiCursorModifier.CtrlCmd => data.CtrlKey,
        MultiCursorModifier.Meta => data.MetaKey,
        _ => false,
    };

    // Monaco: _hasNonMulticursorModifier.
    private bool HasNonMulticursorModifier(MouseDispatchData data) => viewModel.CursorOptions.MultiCursorModifier switch
    {
        MultiCursorModifier.Alt => data.CtrlKey || data.MetaKey,
        MultiCursorModifier.CtrlCmd => data.AltKey || data.MetaKey,
        MultiCursorModifier.Meta => data.CtrlKey || data.AltKey,
        _ => false,
    };

    // Monaco: _usualArgs and the commands that take them.
    private void RunMove(CoreEditorCommand<MoveCommandArgs> command, Position viewPosition, NavigationCommandRevealType revealType)
    {
        viewPosition = ValidateViewColumn(viewPosition);
        command.RunCoreEditorCommand(
            viewModel, new MoveCommandArgs(ConvertViewToModelPosition(viewPosition), viewPosition, revealType, "mouse"));
    }

    private void ColumnSelect(Position viewPosition, int mouseColumn, bool doColumnSelect)
    {
        viewPosition = ValidateViewColumn(viewPosition);
        CoreNavigationCommands.ColumnSelect.RunCoreEditorCommand(
            viewModel,
            new ColumnSelectCommandArgs(ConvertViewToModelPosition(viewPosition), viewPosition, mouseColumn, doColumnSelect, "mouse"));
    }

    private void CreateCursor(Position viewPosition, bool wholeLine)
    {
        viewPosition = ValidateViewColumn(viewPosition);
        CoreNavigationCommands.CreateCursor.RunCoreEditorCommand(
            viewModel, new CreateCursorCommandArgs(ConvertViewToModelPosition(viewPosition), viewPosition, wholeLine, "mouse"));
    }

    private Position ConvertViewToModelPosition(Position viewPosition) =>
        viewModel.CoordinatesConverter.ConvertViewPositionToModelPosition(viewPosition);
}
