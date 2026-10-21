// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursor.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text.RegularExpressions;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Cursors;

// The editing part of the controller: every edit (typing, paste, cut, commands) goes through these methods.
internal sealed partial class CursorsController
{
    // Monaco: /([)\]}>'"`])([^)\]}>'"`]*)$/
    private static readonly Regex CloseCharacterRegex = new(@"([)\]}>'""`])([^)\]}>'""`]*)$", RegexOptions.CultureInvariant);

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

            // Check for auto-closing closed characters
            var autoClosedCharactersRanges = new List<Range>();
            var autoClosedEnclosingRanges = new List<Range>();

            foreach (IEditCommand? command in opResult.Commands)
            {
                if (command is BaseTypeWithAutoClosingCommand { EnclosingRange: { } enclosingRange, CloseCharacterRange: { } closeCharacterRange })
                {
                    autoClosedCharactersRanges.Add(closeCharacterRange);
                    autoClosedEnclosingRanges.Add(enclosingRange);
                }
            }

            if (autoClosedCharactersRanges.Count > 0)
            {
                PushAutoClosedAction(autoClosedCharactersRanges, autoClosedEnclosingRanges);
            }

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
    public void ExecuteEdits(
        ViewModelEventsCollector eventsCollector,
        string? source,
        IReadOnlyList<IdentifiedEditOperation> edits,
        Func<IReadOnlyList<ValidEditOperation>, IReadOnlyList<Selection>?> cursorStateComputer)
    {
        List<(int OpenIndex, int CloseIndex)>? autoClosingIndices = source == "snippet" ? FindAutoClosingPairs(edits) : null;
        if (autoClosingIndices is not null)
        {
            edits = [edits[0] with { IsTracked = true }, .. edits.Skip(1)];
        }

        var autoClosedCharactersRanges = new List<Range>();
        var autoClosedEnclosingRanges = new List<Range>();
        IReadOnlyList<Selection>? selections = _model.PushEditOperations(GetSelections(), edits, undoEdits =>
        {
            if (autoClosingIndices is not null)
            {
                for (int i = 0; i < autoClosingIndices.Count; i++)
                {
                    (int openCharInnerIndex, int closeCharInnerIndex) = autoClosingIndices[i];
                    ValidEditOperation undoEdit = undoEdits[i];
                    int lineNumber = undoEdit.Range.StartLineNumber;
                    int openCharIndex = undoEdit.Range.StartColumn - 1 + openCharInnerIndex;
                    int closeCharIndex = undoEdit.Range.StartColumn - 1 + closeCharInnerIndex;

                    autoClosedCharactersRanges.Add(new Range(lineNumber, closeCharIndex + 1, lineNumber, closeCharIndex + 2));
                    autoClosedEnclosingRanges.Add(new Range(lineNumber, openCharIndex + 1, lineNumber, closeCharIndex + 2));
                }
            }

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

        if (autoClosedCharactersRanges.Count > 0)
        {
            PushAutoClosedAction(autoClosedCharactersRanges, autoClosedEnclosingRanges);
        }
    }

    // Monaco: _findAutoClosingPairs
    private List<(int OpenIndex, int CloseIndex)>? FindAutoClosingPairs(IReadOnlyList<IdentifiedEditOperation> edits)
    {
        if (edits.Count == 0)
        {
            return null;
        }

        var indices = new List<(int OpenIndex, int CloseIndex)>();
        foreach (IdentifiedEditOperation edit in edits)
        {
            if (string.IsNullOrEmpty(edit.Text) || edit.Text.Contains((char)10))
            {
                return null;
            }

            Match m = CloseCharacterRegex.Match(edit.Text);
            if (!m.Success)
            {
                return null;
            }

            string closeChar = m.Groups[1].Value;

            if (!Context.CursorConfig.AutoClosingPairs.AutoClosingPairsCloseSingleChar.TryGetValue(closeChar, out List<StandardAutoClosingPairConditional>? autoClosingPairsCandidates)
                || autoClosingPairsCandidates.Count != 1)
            {
                return null;
            }

            string openChar = autoClosingPairsCandidates[0].Open;
            int closeCharIndex = edit.Text.Length - m.Groups[2].Value.Length - 1;

            // JavaScript accepts a start index of -1 (and then finds nothing); .NET does not.
            if (closeCharIndex - 1 < 0)
            {
                return null;
            }

            int openCharIndex = edit.Text.LastIndexOf(openChar, closeCharIndex - 1, StringComparison.Ordinal);
            if (openCharIndex == -1)
            {
                return null;
            }

            indices.Add((openCharIndex, closeCharIndex));
        }

        return indices;
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
