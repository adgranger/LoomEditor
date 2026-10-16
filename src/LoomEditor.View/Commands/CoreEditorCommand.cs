// Ported from microsoft/vscode: src/vs/editor/browser/coreCommands.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Commands;

/// <summary>A command that runs on the view model (and its cursors) (Monaco: <c>CoreEditorCommand</c>).</summary>
/// <typeparam name="TArgs">The arguments of the command.</typeparam>
/// <remarks>
/// Monaco's <c>runEditorCommand</c> (which finds the view model of an editor) is not ported: the command registration
/// binds each command to the view model of its editor. Threading: UI thread only; the commands hold no state.
/// </remarks>
internal abstract class CoreEditorCommand<TArgs>(string id)
    where TArgs : BaseCommandArgs
{
    /// <summary>The Monaco id of the command, for example <c>cursorLeft</c>.</summary>
    public string Id { get; } = id;

    public abstract void RunCoreEditorCommand(ViewModel viewModel, TArgs args);
}

/// <summary>Monaco: <c>CoreNavigationCommands.BaseCommandOptions</c>.</summary>
/// <param name="Source">Who asked for the move, for example <c>"mouse"</c> or <c>"keyboard"</c>.</param>
internal record BaseCommandArgs(string? Source = null);

/// <summary>Monaco: <c>NavigationCommandRevealType</c>.</summary>
internal enum NavigationCommandRevealType
{
    /// <summary>Do regular revealing.</summary>
    Regular = 0,

    /// <summary>Do only the minimum revealing.</summary>
    Minimal = 1,

    /// <summary>Do not reveal the position.</summary>
    None = 2,
}

/// <summary>Monaco: <c>CoreNavigationCommands.MoveCommandOptions</c> (with <c>revealType</c> defaulting to <c>Regular</c>).</summary>
internal sealed record MoveCommandArgs(
    Position Position,
    Position? ViewPosition = null,
    NavigationCommandRevealType RevealType = NavigationCommandRevealType.Regular,
    string? Source = null) : BaseCommandArgs(Source);

/// <summary>Monaco: <c>CoreNavigationCommands.CursorMoveCommandOptions</c>.</summary>
/// <param name="PageSize">The page size for the page up and down commands; null or 0 uses the configuration's.</param>
/// <param name="Source">Who asked for the move.</param>
internal sealed record CursorMoveCommandArgs(int? PageSize = null, string? Source = null) : BaseCommandArgs(Source);

/// <summary>Monaco: <c>CoreNavigationCommands.EndCommandOptions</c>.</summary>
internal sealed record EndCommandArgs(bool Sticky = false, string? Source = null) : BaseCommandArgs(Source);

/// <summary>Monaco: <c>CoreNavigationCommands.SetSelectionCommandOptions</c>.</summary>
internal sealed record SetSelectionCommandArgs(Selection Selection, string? Source = null) : BaseCommandArgs(Source);

/// <summary>The arguments of <c>cursorMove</c>: Monaco's <c>CursorMove.RawArguments</c> and the <c>source</c>.</summary>
internal sealed record CursorMoveRunArgs(CursorMoveArguments Arguments, string? Source = null) : BaseCommandArgs(Source);
