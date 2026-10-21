// Ported from microsoft/vscode: src/vs/editor/contrib/wordOperations/browser/wordOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Cursors.Commands;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Features.WordOperations;

/// <summary>A command that deletes by word, at every cursor (Monaco: <c>DeleteWordCommand</c>).</summary>
/// <remarks>Threading: UI thread only; the commands hold no state.</remarks>
internal abstract class DeleteWordCommand(string id, bool whitespaceHeuristics, WordNavigationType wordNavigationType)
{
    public string Id { get; } = id;

    public void RunEditorCommand(ViewModel viewModel)
    {
        CursorConfiguration config = viewModel.CursorConfig;
        WordCharacterClassifier wordSeparators = WordCharacterClassifierCache.Get(config.WordSeparators, config.WordSegmenterLocales);
        List<Selection> selections = viewModel.GetSelections();

        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection sel in selections)
        {
            // Phase 4–5 cut point: Monaco also passes the auto-closing pairs of the language configuration.
            Range deleteRange = Delete(
                new DeleteWordContext(
                    wordSeparators,
                    viewModel.Model,
                    sel,
                    whitespaceHeuristics,
                    config.AutoClosingDelete,
                    config.AutoClosingBrackets,
                    config.AutoClosingQuotes,
                    viewModel.GetCursorAutoClosedCharacters()),
                wordNavigationType);
            commands.Add(new ReplaceCommand(deleteRange, string.Empty));
        }

        viewModel.Model.PushStackElement();
        viewModel.ExecuteCommands(commands, Id);
        viewModel.Model.PushStackElement();
    }

    protected abstract Range Delete(DeleteWordContext ctx, WordNavigationType wordNavigationType);
}

/// <summary>Monaco: <c>DeleteWordLeftCommand</c>.</summary>
internal class DeleteWordLeftCommand(string id, bool whitespaceHeuristics, WordNavigationType wordNavigationType)
    : DeleteWordCommand(id, whitespaceHeuristics, wordNavigationType)
{
    protected override Range Delete(DeleteWordContext ctx, WordNavigationType wordNavigationType) =>
        Cursors.WordOperations.DeleteWordLeft(ctx, wordNavigationType) ?? new Range(1, 1, 1, 1);
}

/// <summary>Monaco: <c>DeleteWordRightCommand</c>.</summary>
internal class DeleteWordRightCommand(string id, bool whitespaceHeuristics, WordNavigationType wordNavigationType)
    : DeleteWordCommand(id, whitespaceHeuristics, wordNavigationType)
{
    protected override Range Delete(DeleteWordContext ctx, WordNavigationType wordNavigationType)
    {
        Range? r = Cursors.WordOperations.DeleteWordRight(ctx, wordNavigationType);
        if (r is not null)
        {
            return r.Value;
        }

        int lineCount = ctx.Model.LineCount;
        int maxColumn = ctx.Model.GetLineMaxColumn(lineCount);
        return new Range(lineCount, maxColumn, lineCount, maxColumn);
    }
}

/// <summary>Monaco: <c>DeleteInsideWord</c> (an <c>EditorAction</c> there).</summary>
internal static class DeleteInsideWord
{
    public const string Id = "deleteInsideWord";

    /// <param name="viewModel">The view model of the editor.</param>
    /// <param name="onlyWord">Delete only the word and leave surrounding whitespace.</param>
    public static void Run(ViewModel viewModel, bool onlyWord)
    {
        WordCharacterClassifier wordSeparators = WordCharacterClassifierCache.Get(
            viewModel.CursorConfig.WordSeparators,
            viewModel.CursorConfig.WordSegmenterLocales);
        List<Selection> selections = viewModel.GetSelections();

        var commands = new List<IEditCommand?>(selections.Count);
        foreach (Selection sel in selections)
        {
            Range deleteRange = Cursors.WordOperations.DeleteInsideWord(wordSeparators, new TextModelCursorSimpleModel(viewModel.Model), sel, onlyWord);
            commands.Add(new ReplaceCommand(deleteRange, string.Empty));
        }

        viewModel.Model.PushStackElement();
        viewModel.ExecuteCommands(commands, Id);
        viewModel.Model.PushStackElement();
    }
}
