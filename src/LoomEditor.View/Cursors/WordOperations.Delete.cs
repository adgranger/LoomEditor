// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorWordOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

internal static partial class WordOperations
{
    private static Range? DeleteWordLeftWhitespace(TextModelCursorSimpleModel model, Position position)
    {
        string lineContent = model.GetLineContent(position.LineNumber);
        int startIndex = position.Column - 2;
        int lastNonWhitespace = Strings.LastNonWhitespaceIndex(lineContent, startIndex);
        if (lastNonWhitespace + 1 < startIndex)
        {
            return new Range(position.LineNumber, lastNonWhitespace + 2, position.LineNumber, position.Column);
        }

        return null;
    }

    public static Range? DeleteWordLeft(DeleteWordContext ctx, WordNavigationType wordNavigationType)
    {
        WordCharacterClassifier wordSeparators = ctx.WordSeparators;
        var model = new TextModelCursorSimpleModel(ctx.Model);
        Selection selection = ctx.Selection;
        bool whitespaceHeuristics = ctx.WhitespaceHeuristics;

        if (!selection.IsEmpty)
        {
            return selection.Range;
        }

        // Phase 4–5 cut point: Monaco returns (line, column - 1) -> (line, column + 1) here when
        // DeleteOperations.isAutoClosingPairDelete(...) is true (it needs the auto-closing pairs of the language).
        var position = new Position(selection.PositionLineNumber, selection.PositionColumn);

        int lineNumber = position.LineNumber;
        int column = position.Column;

        if (lineNumber == 1 && column == 1)
        {
            // Ignore deleting at beginning of file
            return null;
        }

        if (whitespaceHeuristics)
        {
            Range? r = DeleteWordLeftWhitespace(model, position);
            if (r is not null)
            {
                return r;
            }
        }

        FindWordResult? prevWordOnLine = FindPreviousWordOnLine(wordSeparators, model, position);

        if (wordNavigationType == WordNavigationType.WordStart)
        {
            if (prevWordOnLine is { } start)
            {
                column = start.Start + 1;
            }
            else if (column > 1)
            {
                column = 1;
            }
            else
            {
                lineNumber--;
                column = model.GetLineMaxColumn(lineNumber);
            }
        }
        else
        {
            if (prevWordOnLine is { } ending && column <= ending.End + 1)
            {
                prevWordOnLine = FindPreviousWordOnLine(wordSeparators, model, new Position(lineNumber, ending.Start + 1));
            }

            if (prevWordOnLine is { } end)
            {
                column = end.End + 1;
            }
            else if (column > 1)
            {
                column = 1;
            }
            else
            {
                lineNumber--;
                column = model.GetLineMaxColumn(lineNumber);
            }
        }

        return new Range(lineNumber, column, position.LineNumber, position.Column);
    }

    public static Range DeleteInsideWord(WordCharacterClassifier wordSeparators, ICursorSimpleModel model, Selection selection, bool onlyWord = false)
    {
        if (!selection.IsEmpty)
        {
            return selection.Range;
        }

        var position = new Position(selection.PositionLineNumber, selection.PositionColumn);

        Range? r = DeleteInsideWordWhitespace(model, position);
        if (r is not null)
        {
            return r.Value;
        }

        return DeleteInsideWordDetermineDeleteRange(wordSeparators, model, position, onlyWord);
    }

    private static bool CharAtIsWhitespace(string str, int index)
    {
        // Monaco: charCodeAt is NaN out of range, which is not a space or a tab.
        if (index < 0 || index >= str.Length)
        {
            return false;
        }

        char charCode = str[index];
        return charCode == ' ' || charCode == '\t';
    }

