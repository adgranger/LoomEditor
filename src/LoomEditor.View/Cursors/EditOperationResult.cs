// Ported from microsoft/vscode: src/vs/editor/common/cursorCommon.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>The commands an edit operation (typing, deleting...) asks the cursor to run (Monaco: <c>EditOperationResult</c>).</summary>
/// <remarks>Threading: immutable; thread-safe.</remarks>
internal sealed class EditOperationResult
{
    /// <summary>Creates a result.</summary>
    /// <param name="type">The kind of operation; an undo stop is pushed when it changes.</param>
    /// <param name="commands">The commands to run, one per cursor; an entry may be <see langword="null"/>.</param>
    /// <param name="shouldPushStackElementBefore">True to push an undo stop before running the commands.</param>
    /// <param name="shouldPushStackElementAfter">True to push an undo stop after running the commands.</param>
    /// <param name="followUp">
    /// An optional edit operation computed from the selections resulting from this one and executed right after it.
    /// It can return <see langword="null"/> to skip the follow-up edit.
    /// </param>
    public EditOperationResult(
        EditOperationType type,
        IReadOnlyList<IEditCommand?> commands,
        bool shouldPushStackElementBefore,
        bool shouldPushStackElementAfter,
        Func<IReadOnlyList<Selection>, EditOperationResult?>? followUp = null)
    {
        Type = type;
        Commands = commands;
        ShouldPushStackElementBefore = shouldPushStackElementBefore;
        ShouldPushStackElementAfter = shouldPushStackElementAfter;
        FollowUp = followUp;
    }

    public EditOperationType Type { get; }

    public IReadOnlyList<IEditCommand?> Commands { get; }

    public bool ShouldPushStackElementBefore { get; }

    public bool ShouldPushStackElementAfter { get; }

    public Func<IReadOnlyList<Selection>, EditOperationResult?>? FollowUp { get; }
}
