namespace LoomEditor.View.Commands;

/// <summary>
/// An action the user can trigger by id (from a keyboard shortcut, a menu, the command palette or code),
/// for example <c>"editor.action.selectAll"</c>.
/// </summary>
/// <remarks>
/// <para>
/// Named <c>EditorCommand</c> rather than <c>ICommand</c> so that it is never confused with
/// <c>System.Windows.Input.ICommand</c>, which WinUI uses for buttons.
/// </para>
/// <para>Threading: <see cref="Execute"/> and <see cref="CanExecute"/> are called on the UI thread.</para>
/// </remarks>
public sealed class EditorCommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>Creates a command.</summary>
    /// <param name="id">
    /// Unique id. Reuse the Monaco id when the command exists in Monaco (e.g. <c>editor.action.selectAll</c>);
    /// otherwise use the <c>loom.</c> prefix.
    /// </param>
    /// <param name="execute">What the command does.</param>
    /// <param name="canExecute">Optional: returns false when the command cannot run right now.</param>
    public EditorCommand(string id, Action execute, Func<bool>? canExecute = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(execute);

        Id = id;
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <summary>The unique id of the command.</summary>
    public string Id { get; }

    /// <summary>True if the command can run now.</summary>
    public bool CanExecute() => _canExecute?.Invoke() ?? true;

    /// <summary>Runs the command. Does nothing if <see cref="CanExecute"/> returns false.</summary>
    /// <returns>True if the command ran.</returns>
    public bool Execute()
    {
        if (!CanExecute())
        {
            return false;
        }

        _execute();
        return true;
    }
}
