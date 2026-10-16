// Ported from microsoft/vscode: src/vs/editor/contrib/wordOperations/browser/wordOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.WordOperations;

/// <summary>A command that moves every cursor by word (Monaco: <c>MoveWordCommand</c>).</summary>
/// <remarks>Threading: UI thread only; the commands hold no state.</remarks>
internal abstract class MoveWordCommand(string id, bool inSelectionMode, WordNavigationType wordNavigationType)
{
    public string Id { get; } = id;

    public void RunEditorCommand(ViewModel viewModel)
    {
        WordCharacterClassifier wordSeparators = WordCharacterClassifierCache.Get(
            viewModel.CursorConfig.WordSeparators,
            viewModel.CursorConfig.WordSegmenterLocales);
        List<Selection> selections = viewModel.GetSelections();
        bool hasMulticursor = selections.Count > 1;

        var result = new List<Selection>(selections.Count);
        foreach (Selection sel in selections)
        {
            var inPosition = new Position(sel.PositionLineNumber, sel.PositionColumn);
            Position outPosition = Move(wordSeparators, viewModel, inPosition, wordNavigationType, hasMulticursor);
            result.Add(MoveTo(sel, outPosition, inSelectionMode));
        }

        viewModel.Model.PushStackElement();
        viewModel.SetCursorStates(
            "moveWordCommand",
            CursorChangeReason.Explicit,
            result.Select(r => (PartialCursorState)CursorState.FromModelSelection(r)).ToList());
        if (result.Count == 1)
        {
            var pos = new Position(result[0].PositionLineNumber, result[0].PositionColumn);

            // Monaco: editor.revealPosition(pos, ScrollType.Smooth)
            viewModel.RevealRange("moveWordCommand", true, Range.Collapsed(pos), VerticalRevealType.Simple, ScrollType.Smooth);
        }
    }

    private static Selection MoveTo(Selection from, Position to, bool inSelectionMode) =>
        inSelectionMode
            ? new Selection(from.SelectionStartLineNumber, from.SelectionStartColumn, to.LineNumber, to.Column) // move just position
            : new Selection(to.LineNumber, to.Column, to.LineNumber, to.Column); // move everything

    protected abstract Position Move(WordCharacterClassifier wordSeparators, ViewModel viewModel, Position position, WordNavigationType wordNavigationType, bool hasMulticursor);
}

/// <summary>Monaco: <c>WordLeftCommand</c>.</summary>
internal class WordLeftCommand(string id, bool inSelectionMode, WordNavigationType wordNavigationType)
    : MoveWordCommand(id, inSelectionMode, wordNavigationType)
{
    protected override Position Move(WordCharacterClassifier wordSeparators, ViewModel viewModel, Position position, WordNavigationType wordNavigationType, bool hasMulticursor) =>
        Cursors.WordOperations.MoveWordLeft(wordSeparators, viewModel, position, wordNavigationType, hasMulticursor);
}

/// <summary>Monaco: <c>WordRightCommand</c>.</summary>
internal class WordRightCommand(string id, bool inSelectionMode, WordNavigationType wordNavigationType)
    : MoveWordCommand(id, inSelectionMode, wordNavigationType)
{
    protected override Position Move(WordCharacterClassifier wordSeparators, ViewModel viewModel, Position position, WordNavigationType wordNavigationType, bool hasMulticursor) =>
        Cursors.WordOperations.MoveWordRight(wordSeparators, viewModel, position, wordNavigationType);
}

// The accessibility commands always use the default word separators (Monaco: EditorOptions.wordSeparators.defaultValue).
internal sealed class WordAccessibilityLeftCommand(string id, bool inSelectionMode)
    : WordLeftCommand(id, inSelectionMode, WordNavigationType.WordAccessibility)
{
    protected override Position Move(WordCharacterClassifier wordSeparators, ViewModel viewModel, Position position, WordNavigationType wordNavigationType, bool hasMulticursor) =>
        base.Move(WordCharacterClassifierCache.Get(WordHelper.UsualWordSeparators, wordSeparators.IntlSegmenterLocales), viewModel, position, wordNavigationType, hasMulticursor);
}

internal sealed class WordAccessibilityRightCommand(string id, bool inSelectionMode)
    : WordRightCommand(id, inSelectionMode, WordNavigationType.WordAccessibility)
{
    protected override Position Move(WordCharacterClassifier wordSeparators, ViewModel viewModel, Position position, WordNavigationType wordNavigationType, bool hasMulticursor) =>
        base.Move(WordCharacterClassifierCache.Get(WordHelper.UsualWordSeparators, wordSeparators.IntlSegmenterLocales), viewModel, position, wordNavigationType, hasMulticursor);
}
