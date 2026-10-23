// Inspired by microsoft/vscode: src/vs/editor/contrib/find/browser/findController.ts (CommonFindController)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Accessibility;
using LoomEditor.View.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.Find;

/// <summary>
/// Find and replace (Ctrl+F, Ctrl+H, F3…): creates the editor's <see cref="FindController"/> and registers the find commands.
/// The widget comes with the next cards.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
public sealed class FindFeature : IEditorFeature
{
    private readonly List<IDisposable> _registrations = [];
    private EditorContext? _editorContext;
    private FindController? _controller;

    /// <inheritdoc />
    public string Id => "loom.find";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ViewModel? viewModel = context.GetViewModel();
        if (viewModel is null)
        {
            return;
        }

        FindController controller = new(viewModel, context.Scheduler) { Announcer = new ContextAnnouncer(context) };
        _controller = controller;
        _editorContext = (EditorContext)context;
        _editorContext.FindController = controller;

        void Register(string id, Action run, Func<bool>? canExecute = null) =>
            _registrations.Add(context.Commands.Register(new EditorCommand(id, run, canExecute)));

        bool IsRevealed() => controller.State.IsRevealed;

        Register(FindCommands.StartFindAction, () => FindActions.StartFind(controller));
        Register(FindCommands.StartFindWithSelection, () => FindActions.StartFindWithSelection(controller));
        Register(FindCommands.NextMatchFindAction, () => FindActions.NextMatch(controller));
        Register(FindCommands.PreviousMatchFindAction, () => FindActions.PreviousMatch(controller));
        Register(FindCommands.NextSelectionMatchFindAction, () => FindActions.NextSelectionMatch(controller));
        Register(FindCommands.PreviousSelectionMatchFindAction, () => FindActions.PreviousSelectionMatch(controller));
        Register(FindCommands.StartFindReplaceAction, () => FindActions.StartFindReplace(controller));
        Register(FindCommands.CloseFindWidgetCommand, controller.CloseFindWidget, IsRevealed);
        Register(FindCommands.ToggleCaseSensitiveCommand, controller.ToggleCaseSensitive);
        Register(FindCommands.ToggleWholeWordCommand, controller.ToggleWholeWords);
        Register(FindCommands.ToggleRegexCommand, controller.ToggleRegex);
        Register(FindCommands.ToggleSearchScopeCommand, controller.ToggleSearchScope);
        Register(FindCommands.TogglePreserveCaseCommand, controller.TogglePreserveCase);
        Register(FindCommands.ReplaceOneAction, () => controller.Replace(), IsRevealed);
        Register(FindCommands.ReplaceAllAction, () => controller.ReplaceAll(), IsRevealed);
        Register(FindCommands.SelectAllMatchesAction, () => controller.SelectAllMatches(), IsRevealed);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (IDisposable registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();

        if (_editorContext is not null)
        {
            _editorContext.FindController = null;
            _editorContext = null;
        }

        _controller?.Dispose();
        _controller = null;
    }
}
