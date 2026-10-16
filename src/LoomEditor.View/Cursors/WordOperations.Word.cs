// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorWordOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

internal static partial class WordOperations
{
    private static WordAtPosition CreateWordAtPosition(TextModel model, int lineNumber, FindWordResult word)
    {
        var range = new Range(lineNumber, word.Start + 1, lineNumber, word.End + 1);
        return new WordAtPosition(model.GetValueInRange(range), range.StartColumn, range.EndColumn);
    }

    public static WordAtPosition? GetWordAtPosition(TextModel model, string wordSeparatorsText, IReadOnlyList<string> intlSegmenterLocales, Position position)
    {
        WordCharacterClassifier wordSeparators = WordCharacterClassifierCache.Get(wordSeparatorsText, intlSegmenterLocales);
        var simpleModel = new TextModelCursorSimpleModel(model);
        FindWordResult? prevWord = FindPreviousWordOnLine(wordSeparators, simpleModel, position);
        if (prevWord is { } prev && prev.WordType == WordType.Regular && prev.Start <= position.Column - 1 && position.Column - 1 <= prev.End)
        {
            return CreateWordAtPosition(model, position.LineNumber, prev);
        }

        FindWordResult? nextWord = FindNextWordOnLine(wordSeparators, simpleModel, position);
        if (nextWord is { } next && next.WordType == WordType.Regular && next.Start <= position.Column - 1 && position.Column - 1 <= next.End)
        {
            return CreateWordAtPosition(model, position.LineNumber, next);
        }

        return null;
    }

    /// <summary>Word selection (double click).</summary>
    public static SingleCursorState Word(CursorConfiguration config, ICursorSimpleModel model, SingleCursorState cursor, bool inSelectionMode, Position position)
    {
        WordCharacterClassifier wordSeparators = WordCharacterClassifierCache.Get(config.WordSeparators, config.WordSegmenterLocales);
        FindWordResult? prevWord = FindPreviousWordOnLine(wordSeparators, model, position);
        FindWordResult? nextWord = FindNextWordOnLine(wordSeparators, model, position);

        int column0 = position.Column - 1;

        if (!inSelectionMode)
        {
            // Entering word selection for the first time
            int startColumn;
            int endColumn;

            if (prevWord is { } prevRegular && prevRegular.WordType == WordType.Regular && prevRegular.Start <= column0 && column0 <= prevRegular.End)
            {
                // isTouchingPrevWord (Regular word)
                startColumn = prevRegular.Start + 1;
                endColumn = prevRegular.End + 1;
            }
            else if (prevWord is { } prevSeparator && prevSeparator.WordType == WordType.Separator && prevSeparator.Start <= column0 && column0 < prevSeparator.End)
            {
                // isTouchingPrevWord (Separator word) - stricter check, don't include end boundary
                startColumn = prevSeparator.Start + 1;
                endColumn = prevSeparator.End + 1;
            }
            else if (nextWord is { } nextRegular && nextRegular.WordType == WordType.Regular && nextRegular.Start <= column0 && column0 <= nextRegular.End)
            {
                // isTouchingNextWord (Regular word)
                startColumn = nextRegular.Start + 1;
                endColumn = nextRegular.End + 1;
            }
            else if (nextWord is { } nextSeparator && nextSeparator.WordType == WordType.Separator && nextSeparator.Start <= column0 && column0 < nextSeparator.End)
            {
                // isTouchingNextWord (Separator word) - stricter check, don't include end boundary
                startColumn = nextSeparator.Start + 1;
                endColumn = nextSeparator.End + 1;
            }
            else
            {
                startColumn = prevWord is { } before ? before.End + 1 : 1;
                endColumn = nextWord is { } after ? after.Start + 1 : model.GetLineMaxColumn(position.LineNumber);
            }

            return new SingleCursorState(
                new Range(position.LineNumber, startColumn, position.LineNumber, endColumn),
                SelectionStartKind.Word,
                0,
                new Position(position.LineNumber, endColumn),
                0);
        }

        int selectionStartColumn;
        int selectionEndColumn;

        if (prevWord is { } insidePrev && insidePrev.WordType == WordType.Regular && insidePrev.Start < column0 && column0 < insidePrev.End)
        {
            // isInsidePrevWord (Regular word)
            selectionStartColumn = insidePrev.Start + 1;
            selectionEndColumn = insidePrev.End + 1;
        }
        else if (nextWord is { } insideNext && insideNext.WordType == WordType.Regular && insideNext.Start < column0 && column0 < insideNext.End)
        {
            // isInsideNextWord (Regular word)
            selectionStartColumn = insideNext.Start + 1;
            selectionEndColumn = insideNext.End + 1;
        }
        else
        {
            selectionStartColumn = position.Column;
            selectionEndColumn = position.Column;
        }

        int lineNumber = position.LineNumber;
        int column;
        if (cursor.SelectionStart.Contains(position))
        {
            column = cursor.SelectionStart.EndColumn;
        }
        else if (position <= cursor.SelectionStart.Start)
        {
            column = selectionStartColumn;
            var possiblePosition = new Position(lineNumber, column);
            if (cursor.SelectionStart.Contains(possiblePosition))
            {
                column = cursor.SelectionStart.EndColumn;
            }
        }
        else
        {
            column = selectionEndColumn;
            var possiblePosition = new Position(lineNumber, column);
            if (cursor.SelectionStart.Contains(possiblePosition))
            {
                column = cursor.SelectionStart.StartColumn;
            }
        }

        return cursor.Move(true, lineNumber, column, 0);
    }
}