    private static Range? DeleteInsideWordWhitespace(ICursorSimpleModel model, Position position)
    {
        string lineContent = model.GetLineContent(position.LineNumber);
        int lineContentLength = lineContent.Length;

        if (lineContentLength == 0)
        {
            // empty line
            return null;
        }

        int leftIndex = Math.Max(position.Column - 2, 0);
        if (!CharAtIsWhitespace(lineContent, leftIndex))
        {
            // touches a non-whitespace character to the left
            return null;
        }

        int rightIndex = Math.Min(position.Column - 1, lineContentLength - 1);
        if (!CharAtIsWhitespace(lineContent, rightIndex))
        {
            // touches a non-whitespace character to the right
            return null;
        }

        // walk over whitespace to the left
        while (leftIndex > 0 && CharAtIsWhitespace(lineContent, leftIndex - 1))
        {
            leftIndex--;
        }

        // walk over whitespace to the right
        while (rightIndex + 1 < lineContentLength && CharAtIsWhitespace(lineContent, rightIndex + 1))
        {
            rightIndex++;
        }

        return new Range(position.LineNumber, leftIndex + 1, position.LineNumber, rightIndex + 2);
    }

    private static Range DeleteInsideWordDetermineDeleteRange(WordCharacterClassifier wordSeparators, ICursorSimpleModel model, Position position, bool onlyWord)
    {
        string lineContent = model.GetLineContent(position.LineNumber);
        int lineLength = lineContent.Length;
        if (lineLength == 0)
        {
            // empty line
            if (position.LineNumber > 1)
            {
                return new Range(position.LineNumber - 1, model.GetLineMaxColumn(position.LineNumber - 1), position.LineNumber, 1);
            }

            if (position.LineNumber < model.GetLineCount())
            {
                return new Range(position.LineNumber, 1, position.LineNumber + 1, 1);
            }

            // empty model
            return new Range(position.LineNumber, 1, position.LineNumber, 1);
        }

        bool TouchesWord(FindWordResult word) => word.Start + 1 <= position.Column && position.Column <= word.End + 1;

        Range CreateRangeWithPosition(int startColumn, int endColumn)
        {
            startColumn = Math.Min(startColumn, position.Column);
            endColumn = Math.Max(endColumn, position.Column);
            return new Range(position.LineNumber, startColumn, position.LineNumber, endColumn);
        }

        Range DeleteWordAndAdjacentWhitespace(FindWordResult word)
        {
            int startColumn = word.Start + 1;
            int endColumn = word.End + 1;
            if (onlyWord)
            {
                return CreateRangeWithPosition(startColumn, endColumn);
            }

            bool expandedToTheRight = false;
            while (endColumn - 1 < lineLength && CharAtIsWhitespace(lineContent, endColumn - 1))
            {
                expandedToTheRight = true;
                endColumn++;
            }

            if (!expandedToTheRight)
            {
                while (startColumn > 1 && CharAtIsWhitespace(lineContent, startColumn - 2))
                {
                    startColumn--;
                }
            }

            return CreateRangeWithPosition(startColumn, endColumn);
        }

        FindWordResult? prevWordOnLine = FindPreviousWordOnLine(wordSeparators, model, position);
        if (prevWordOnLine is { } prev && TouchesWord(prev))
        {
            return DeleteWordAndAdjacentWhitespace(prev);
        }

        FindWordResult? nextWordOnLine = FindNextWordOnLine(wordSeparators, model, position);
        if (nextWordOnLine is { } next && TouchesWord(next))
        {
            return DeleteWordAndAdjacentWhitespace(next);
        }

        if (prevWordOnLine is { } before && nextWordOnLine is { } after)
        {
            return CreateRangeWithPosition(before.End + 1, after.Start + 1);
        }

        if (prevWordOnLine is { } onlyPrev)
        {
            return CreateRangeWithPosition(onlyPrev.Start + 1, onlyPrev.End + 1);
        }

        if (nextWordOnLine is { } onlyNext)
        {
            return CreateRangeWithPosition(onlyNext.Start + 1, onlyNext.End + 1);
        }

        return CreateRangeWithPosition(1, lineLength + 1);
    }

    public static Range DeleteWordPartLeft(ICursorSimpleModel model, Selection selection)
    {
        if (!selection.IsEmpty)
        {
            return selection.Range;
        }

        Position pos = selection.Position;
        Position toPosition = MoveWordPartLeft(model, pos);

        // Monaco: new Range(pos.lineNumber, pos.column, toPosition.lineNumber, toPosition.column), which orders its corners.
        return new Range(pos, toPosition);
    }

