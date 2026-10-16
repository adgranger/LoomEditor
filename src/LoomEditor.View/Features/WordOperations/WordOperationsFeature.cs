// Ported from microsoft/vscode: src/vs/editor/contrib/wordOperations/browser/wordOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;

namespace LoomEditor.View.Features.WordOperations;

/// <summary>Adds the word move and delete commands (<c>cursorWordLeft</c>, <c>deleteWordLeft</c>...).</summary>
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

        DeleteWordCommand[] deleteCommands =
        [
            new DeleteWordLeftCommand(WordOperationsCommands.DeleteWordStartLeft, false, WordNavigationType.WordStart),
            new DeleteWordLeftCommand(WordOperationsCommands.DeleteWordEndLeft, false, WordNavigationType.WordEnd),
            new DeleteWordLeftCommand(WordOperationsCommands.DeleteWordLeft, true, WordNavigationType.WordStart),
            new DeleteWordRightCommand(WordOperationsCommands.DeleteWordStartRight, false, WordNavigationType.WordStart),
            new DeleteWordRightCommand(WordOperationsCommands.DeleteWordEndRight, false, WordNavigationType.WordEnd),
            new DeleteWordRightCommand(WordOperationsCommands.DeleteWordRight, true, WordNavigationType.WordEnd),
        ];

        foreach (DeleteWordCommand command in deleteCommands)
        {
            DeleteWordCommand captured = command;
            _registrations.Add(context.Commands.Register(new EditorCommand(captured.Id, () => captured.RunEditorCommand(viewModel))));
        }

        // Monaco: DeleteInsideWord is an EditorAction taking { onlyWord }; the command runs it with onlyWord = false.
        _registrations.Add(context.Commands.Register(new EditorCommand(WordOperationsCommands.DeleteInsideWord, () => DeleteInsideWord.Run(viewModel, onlyWord: false))));

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
