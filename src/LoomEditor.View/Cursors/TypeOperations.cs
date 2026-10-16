// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorTypeOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>The entry points of typing (Monaco: <c>TypeOperations</c>); only the plain-text part is ported so far.</summary>
/// <remarks>
/// The <c>autoClosedCharacters</c> parameter of Monaco's <c>typeWithInterceptors</c> is not ported: only the auto-closing
/// interceptors read it (phases 4–5).
/// Threading: stateless; UI thread only.
/// </remarks>
internal static class TypeOperations
{
    /// <summary>The commands that indent the lines of each selection (Monaco: <c>indent</c>); none without a model or selections.</summary>
    public static List<IEditCommand?> Indent(CursorConfiguration config, ICursorSimpleModel? model, IReadOnlyList<Selection>? selections)
    {
        if (model is null || selections is null)
        {
            return [];
        }

        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection selection in selections)
        {
            commands.Add(new ShiftCommand(selection, ShiftOptions(config, false)));
        }

        return commands;
    }

    /// <summary>The commands that outdent the lines of each selection (Monaco: <c>outdent</c>).</summary>
    public static List<IEditCommand?> Outdent(CursorConfiguration config, ICursorSimpleModel model, IReadOnlyList<Selection> selections)
    {
        ArgumentNullException.ThrowIfNull(model);
        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection selection in selections)
        {
            commands.Add(new ShiftCommand(selection, ShiftOptions(config, true)));
        }

        return commands;
    }

    /// <summary>The edits of a paste (Monaco: <c>paste</c>).</summary>
    public static EditOperationResult Paste(
        CursorConfiguration config,
        IReadOnlyList<Selection> selections,
        string text,
        bool pasteOnNewLine,
        IReadOnlyList<string> multicursorText,
        bool isBlock = false) =>
        PasteOperation.GetEdits(config, selections, text, pasteOnNewLine, multicursorText, isBlock);

    /// <summary>The commands of the Tab key (Monaco: <c>tab</c>).</summary>
    public static List<IEditCommand?> Tab(CursorConfiguration config, TextModel model, IReadOnlyList<Selection> selections) =>
        TabOperation.GetCommands(config, model, selections);

    /// <summary>The edits of one step of an IME composition (Monaco: <c>compositionType</c>).</summary>
    public static EditOperationResult CompositionType(
        EditOperationType prevEditOperationType,
        TextModel model,
        IReadOnlyList<Selection> selections,
        string text,
        int replacePrevCharCnt,
        int replaceNextCharCnt,
        int positionDelta) =>
        CompositionOperation.GetEdits(prevEditOperationType, model, selections, text, replacePrevCharCnt, replaceNextCharCnt, positionDelta);

    /// <summary>
    /// The edits to make when a composition ends: typing, but the character is already in the text buffer (Monaco:
    /// <c>compositionEndWithInterceptors</c>). <see langword="null"/> when there is nothing to do.
    /// </summary>
    public static EditOperationResult? CompositionEndWithInterceptors(CursorConfiguration config, IReadOnlyList<CompositionOutcome>? compositions)
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

        foreach (CompositionOutcome composition in compositions)
        {
            if (composition.DeletedText.Length != 0)
            {
                // Phase 4–5 cut point: Monaco checks here whether the composition was a surround selection
                // (shouldSurroundChar, config.surroundingPairs). Without a language configuration it returns null.
                return null;
            }
        }

        // Phase 4–5 cut point: AutoClosingOvertypeWithInterceptorsOperation and AutoClosingOpenCharTypeOperation
        // (they return undefined without a language configuration).
        return CompositionEndOvertypeOperation.GetEdits(config, compositions);
    }

    private static ShiftCommandOptions ShiftOptions(CursorConfiguration config, bool isUnshift) =>
        new(isUnshift, config.TabSize, config.IndentSize, config.InsertSpaces, config.UseTabStops, config.AutoIndent);

    /// <summary>Indents <paramref name="indentation"/> by <paramref name="count"/> steps (Monaco: <c>shiftIndent</c>).</summary>
    public static string ShiftIndent(CursorConfiguration config, string indentation, int count = 1) =>
        TypeEditHelpers.ShiftIndent(config, indentation, count);

    /// <summary>Outdents <paramref name="indentation"/> by <paramref name="count"/> steps (Monaco: <c>unshiftIndent</c>).</summary>
    public static string UnshiftIndent(CursorConfiguration config, string indentation, int count = 1) =>
        TypeEditHelpers.UnshiftIndent(config, indentation, count);

    /// <summary>The edits for one typed character coming from the keyboard (Monaco: <c>typeWithInterceptors</c>).</summary>
    public static EditOperationResult TypeWithInterceptors(
        bool isDoingComposition,
        EditOperationType prevEditOperationType,
        CursorConfiguration config,
        TextModel model,
        IReadOnlyList<Selection> selections,
        string ch)
    {
        EditOperationResult? enterEdits = EnterOperation.GetEdits(config, model, selections, ch, isDoingComposition);
        if (enterEdits is not null)
        {
            return enterEdits;
        }

        // Phase 4–5 cut point: AutoIndentOperation (cursorTypeEditOperations.ts 29–113)
        // Phase 4–5 cut point: AutoClosingOvertypeOperation (115–152)
        // Phase 4–5 cut point: AutoClosingOpenCharTypeOperation (154–404)
        // Phase 4–5 cut point: SurroundSelectionOperation (421–478)
        // Phase 4–5 cut point: InterceptorElectricCharOperation (480–546)
        return SimpleCharacterTypeOperation.GetEdits(config, prevEditOperationType, selections, ch, isDoingComposition);
    }

    /// <summary>The edits for a text typed or pasted without looking at its characters (Monaco: <c>typeWithoutInterceptors</c>).</summary>
    public static EditOperationResult TypeWithoutInterceptors(EditOperationType prevEditOperationType, IReadOnlyList<Selection> selections, string str) =>
        TypeWithoutInterceptorsOperation.GetEdits(prevEditOperationType, selections, str);
}
