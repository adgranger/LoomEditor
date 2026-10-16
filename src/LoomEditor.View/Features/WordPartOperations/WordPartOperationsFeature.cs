// Ported from microsoft/vscode: src/vs/editor/contrib/wordPartOperations/browser/wordPartOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Commands;
using LoomEditor.View.Cursors;
using LoomEditor.View.Features.WordOperations;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.WordPartOperations;

/// <summary>Adds the word part commands: move and delete inside <c>camelCase</c> and <c>snake_case</c> identifiers.</summary>
/// <remarks>
/// Phase 3 cut points: <c>precondition</c> and the keybindings of Monaco's registrations are not ported (P3-39).
/// Monaco's command aliases <c>cursorWordPartStartLeft</c> and <c>cursorWordPartStartLeftSelect</c> are registered too.
/// Threading: UI thread only.
/// </remarks>
public sealed class WordPartOperationsFeature : IEditorFeature
{
    // Monaco: CommandsRegistry.registerCommandAlias, "for compatibility purposes"
    private const string CursorWordPartStartLeftAlias = "cursorWordPartStartLeft";
    private const string CursorWordPartStartLeftSelectAlias = "cursorWordPartStartLeftSelect";

    private readonly List<IDisposable> _registrations = [];

    /// <inheritdoc />
    public string Id => "loom.wordPartOperations";

    /// <inheritdoc />
    public void Attach(IEditorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ViewModel? viewModel = context.GetViewModel();
        if (viewModel is null)
        {
            return;
        }

        Register(context, WordPartOperationsCommands.DeleteWordPartLeft, () => new DeleteWordPartLeft().RunEditorCommand(viewModel));
        Register(context, WordPartOperationsCommands.DeleteWordPartRight, () => new DeleteWordPartRight().RunEditorCommand(viewModel));

        var cursorWordPartLeft = new WordPartLeftCommand(WordPartOperationsCommands.CursorWordPartLeft, false);
        var cursorWordPartLeftSelect = new WordPartLeftCommand(WordPartOperationsCommands.CursorWordPartLeftSelect, true);
        Register(context, cursorWordPartLeft.Id, () => cursorWordPartLeft.RunEditorCommand(viewModel));
        Register(context, CursorWordPartStartLeftAlias, () => cursorWordPartLeft.RunEditorCommand(viewModel));
        Register(context, cursorWordPartLeftSelect.Id, () => cursorWordPartLeftSelect.RunEditorCommand(viewModel));
        Register(context, CursorWordPartStartLeftSelectAlias, () => cursorWordPartLeftSelect.RunEditorCommand(viewModel));

        var cursorWordPartRight = new WordPartRightCommand(WordPartOperationsCommands.CursorWordPartRight, false);
        var cursorWordPartRightSelect = new WordPartRightCommand(WordPartOperationsCommands.CursorWordPartRightSelect, true);
        Register(context, cursorWordPartRight.Id, () => cursorWordPartRight.RunEditorCommand(viewModel));
        Register(context, cursorWordPartRightSelect.Id, () => cursorWordPartRightSelect.RunEditorCommand(viewModel));
    }

    private void Register(IEditorContext context, string id, Action run) =>
        _registrations.Add(context.Commands.Register(new EditorCommand(id, run)));

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

/// <summary>Monaco: <c>DeleteWordPartLeft</c>.</summary>
internal sealed class DeleteWordPartLeft()
    : DeleteWordCommand(WordPartOperationsCommands.DeleteWordPartLeft, true, WordNavigationType.WordStart)
{
    protected override Range Delete(DeleteWordContext ctx, WordNavigationType wordNavigationType) =>
        Cursors.WordPartOperations.DeleteWordPartLeft(ctx) ?? new Range(1, 1, 1, 1);
}

/// <summary>Monaco: <c>DeleteWordPartRight</c>.</summary>
internal sealed class DeleteWordPartRight()
    : DeleteWordCommand(WordPartOperationsCommands.DeleteWordPartRight, true, WordNavigationType.WordEnd)
{
    protected override Range Delete(DeleteWordContext ctx, WordNavigationType wordNavigationType)
    {
        Range? r = Cursors.WordPartOperations.DeleteWordPartRight(ctx);
        if (r is not null)
        {
            return r.Value;
        }

        int lineCount = ctx.Model.LineCount;
        int maxColumn = ctx.Model.GetLineMaxColumn(lineCount);
        return new Range(lineCount, maxColumn, lineCount, maxColumn);
    }
}

/// <summary>Monaco: <c>WordPartLeftCommand</c> (with <c>CursorWordPartLeft(Select)</c>).</summary>
internal sealed class WordPartLeftCommand(string id, bool inSelectionMode)
    : MoveWordCommand(id, inSelectionMode, WordNavigationType.WordStart)
{
    protected override Position Move(WordCharacterClassifier wordSeparators, ViewModel viewModel, Position position, WordNavigationType wordNavigationType, bool hasMulticursor) =>
        Cursors.WordPartOperations.MoveWordPartLeft(wordSeparators, viewModel, position, hasMulticursor);
}

/// <summary>Monaco: <c>WordPartRightCommand</c> (with <c>CursorWordPartRight(Select)</c>).</summary>
internal sealed class WordPartRightCommand(string id, bool inSelectionMode)
    : MoveWordCommand(id, inSelectionMode, WordNavigationType.WordEnd)
{
    protected override Position Move(WordCharacterClassifier wordSeparators, ViewModel viewModel, Position position, WordNavigationType wordNavigationType, bool hasMulticursor) =>
        Cursors.WordPartOperations.MoveWordPartRight(wordSeparators, viewModel, position);
}
