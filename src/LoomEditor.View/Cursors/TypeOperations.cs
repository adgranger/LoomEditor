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

    /// <summary>The commands of the Tab key (Monaco: <c>tab</c>).</summary>
    public static List<IEditCommand?> Tab(CursorConfiguration config, TextModel model, IReadOnlyList<Selection> selections) =>
        TabOperation.GetCommands(config, model, selections);

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
