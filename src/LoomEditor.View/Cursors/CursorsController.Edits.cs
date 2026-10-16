// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursor.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Cursors;

// The editing part of the controller: every edit (typing, paste, cut, commands) goes through these methods.
internal sealed partial class CursorsController
{
    // Monaco: _executeEditOperation
    internal void ExecuteEditOperation(EditOperationResult? opResult)
    {
        if (opResult is null)
        {
            // Nothing to execute
            return;
        }

        if (opResult.ShouldPushStackElementBefore)
        {
            _model.PushStackElement();
        }

        IReadOnlyList<Selection>? result = CommandExecutor.ExecuteCommands(_model, _cursors.GetSelections(), opResult.Commands);
        if (result is not null)
        {
            // The commands were applied correctly
            InterpretCommandResult(result);

            // Phase 4–5 cut point: auto-closed characters. Monaco collects the close character ranges of the
            // BaseTypeWithAutoClosingCommand commands here and pushes an AutoClosedAction (cursor.ts 366–381).

            _prevEditOperationType = opResult.Type;
        }

        if (opResult.ShouldPushStackElementAfter)
        {
            _model.PushStackElement();
        }

        if (result is not null && opResult.FollowUp is not null)
        {
            ExecuteEditOperation(opResult.FollowUp(GetSelections()));
        }
    }

    /// <summary>Applies raw edits to the model and sets the selections computed by <paramref name="cursorStateComputer"/>.</summary>
    /// <remarks>
    /// Phase 4–5 cut point: Monaco looks for auto-closing pairs when <paramref name="source"/> is <c>"snippet"</c>;
    /// snippets do not exist in phase 3, so that path is never taken.
    /// </remarks>
    public void ExecuteEdits(
        ViewModelEventsCollector eventsCollector,
        string? source,
        IReadOnlyList<IdentifiedEditOperation> edits,
        Func<IReadOnlyList<ValidEditOperation>, IReadOnlyList<Selection>?> cursorStateComputer)
    {
        IReadOnlyList<Selection>? selections = _model.PushEditOperations(GetSelections(), edits, undoEdits =>
        {
            IReadOnlyList<Selection>? computed = cursorStateComputer(undoEdits);
            if (computed is not null)
            {
                // Don't recover the selection from markers because we know what it should be.
                _isHandling = true;
            }

            return computed;
        });

        if (selections is not null)
        {
            _isHandling = false;
            SetSelections(eventsCollector, source, selections, CursorChangeReason.NotSet);
        }
    }

    // Monaco: _executeEdit. A read-only editor cannot be edited: the view model reports the attempt (P3-20).
    // An exception thrown by the callback is not swallowed (Monaco calls onUnexpectedError): a bug in a port must
    // fail the tests. The cursors still go back to tracking their selections.
    internal void ExecuteEdit(
        Action callback,
        ViewModelEventsCollector eventsCollector,
        string? source,
        CursorChangeReason reason = CursorChangeReason.NotSet)
    {
        if (Context.CursorConfig.ReadOnly)
        {
            // we cannot edit when read only...
            return;
        }

        CursorModelState oldState = CursorModelState.From(_model, this);
        _cursors.StopTrackingSelections();
        _isHandling = true;

        try
        {
            _cursors.EnsureValidState();
            callback();
        }
        finally
        {
            _isHandling = false;
            _cursors.StartTrackingSelections();
        }

        ValidateAutoClosedActions();
        if (EmitStateChangedIfNecessary(eventsCollector, source, reason, oldState, false))
        {
            RevealAll(eventsCollector, source, false, VerticalRevealType.Simple, true, ScrollType.Smooth);
        }
    }

    public void ExecuteCommand(ViewModelEventsCollector eventsCollector, IEditCommand command, string? source)
    {
        ExecuteEdit(
            () =>
            {
                _cursors.KillSecondaryCursors();

                ExecuteEditOperation(new EditOperationResult(EditOperationType.Other, [command], false, false));
            },
            eventsCollector,
            source);
    }

    public void ExecuteCommands(ViewModelEventsCollector eventsCollector, IReadOnlyList<IEditCommand?> commands, string? source)
    {
        ExecuteEdit(
            () => ExecuteEditOperation(new EditOperationResult(EditOperationType.Other, commands, false, false)),
            eventsCollector,
            source);
    }
}
