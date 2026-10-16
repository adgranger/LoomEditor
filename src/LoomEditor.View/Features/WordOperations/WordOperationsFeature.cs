// Ported from microsoft/vscode: src/vs/editor/contrib/wordOperations/browser/wordOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;

namespace LoomEditor.View.Features.WordOperations;

/// <summary>Adds the word move commands (<c>cursorWordLeft</c>, <c>cursorWordEndRight</c>...).</summary>
/// <remarks>
/// Phase 3 cut points: <c>precondition</c> and the keybindings of Monaco's registrations are not ported (P3-39).
/// Threading: UI thread only.
/// </remarks>
public sealed class WordOperationsFeature : IEditorFeature
{
    private readonly List<IDisposable> _registrations = [];

    /// <inheritdoc />
    public string Id => "loom.wordOperations";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Layout.ViewModel? viewModel = context.GetViewModel();
        if (viewModel is null)
        {
            return;
        }

        MoveWordCommand[] commands =
        [
            new WordLeftCommand(WordOperationsCommands.CursorWordStartLeft, false, WordNavigationType.WordStart),
            new WordLeftCommand(WordOperationsCommands.CursorWordEndLeft, false, WordNavigationType.WordEnd),
            new WordLeftCommand(WordOperationsCommands.CursorWordLeft, false, WordNavigationType.WordStartFast),
            new WordLeftCommand(WordOperationsCommands.CursorWordStartLeftSelect, true, WordNavigationType.WordStart),
            new WordLeftCommand(WordOperationsCommands.CursorWordEndLeftSelect, true, WordNavigationType.WordEnd),
            new WordLeftCommand(WordOperationsCommands.CursorWordLeftSelect, true, WordNavigationType.WordStartFast),
            new WordAccessibilityLeftCommand(WordOperationsCommands.CursorWordAccessibilityLeft, false),
            new WordAccessibilityLeftCommand(WordOperationsCommands.CursorWordAccessibilityLeftSelect, true),
            new WordRightCommand(WordOperationsCommands.CursorWordStartRight, false, WordNavigationType.WordStart),
            new WordRightCommand(WordOperationsCommands.CursorWordEndRight, false, WordNavigationType.WordEnd),
            new WordRightCommand(WordOperationsCommands.CursorWordRight, false, WordNavigationType.WordEnd),
            new WordRightCommand(WordOperationsCommands.CursorWordStartRightSelect, true, WordNavigationType.WordStart),
            new WordRightCommand(WordOperationsCommands.CursorWordEndRightSelect, true, WordNavigationType.WordEnd),
            new WordRightCommand(WordOperationsCommands.CursorWordRightSelect, true, WordNavigationType.WordEnd),
            new WordAccessibilityRightCommand(WordOperationsCommands.CursorWordAccessibilityRight, false),
            new WordAccessibilityRightCommand(WordOperationsCommands.CursorWordAccessibilityRightSelect, true),
        ];

        foreach (MoveWordCommand command in commands)
        {
            MoveWordCommand captured = command;
            _registrations.Add(context.Commands.Register(new EditorCommand(captured.Id, () => captured.RunEditorCommand(viewModel))));
        }
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
}
