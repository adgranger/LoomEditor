// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorTypeEditOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>Typing a plain character at every cursor (Monaco: <c>SimpleCharacterTypeOperation</c>).</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static class SimpleCharacterTypeOperation
{
    public static EditOperationResult GetEdits(
        CursorConfiguration config,
        EditOperationType prevEditOperationType,
        IReadOnlyList<Selection> selections,
        string ch,
        bool isDoingComposition)
    {
        // A simple character type
        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection selection in selections)
        {
            commands.Add(config.InputMode == InputMode.Overtype && !isDoingComposition
                ? new ReplaceOvertypeCommand(selection.Range, ch)
                : new ReplaceCommand(selection.Range, ch));
        }

        EditOperationType opType = TypeEditHelpers.GetTypingOperation(ch, prevEditOperationType);
        return new EditOperationResult(
            opType,
            commands,
            TypeEditHelpers.ShouldPushStackElementBetween(prevEditOperationType, opType),
            false);
    }
}

/// <summary>Typing without looking at what is typed (Monaco: <c>TypeWithoutInterceptorsOperation</c>).</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static class TypeWithoutInterceptorsOperation
{
    public static EditOperationResult GetEdits(EditOperationType prevEditOperationType, IReadOnlyList<Selection> selections, string str)
    {
        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection selection in selections)
        {
            commands.Add(new ReplaceCommand(selection.Range, str));
        }

        EditOperationType opType = TypeEditHelpers.GetTypingOperation(str, prevEditOperationType);
        return new EditOperationResult(
            opType,
            commands,
            TypeEditHelpers.ShouldPushStackElementBetween(prevEditOperationType, opType),
            false);
    }
}

/// <summary>The free functions of <c>cursorTypeEditOperations.ts</c>.</summary>
/// <remarks>Threading: stateless; thread-safe.</remarks>
internal static class TypeEditHelpers
{
    public static EditOperationType GetTypingOperation(string typedText, EditOperationType previousTypingOperation)
    {
        if (typedText == " ")
        {
            return previousTypingOperation is EditOperationType.TypingFirstSpace or EditOperationType.TypingConsecutiveSpace
                ? EditOperationType.TypingConsecutiveSpace
                : EditOperationType.TypingFirstSpace;
        }

        return EditOperationType.TypingOther;
    }

    public static bool ShouldPushStackElementBetween(EditOperationType previousTypingOperation, EditOperationType typingOperation)
    {
        if (IsTypingOperation(previousTypingOperation) && !IsTypingOperation(typingOperation))
        {
            // Always set an undo stop before non-type operations
            return true;
        }

        if (previousTypingOperation == EditOperationType.TypingFirstSpace)
        {
            // `abc |d`: No undo stop
            // `abc  |d`: Undo stop
            return false;
        }

        // Insert undo stop between different operation types
        return NormalizeOperationType(previousTypingOperation) != NormalizeOperationType(typingOperation);
    }

    // Monaco returns the type, or the string 'space' for the two space types: -1 stands for 'space' (no type has that number).
    public static int NormalizeOperationType(EditOperationType type) =>
        type is EditOperationType.TypingConsecutiveSpace or EditOperationType.TypingFirstSpace ? -1 : (int)type;

    public static bool IsTypingOperation(EditOperationType type) =>
        type is EditOperationType.TypingOther or EditOperationType.TypingFirstSpace or EditOperationType.TypingConsecutiveSpace;

    public static IEditCommand TypeCommand(Range range, string text, bool keepPosition) =>
        keepPosition
            ? new ReplaceCommandWithoutChangingPosition(range, text, true)
            : new ReplaceCommand(range, text, true);

    public static string ShiftIndent(CursorConfiguration config, string indentation, int count = 1) =>
        ShiftCommand.ShiftIndent(indentation, indentation.Length + count, config.TabSize, config.IndentSize, config.InsertSpaces);

    public static string UnshiftIndent(CursorConfiguration config, string indentation, int count = 1) =>
        ShiftCommand.UnshiftIndent(indentation, indentation.Length + count, config.TabSize, config.IndentSize, config.InsertSpaces);
}
