// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorTypeOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>What typing the character of a finished composition would have done (Monaco: <c>compositionEndWithInterceptors</c>).</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static class CompositionEndOperation
{
    public static EditOperationResult? GetEdits(
        EditOperationType prevEditOperationType,
        CursorConfiguration config,
        TextModel model,
        IReadOnlyList<CompositionOutcome>? compositions,
        IReadOnlyList<Selection> selections,
        IReadOnlyList<Range> autoClosedCharacters)
    {
        if (compositions is null)
        {
            // could not deduce what the composition did
            return null;
        }

        string? insertedText = null;
        foreach (CompositionOutcome composition in compositions)
        {
            if (insertedText is null)
            {
                insertedText = composition.InsertedText;
            }
            else if (insertedText != composition.InsertedText)
            {
                // not all selections agree on what was typed
                return null;
            }
        }

        if (string.IsNullOrEmpty(insertedText) || insertedText.Length != 1)
        {
            // we're only interested in the case where a single character was inserted
            return CompositionEndOvertypeOperation.GetEdits(config, compositions);
        }

        string ch = insertedText;

        bool hasDeletion = false;
        foreach (CompositionOutcome composition in compositions)
        {
            if (composition.DeletedText.Length != 0)
            {
                hasDeletion = true;
                break;
            }
        }

        if (hasDeletion)
        {
            return GetSurroundEdits(config, compositions, selections, ch);
        }

        EditOperationResult? autoClosingOvertypeEdits = AutoClosingOvertypeWithInterceptorsOperation.GetEdits(config, model, selections, autoClosedCharacters, ch);
        if (autoClosingOvertypeEdits is not null)
        {
            return autoClosingOvertypeEdits;
        }

        EditOperationResult? autoClosingOpenCharEdits = AutoClosingOpenCharTypeOperation.GetEdits(config, model, selections, ch, true, false);
        if (autoClosingOpenCharEdits is not null)
        {
            return autoClosingOpenCharEdits;
        }

        return CompositionEndOvertypeOperation.GetEdits(config, compositions);
    }

    // Check if this could have been a surround selection
    private static EditOperationResult? GetSurroundEdits(
        CursorConfiguration config,
        IReadOnlyList<CompositionOutcome> compositions,
        IReadOnlyList<Selection> selections,
        string ch)
    {
        if (!SurroundSelectionOperation.ShouldSurroundChar(config, ch) || !config.SurroundingPairs.ContainsKey(ch))
        {
            return null;
        }

        bool isTypingAQuoteCharacter = CursorCommon.IsQuote(ch);

        foreach (CompositionOutcome composition in compositions)
        {
            if (composition.DeletedSelectionStart != 0 || composition.DeletedSelectionEnd != composition.DeletedText.Length)
            {
                // more text was deleted than was selected, so this could not have been a surround selection
                return null;
            }

            if (IsOnlyBlank(composition.DeletedText))
            {
                // deleted text was only whitespace
                return null;
            }

            if (isTypingAQuoteCharacter && CursorCommon.IsQuote(composition.DeletedText))
            {
                // deleted text was a quote
                return null;
            }
        }

        var positions = new List<Position>(selections.Count);
        foreach (Selection selection in selections)
        {
            if (!selection.IsEmpty)
            {
                return null;
            }

            positions.Add(selection.Position);
        }

        if (positions.Count != compositions.Count)
        {
            return null;
        }

        var commands = new List<IEditCommand?>(positions.Count);
        for (int i = 0; i < positions.Count; i++)
        {
            commands.Add(new CompositionSurroundSelectionCommand(positions[i], compositions[i].DeletedText, config.SurroundingPairs[ch]));
        }

        return new EditOperationResult(EditOperationType.TypingOther, commands, true, false);
    }

    // Monaco: /^[ \t]+$/.test(text)
    private static bool IsOnlyBlank(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        foreach (char c in text)
        {
            if (c != ' ' && c != (char)9)
            {
                return false;
            }
        }

        return true;
    }
}