    private static int FindFirstNonWhitespaceChar(string str, int startIndex)
    {
        int len = str.Length;
        for (int chIndex = startIndex; chIndex < len; chIndex++)
        {
            char ch = str[chIndex];
            if (ch != ' ' && ch != '\t')
            {
                return chIndex;
            }
        }

        return len;
    }

    private static Range? DeleteWordRightWhitespace(TextModelCursorSimpleModel model, Position position)
    {
        string lineContent = model.GetLineContent(position.LineNumber);
        int startIndex = position.Column - 1;
        int firstNonWhitespace = FindFirstNonWhitespaceChar(lineContent, startIndex);
        if (startIndex < firstNonWhitespace)
        {
            // bingo
            return new Range(position.LineNumber, position.Column, position.LineNumber, firstNonWhitespace + 1);
        }

        return null;
    }

    public static Range? DeleteWordRight(DeleteWordContext ctx, WordNavigationType wordNavigationType)
    {
        WordCharacterClassifier wordSeparators = ctx.WordSeparators;
        var model = new TextModelCursorSimpleModel(ctx.Model);
        Selection selection = ctx.Selection;
        bool whitespaceHeuristics = ctx.WhitespaceHeuristics;

        if (!selection.IsEmpty)
        {
            return selection.Range;
        }

        var position = new Position(selection.PositionLineNumber, selection.PositionColumn);

        int lineNumber = position.LineNumber;
        int column = position.Column;

        int lineCount = model.GetLineCount();
        int maxColumn = model.GetLineMaxColumn(lineNumber);
        if (lineNumber == lineCount && column == maxColumn)
        {
            // Ignore deleting at end of file
            return null;
        }

        if (whitespaceHeuristics)
        {
            Range? r = DeleteWordRightWhitespace(model, position);
            if (r is not null)
            {
                return r;
            }
        }

        FindWordResult? nextWordOnLine = FindNextWordOnLine(wordSeparators, model, position);

        if (wordNavigationType == WordNavigationType.WordEnd)
        {
            if (nextWordOnLine is { } end)
            {
                column = end.End + 1;
            }
            else
            {
                (lineNumber, column) = DeleteRightAtLineEnd(wordSeparators, model, lineNumber, column, maxColumn, lineCount);
            }
        }
        else
        {
            if (nextWordOnLine is { } current && column >= current.Start + 1)
            {
                nextWordOnLine = FindNextWordOnLine(wordSeparators, model, new Position(lineNumber, current.End + 1));
            }

            if (nextWordOnLine is { } start)
            {
                column = start.Start + 1;
            }
            else
            {
                (lineNumber, column) = DeleteRightAtLineEnd(wordSeparators, model, lineNumber, column, maxColumn, lineCount);
            }
        }

        return new Range(lineNumber, column, position.LineNumber, position.Column);
    }

    // Monaco repeats this block in both branches of deleteWordRight.
    private static (int LineNumber, int Column) DeleteRightAtLineEnd(WordCharacterClassifier wordSeparators, TextModelCursorSimpleModel model, int lineNumber, int column, int maxColumn, int lineCount)
    {
        if (column < maxColumn || lineNumber == lineCount)
        {
            return (lineNumber, maxColumn);
        }

        lineNumber++;
        FindWordResult? nextWordOnLine = FindNextWordOnLine(wordSeparators, model, new Position(lineNumber, 1));
        return (lineNumber, nextWordOnLine is { } next ? next.Start + 1 : model.GetLineMaxColumn(lineNumber));
    }

    public static Range DeleteWordPartRight(ICursorSimpleModel model, Selection selection)
    {
        if (!selection.IsEmpty)
        {
            return selection.Range;
        }

        Position pos = selection.Position;
        Position toPosition = MoveWordPartRight(model, pos);
        return new Range(pos, toPosition);
    }
}
