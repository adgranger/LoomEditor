// Inspired by microsoft/vscode: src/vs/editor/contrib/indentation/browser/indentation.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.Indentation;

/// <summary>Registers the eight indentation commands.</summary>
/// <remarks>
/// The size commands (<see cref="IndentationCommands.IndentUsingTabs"/>, <see cref="IndentationCommands.IndentUsingSpaces"/>,
/// <see cref="IndentationCommands.ChangeTabDisplaySize"/>) have no quick pick and no argument: they apply the item the
/// quick pick pre-selects. Threading: UI thread only.
/// </remarks>
public sealed class IndentationFeature : IEditorFeature
{
    private readonly List<IDisposable> _registrations = [];

    /// <inheritdoc />
    public string Id => "loom.indentation";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ViewModel? viewModel = context.GetViewModel();
        if (viewModel is null)
        {
            return;
        }

        // Monaco: precondition `writable`.
        Register(context, IndentationCommands.IndentationToSpaces, () => IndentationActions.IndentationToSpaces(viewModel), viewModel);
        Register(context, IndentationCommands.IndentationToTabs, () => IndentationActions.IndentationToTabs(viewModel), viewModel);
        Register(context, IndentationCommands.ReindentLines, () => IndentationActions.ReindentLines(viewModel), viewModel);
        Register(context, IndentationCommands.ReindentSelectedLines, () => IndentationActions.ReindentSelectedLines(viewModel), viewModel);

        // Monaco: no precondition.
        Register(context, IndentationCommands.IndentUsingTabs, () => IndentationActions.ChangeIndentationSizeToPreselected(viewModel, false, false));
        Register(context, IndentationCommands.IndentUsingSpaces, () => IndentationActions.ChangeIndentationSizeToPreselected(viewModel, true, false));
        Register(context, IndentationCommands.ChangeTabDisplaySize, () => IndentationActions.ChangeIndentationSizeToPreselected(viewModel, true, true));
        Register(context, IndentationCommands.DetectIndentation, () => IndentationActions.DetectIndentation(viewModel));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
    }

    private void Register(IEditorContext context, string id, Action run, ViewModel? writableOnly = null) =>
        _registrations.Add(context.Commands.Register(writableOnly is null
            ? new EditorCommand(id, run)
            : new EditorCommand(id, run, () => !writableOnly.CursorConfig.ReadOnly)));
}
