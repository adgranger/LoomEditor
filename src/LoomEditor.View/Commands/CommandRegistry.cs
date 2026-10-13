// Inspired by microsoft/vscode: src/vs/platform/commands/common/commands.ts (CommandsRegistry)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Commands;

/// <summary>
/// The list of commands available in one editor, indexed by id.
/// Features add their commands here in <c>IEditorFeature.Attach</c>.
/// </summary>
/// <remarks>
/// <para>
/// Unlike Monaco's global <c>CommandsRegistry</c>, there is one registry per editor instance:
/// two editors on the same page never share state.
/// </para>
/// <para>Threading: UI thread only. Not thread-safe.</para>
/// </remarks>
public sealed class CommandRegistry
{
    private readonly Dictionary<string, EditorCommand> _commands = new(StringComparer.Ordinal);

    /// <summary>The ids of all registered commands.</summary>
    public IReadOnlyCollection<string> Ids => _commands.Keys;

    /// <summary>
    /// Adds a command. Dispose the returned object to remove it again (typically in the feature's <c>Dispose</c>).
    /// </summary>
    /// <exception cref="InvalidOperationException">A command with the same id is already registered.</exception>
    public IDisposable Register(EditorCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_commands.TryAdd(command.Id, command))
        {
            throw new InvalidOperationException(
                $"A command with id '{command.Id}' is already registered. Command ids must be unique.");
        }

        return new Registration(this, command);
    }

    /// <summary>True if a command with this id is registered.</summary>
    public bool Contains(string id) => _commands.ContainsKey(id);

    /// <summary>
    /// Runs the command with the given id.
    /// </summary>
    /// <returns>True if the command exists and ran; false if it is unknown or cannot run now.</returns>
    public bool Execute(string id) => _commands.TryGetValue(id, out EditorCommand? command) && command.Execute();

    private void Unregister(EditorCommand command)
    {
        // Only remove the exact instance that was registered, in case the id was reused since.
        if (_commands.TryGetValue(command.Id, out EditorCommand? current) && ReferenceEquals(current, command))
        {
            _commands.Remove(command.Id);
        }
    }

    private sealed class Registration(CommandRegistry registry, EditorCommand command) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            registry.Unregister(command);
        }
    }
}
