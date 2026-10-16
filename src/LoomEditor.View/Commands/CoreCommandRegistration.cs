using LoomEditor.View.Layout;

namespace LoomEditor.View.Commands;

/// <summary>Registers the core editor commands of one editor in its <see cref="CommandRegistry"/>.</summary>
/// <remarks>
/// <para>
/// Only the commands that a key can trigger without arguments are registered. <c>_moveTo</c>, <c>_moveToSelect</c>, <c>columnSelect</c>,
/// <c>setSelection</c> and <c>cursorMove</c> need arguments: call them through <see cref="CoreNavigationCommands"/>.
/// </para>
/// <para>
/// <c>editor.action.selectAll</c> is not registered here: <c>SelectAllFeature</c> owns that id (registering it twice
/// throws). Its result is the same as Monaco's <c>selectAll</c> (one cursor selecting the whole text).
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal static class CoreCommandRegistration
{
    /// <summary>Registers the commands; dispose the result to unregister them all.</summary>
    public static IDisposable Register(CommandRegistry registry, ViewModel viewModel)
    {
        var registrations = new List<IDisposable>();

        void Add<TArgs>(CoreEditorCommand<TArgs> command, TArgs args)
            where TArgs : BaseCommandArgs =>
            registrations.Add(registry.Register(new EditorCommand(command.Id, () => command.RunCoreEditorCommand(viewModel, args))));

        var move = new CursorMoveCommandArgs();
        Add(CoreNavigationCommands.CursorLeft, move);
        Add(CoreNavigationCommands.CursorLeftSelect, move);
        Add(CoreNavigationCommands.CursorRight, move);
        Add(CoreNavigationCommands.CursorRightSelect, move);
        Add(CoreNavigationCommands.CursorUp, move);
        Add(CoreNavigationCommands.CursorUpSelect, move);
        Add(CoreNavigationCommands.CursorPageUp, move);
        Add(CoreNavigationCommands.CursorPageUpSelect, move);
        Add(CoreNavigationCommands.CursorDown, move);
        Add(CoreNavigationCommands.CursorDownSelect, move);
        Add(CoreNavigationCommands.CursorPageDown, move);
        Add(CoreNavigationCommands.CursorPageDownSelect, move);

        var baseArgs = new BaseCommandArgs();
        Add(CoreNavigationCommands.CursorHome, baseArgs);
        Add(CoreNavigationCommands.CursorHomeSelect, baseArgs);
        Add(CoreNavigationCommands.CursorLineStart, baseArgs);
        Add(CoreNavigationCommands.CursorLineStartSelect, baseArgs);
        Add(CoreNavigationCommands.CursorEnd, new EndCommandArgs());
        Add(CoreNavigationCommands.CursorEndSelect, new EndCommandArgs());
        Add(CoreNavigationCommands.CursorLineEnd, baseArgs);
        Add(CoreNavigationCommands.CursorLineEndSelect, baseArgs);
        Add(CoreNavigationCommands.CursorTop, baseArgs);
        Add(CoreNavigationCommands.CursorTopSelect, baseArgs);
        Add(CoreNavigationCommands.CursorBottom, baseArgs);
        Add(CoreNavigationCommands.CursorBottomSelect, baseArgs);
        Add(CoreNavigationCommands.CursorColumnSelectLeft, baseArgs);
        Add(CoreNavigationCommands.CursorColumnSelectRight, baseArgs);
        Add(CoreNavigationCommands.CursorColumnSelectUp, baseArgs);
        Add(CoreNavigationCommands.CursorColumnSelectPageUp, baseArgs);
        Add(CoreNavigationCommands.CursorColumnSelectDown, baseArgs);
        Add(CoreNavigationCommands.CursorColumnSelectPageDown, baseArgs);
        Add(CoreNavigationCommands.CancelSelection, baseArgs);
        Add(CoreNavigationCommands.RemoveSecondaryCursors, baseArgs);
        Add(CoreNavigationCommands.ScrollLineUp, baseArgs);
        Add(CoreNavigationCommands.ScrollPageUp, baseArgs);
        Add(CoreNavigationCommands.ScrollEditorTop, baseArgs);
        Add(CoreNavigationCommands.ScrollLineDown, baseArgs);
        Add(CoreNavigationCommands.ScrollPageDown, baseArgs);
        Add(CoreNavigationCommands.ScrollEditorBottom, baseArgs);
        Add(CoreNavigationCommands.ScrollLeft, baseArgs);
        Add(CoreNavigationCommands.ScrollRight, baseArgs);

        void AddEditing(CoreEditingCommand command) =>
            registrations.Add(registry.Register(new EditorCommand(command.Id, () => command.RunCoreEditingCommand(viewModel))));

        AddEditing(CoreEditingCommands.Undo);
        AddEditing(CoreEditingCommands.Redo);
        AddEditing(CoreEditingCommands.Outdent);
        AddEditing(CoreEditingCommands.Tab);
        AddEditing(CoreEditingCommands.LineBreakInsert);
        AddEditing(CoreEditingCommands.DeleteLeft);
        AddEditing(CoreEditingCommands.DeleteRight);

        return new CompositeDisposable(registrations);
    }

    private sealed class CompositeDisposable(List<IDisposable> items) : IDisposable
    {
        public void Dispose()
        {
            foreach (IDisposable item in items)
            {
                item.Dispose();
            }

            items.Clear();
        }
    }
}
