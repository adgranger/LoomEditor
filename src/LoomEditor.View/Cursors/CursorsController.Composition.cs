// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursor.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Events;

namespace LoomEditor.View.Cursors;

// The IME composition entry points of the controller.
internal sealed partial class CursorsController
{
    private CompositionState? _compositionState;

    /// <summary>Remembers the lines under the cursors; a composition is active until <see cref="EndComposition"/> (Monaco: <c>startComposition</c>).</summary>
    public void StartComposition(ViewModelEventsCollector eventsCollector) =>
        _compositionState = new CompositionState(_model, GetSelections());

    /// <summary>Ends the composition and, for keyboard input, makes the edit the typed character would have made (Monaco: <c>endComposition</c>).</summary>
    public void EndComposition(ViewModelEventsCollector eventsCollector, string? source = null)
    {
        List<CompositionOutcome>? compositionOutcome = _compositionState?.DeduceOutcome(_model, GetSelections());
        _compositionState = null;

        ExecuteEdit(
            () =>
            {
                if (source == "keyboard")
                {
                    // composition finishes, let's check if we need to auto complete if necessary.
                    EditOperationResult? edits = TypeOperations.CompositionEndWithInterceptors(Context.CursorConfig, compositionOutcome);
                    if (edits is not null)
                    {
                        ExecuteEditOperation(edits);
                    }
                }
            },
            eventsCollector,
            source);
    }

    /// <summary>One step of a composition: replaces the characters around the cursors (Monaco: <c>compositionType</c>).</summary>
    /// <param name="eventsCollector">Collects the events of this edit.</param>
    /// <param name="text">The new text.</param>
    /// <param name="replacePrevCharCnt">How many characters before each cursor it replaces.</param>
    /// <param name="replaceNextCharCnt">How many characters after each cursor it replaces.</param>
    /// <param name="positionDelta">How many columns the cursors move from the end of the new text.</param>
    /// <param name="source">Who asked for the edit.</param>
    public void CompositionType(
        ViewModelEventsCollector eventsCollector,
        string text,
        int replacePrevCharCnt,
        int replaceNextCharCnt,
        int positionDelta,
        string? source = null)
    {
        if (text.Length == 0 && replacePrevCharCnt == 0 && replaceNextCharCnt == 0)
        {
            // this edit is a no-op
            if (positionDelta != 0)
            {
                // but it still wants to move the cursor
                var newSelections = new List<Selection>();
                foreach (Selection selection in GetSelections())
                {
                    Position position = selection.Position;

                    // Monaco may build column 0 here; the selections are validated against the model anyway.
                    int column = Math.Max(1, position.Column + positionDelta);
                    newSelections.Add(Selection.Collapsed(new Position(position.LineNumber, column)));
                }

                SetSelections(eventsCollector, source, newSelections, CursorChangeReason.NotSet);
            }

            return;
        }

        ExecuteEdit(
            () => ExecuteEditOperation(TypeOperations.CompositionType(
                _prevEditOperationType, _model, GetSelections(), text, replacePrevCharCnt, replaceNextCharCnt, positionDelta)),
            eventsCollector,
            source);
    }
}
