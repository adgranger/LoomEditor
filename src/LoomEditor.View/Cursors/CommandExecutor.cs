// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursor.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>
/// Runs one <see cref="IEditCommand"/> per cursor as a single undoable model edit, drops the cursors whose edits
/// overlap those of another one, and returns the new selections.
/// </summary>
/// <remarks>
/// <para>
/// Monaco: <c>CommandExecutor</c>. The <c>editReason</c> parameter is not ported. A command that throws is ignored
/// (its operations are dropped), like Monaco, which reports the error with <c>onUnexpectedError</c>.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal static partial class CommandExecutor
{
    // Monaco: IExecContext
    private sealed class ExecContext(TextModel model, IReadOnlyList<Selection> selectionsBefore)
    {
        public TextModel Model { get; } = model;

        public IReadOnlyList<Selection> SelectionsBefore { get; } = selectionsBefore;

        public List<string?> TrackedRanges { get; } = [];

        public List<SelectionDirection> TrackedRangesDirection { get; } = [];
    }

    // Monaco: ICommandData and ICommandsData
    private sealed record CommandsData(List<IdentifiedEditOperation> Operations, bool HadTrackedEditOperation);

    /// <summary>Executes the commands, one per cursor; a <see langword="null"/> command leaves its cursor alone.</summary>
    /// <returns>The selections after the edit (without the cursors that lost), or <see langword="null"/> when nothing was executed.</returns>
    public static IReadOnlyList<Selection>? ExecuteCommands(
        TextModel model,
        IReadOnlyList<Selection> selectionsBefore,
        IReadOnlyList<IEditCommand?> commands)
    {
        var ctx = new ExecContext(model, selectionsBefore);

        IReadOnlyList<Selection>? result = InnerExecuteCommands(ctx, commands);

        for (int i = 0, len = ctx.TrackedRanges.Count; i < len; i++)
        {
            ctx.Model.SetTrackedRange(ctx.TrackedRanges[i], null, TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);
        }

        return result;
    }

    private static List<Selection>? InnerExecuteCommands(ExecContext ctx, IReadOnlyList<IEditCommand?> commands)
    {
        if (ArrayIsEmpty(commands))
        {
            return null;
        }

        CommandsData commandsData = GetEditOperations(ctx, commands);
        if (commandsData.Operations.Count == 0)
        {
            return null;
        }

        List<IdentifiedEditOperation> rawOperations = commandsData.Operations;

        // Monaco's map is keyed by the string of the major identifier; here a set of majors.
        HashSet<int> loserCursors = GetLoserCursorMap(rawOperations);
        if (loserCursors.Contains(0))
        {
            // These commands are very messed up
            return null;
        }

        // Remove operations belonging to losing cursors
        var filteredOperations = new List<IdentifiedEditOperation>();
        foreach (IdentifiedEditOperation operation in rawOperations)
        {
            if (!loserCursors.Contains(operation.Identifier!.Value.Major))
            {
                filteredOperations.Add(operation);
            }
        }

        // Give the hint that edit operations are tracked to the model
        if (commandsData.HadTrackedEditOperation && filteredOperations.Count > 0)
        {
            filteredOperations[0] = filteredOperations[0] with { IsTracked = true };
        }

        IReadOnlyList<Selection>? selectionsAfter = ctx.Model.PushEditOperations(
            ctx.SelectionsBefore,
            filteredOperations,
            inverseEditOperations => ComputeCursorStates(ctx, commands, inverseEditOperations));

        // Monaco mutates the array in place; here a new list is always built.
        var result = new List<Selection>(selectionsAfter ?? ctx.SelectionsBefore);

        // Remove losing cursors, in descending order of index
        var losingCursors = new List<int>(loserCursors);
        losingCursors.Sort(static (a, b) => b.CompareTo(a));
        foreach (int losingCursor in losingCursors)
        {
            result.RemoveAt(losingCursor);
        }

        return result;
    }

    // Monaco: the cursor state computer given to pushEditOperations
    private static Selection[] ComputeCursorStates(
        ExecContext ctx,
        IReadOnlyList<IEditCommand?> commands,
        IReadOnlyList<ValidEditOperation> inverseEditOperations)
    {
        int cursorCount = ctx.SelectionsBefore.Count;
        var groupedInverseEditOperations = new List<ValidEditOperation>[cursorCount];
        for (int i = 0; i < cursorCount; i++)
        {
            groupedInverseEditOperations[i] = [];
        }

        foreach (ValidEditOperation op in inverseEditOperations)
        {
            if (op.Identifier is not { } identifier)
            {
                // perhaps auto whitespace trim edits
                continue;
            }

            groupedInverseEditOperations[identifier.Major].Add(op);
        }

        var cursorSelections = new Selection[cursorCount];
        for (int i = 0; i < cursorCount; i++)
        {
            if (groupedInverseEditOperations[i].Count > 0)
            {
                // JavaScript's sort is stable and so is OrderBy
                List<ValidEditOperation> sorted = groupedInverseEditOperations[i].OrderBy(static op => op.Identifier!.Value.Minor).ToList();
                cursorSelections[i] = commands[i]!.ComputeCursorState(ctx.Model, new CursorStateComputerData(ctx, sorted));
            }
            else
            {
                cursorSelections[i] = ctx.SelectionsBefore[i];
            }
        }

        return cursorSelections;
    }

    private sealed class CursorStateComputerData(ExecContext ctx, IReadOnlyList<ValidEditOperation> inverseEditOperations) : ICursorStateComputerData
    {
        public IReadOnlyList<ValidEditOperation> GetInverseEditOperations() => inverseEditOperations;

        public Selection GetTrackedSelection(string id)
        {
            int idx = int.Parse(id, CultureInfo.InvariantCulture);
            Range range = ctx.Model.GetTrackedRange(ctx.TrackedRanges[idx]!)!.Value;
            return Selection.FromRange(range, ctx.TrackedRangesDirection[idx]);
        }
    }

    private static bool ArrayIsEmpty(IReadOnlyList<IEditCommand?> commands)
    {
        for (int i = 0, len = commands.Count; i < len; i++)
        {
            if (commands[i] is not null)
            {
                return false;
            }
        }

        return true;
    }

    private static CommandsData GetEditOperations(ExecContext ctx, IReadOnlyList<IEditCommand?> commands)
    {
        var operations = new List<IdentifiedEditOperation>();
        bool hadTrackedEditOperation = false;

        for (int i = 0, len = commands.Count; i < len; i++)
        {
            IEditCommand? command = commands[i];
            if (command is not null)
            {
                CommandsData r = GetEditOperationsFromCommand(ctx, i, command);
                operations.AddRange(r.Operations);
                hadTrackedEditOperation = hadTrackedEditOperation || r.HadTrackedEditOperation;
            }
        }

        return new CommandsData(operations, hadTrackedEditOperation);
    }

    private static CommandsData GetEditOperationsFromCommand(ExecContext ctx, int majorIdentifier, IEditCommand command)
    {
        // This method acts as a transaction, if the command fails
        // everything it has done is ignored
        var builder = new EditOperationBuilder(ctx, majorIdentifier, command);

        try
        {
            command.GetEditOperations(ctx.Model, builder);
        }
        catch (Exception)
        {
            return new CommandsData([], false);
        }

        return new CommandsData(builder.Operations, builder.HadTrackedEditOperation);
    }

    // Monaco: the editOperationBuilder object literal of _getEditOperationsFromCommand
    private sealed class EditOperationBuilder(ExecContext ctx, int majorIdentifier, IEditCommand command) : IEditOperationBuilder
    {
        private int _operationMinor;

        public List<IdentifiedEditOperation> Operations { get; } = [];

        public bool HadTrackedEditOperation { get; private set; }

        public void AddEditOperation(Range range, string? text, bool forceMoveMarkers = false)
        {
            if (range.IsEmpty && text == string.Empty)
            {
                // This command wants to add a no-op => no thank you
                return;
            }

            Operations.Add(new IdentifiedEditOperation(
                range,
                text,
                new EditOperationIdentifier(majorIdentifier, _operationMinor++),
                forceMoveMarkers,
                command.InsertsAutoWhitespace));
        }

        public void AddTrackedEditOperation(Range range, string? text, bool forceMoveMarkers = false)
        {
            HadTrackedEditOperation = true;
            AddEditOperation(range, text, forceMoveMarkers);
        }

        public string TrackSelection(Selection selection, bool? trackPreviousOnEmpty = null)
        {
            TrackedRangeStickiness stickiness;
            if (selection.IsEmpty)
            {
                if (trackPreviousOnEmpty is { } trackPrevious)
                {
                    stickiness = trackPrevious
                        ? TrackedRangeStickiness.GrowsOnlyWhenTypingBefore
                        : TrackedRangeStickiness.GrowsOnlyWhenTypingAfter;
                }
                else
                {
                    // Try to lock it with surrounding text
                    int maxLineColumn = ctx.Model.GetLineMaxColumn(selection.Range.StartLineNumber);
                    stickiness = selection.Range.StartColumn == maxLineColumn
                        ? TrackedRangeStickiness.GrowsOnlyWhenTypingBefore
                        : TrackedRangeStickiness.GrowsOnlyWhenTypingAfter;
                }
            }
            else
            {
                stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges;
            }

            int l = ctx.TrackedRanges.Count;
            string? id = ctx.Model.SetTrackedRange(null, selection.Range, stickiness);
            ctx.TrackedRanges.Add(id);
            ctx.TrackedRangesDirection.Add(selection.Direction);
            return l.ToString(CultureInfo.InvariantCulture);
        }
    }
}
