// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorTypeEditOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>The end of a composition in overtype mode (Monaco: <c>CompositionEndOvertypeOperation</c>).</summary>
/// <remarks>Without overtype mode (always, in phase 3) it makes no edit. Threading: stateless; UI thread only.</remarks>
internal static class CompositionEndOvertypeOperation
{
    public static EditOperationResult? GetEdits(CursorConfiguration config, IReadOnlyList<CompositionOutcome> compositions)
    {
        if (config.InputMode != EditorInputMode.Overtype)
        {
            return null;
        }

        var commands = new List<IEditCommand?>(compositions.Count);
        foreach (CompositionOutcome composition in compositions)
        {
            commands.Add(new ReplaceOvertypeCommandOnCompositionEnd(composition.InsertedTextRange));
        }

        return new EditOperationResult(EditOperationType.TypingOther, commands, true, false);
    }
}

/// <summary>One step of a composition: replace the characters around each cursor (Monaco: <c>CompositionOperation</c>).</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static class CompositionOperation
{
    public static EditOperationResult GetEdits(
        EditOperationType prevEditOperationType,
        TextModel model,
        IReadOnlyList<Selection> selections,
        string text,
        int replacePrevCharCnt,
        int replaceNextCharCnt,
        int positionDelta)
    {
        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection selection in selections)
        {
            commands.Add(CompositionType(model, selection, text, replacePrevCharCnt, replaceNextCharCnt, positionDelta));
        }

        return new EditOperationResult(
            EditOperationType.TypingOther,
            commands,
            TypeEditHelpers.ShouldPushStackElementBetween(prevEditOperationType, EditOperationType.TypingOther),
            false);
    }

    private static ReplaceCommandWithOffsetCursorState? CompositionType(
        TextModel model,
        Selection selection,
        string text,
        int replacePrevCharCnt,
        int replaceNextCharCnt,
        int positionDelta)
    {
        if (!selection.IsEmpty)
        {
            // looks like https://github.com/microsoft/vscode/issues/2773
            // where a cursor operation occurred before a canceled composition
            // => ignore composition
            return null;
        }

        Position pos = selection.Position;
        int startColumn = Math.Max(1, pos.Column - replacePrevCharCnt);
        int endColumn = Math.Min(model.GetLineMaxColumn(pos.LineNumber), pos.Column + replaceNextCharCnt);
        var range = new Range(pos.LineNumber, startColumn, pos.LineNumber, endColumn);
        return new ReplaceCommandWithOffsetCursorState(range, text, 0, positionDelta);
    }
}
