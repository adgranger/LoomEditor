// Ported from microsoft/vscode: src/vs/editor/contrib/comment/browser/lineCommentCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;

namespace LoomEditor.View.Features.Comment;

// The analysis of the lines: which lines take part and what a toggle does.
internal sealed partial class LineCommentCommand
{
    private const int MaxSafeSmallInteger = 1 << 30;

    /// <summary>
    /// Do an initial pass over the lines and gather info about the line comment string.
    /// Returns <see langword="null"/> if any of the lines doesn't support a line comment string.
    /// </summary>
    private static List<LinePreflightData>? GatherPreflightCommentStrings(
        TextModel model,
        int startLineNumber,
        int endLineNumber,
        LanguageConfigurationRegistry languageConfigurations)
    {
        model.Tokenization.TokenizeIfCheap(startLineNumber);
        string languageId = model.Tokenization.GetLanguageIdAtPosition(startLineNumber, 1);

        CommentsConfiguration? config = languageConfigurations.GetLanguageConfiguration(languageId).Comments;
        string? commentStr = config?.LineCommentToken;
        if (string.IsNullOrEmpty(commentStr))
        {
            // Mode does not support line comments
            return null;
        }

        int lineCount = endLineNumber - startLineNumber + 1;
        var lines = new List<LinePreflightData>(lineCount);
        for (int i = 0; i < lineCount; i++)
        {
            lines.Add(new LinePreflightData { Ignore = false, CommentStr = commentStr, CommentStrOffset = 0, CommentStrLength = commentStr.Length });
        }

        return lines;
    }

    /// <summary>
    /// Analyze lines and decide which lines are relevant and what the toggle should do.
    /// Also, build up several offsets and lengths useful in the generation of editor operations.
    /// </summary>
    internal static PreflightData AnalyzeLines(
        LineCommentType type,
        bool insertSpace,
        Func<int, string> getLineContent,
        List<LinePreflightData> lines,
        int startLineNumber,
        bool ignoreEmptyLines,
        bool ignoreFirstLine,
        LanguageConfigurationRegistry languageConfigurations,
        string languageId)
    {
        bool onlyWhitespaceLines = true;

        CommentsConfiguration? config = languageConfigurations.GetLanguageConfiguration(languageId).Comments;
        bool lineCommentNoIndent = config?.LineCommentNoIndent ?? false;

        bool shouldRemoveComments = type != LineCommentType.ForceAdd;

        for (int i = 0; i < lines.Count; i++)
        {
            LinePreflightData lineData = lines[i];
            int lineNumber = startLineNumber + i;

            if (lineNumber == startLineNumber && ignoreFirstLine)
            {
                // first line ignored
                lineData.Ignore = true;
                continue;
            }

            string lineContent = getLineContent(lineNumber);
            int lineContentStartOffset = StringClassification.FirstNonWhitespaceIndex(lineContent);

            if (lineContentStartOffset == -1)
            {
                // Empty or whitespace only line
                lineData.Ignore = ignoreEmptyLines;
                lineData.CommentStrOffset = lineCommentNoIndent ? 0 : lineContent.Length;
                continue;
            }

            onlyWhitespaceLines = false;
            int offset = lineCommentNoIndent ? 0 : lineContentStartOffset;
            lineData.Ignore = false;
            lineData.CommentStrOffset = offset;

            if (shouldRemoveComments && !BlockCommentCommand.HaystackHasNeedleAtOffset(lineContent, lineData.CommentStr, offset))
            {
                if (type == LineCommentType.Toggle)
                {
                    // Every line so far has been a line comment, but this one is not
                    shouldRemoveComments = false;
                }
                else if (type == LineCommentType.ForceAdd)
                {
                    // Will not happen
                }
                else
                {
                    lineData.Ignore = true;
                }
            }

            if (shouldRemoveComments && insertSpace)
            {
                // Remove a following space if present
                int commentStrEndOffset = lineContentStartOffset + lineData.CommentStrLength;
                if (commentStrEndOffset < lineContent.Length && lineContent[commentStrEndOffset] == ' ')
                {
                    lineData.CommentStrLength += 1;
                }
            }
        }

        if (type == LineCommentType.Toggle && onlyWhitespaceLines)
        {
            // For only whitespace lines, we insert comments
            shouldRemoveComments = false;

            // Also, no longer ignore them
            foreach (LinePreflightData line in lines)
            {
                line.Ignore = false;
            }
        }

        return new PreflightData(true, shouldRemoveComments, lines);
    }

    /// <summary>Analyze all lines and decide exactly what to do: not supported, insert line comments or remove line comments.</summary>
    internal static PreflightData GatherPreflightData(
        LineCommentType type,
        bool insertSpace,
        TextModel model,
        int startLineNumber,
        int endLineNumber,
        bool ignoreEmptyLines,
        bool ignoreFirstLine,
        LanguageConfigurationRegistry languageConfigurations)
    {
        List<LinePreflightData>? lines = GatherPreflightCommentStrings(model, startLineNumber, endLineNumber, languageConfigurations);
        string languageId = model.Tokenization.GetLanguageIdAtPosition(startLineNumber, 1);
        if (lines is null)
        {
            return new PreflightData(false, false, []);
        }

        return AnalyzeLines(type, insertSpace, model.GetLineContent, lines, startLineNumber, ignoreEmptyLines, ignoreFirstLine, languageConfigurations, languageId);
    }

    private static int NextVisibleColumn(int currentVisibleColumn, int indentSize, bool isTab, int columnSize) =>
        isTab ? currentVisibleColumn + (indentSize - (currentVisibleColumn % indentSize)) : currentVisibleColumn + columnSize;

    /// <summary>Adjust insertion points to have them vertically aligned in the add line comment case.</summary>
    internal static void NormalizeInsertionPoint(Func<int, string> getLineContent, List<LinePreflightData> lines, int startLineNumber, int indentSize)
    {
        int minVisibleColumn = MaxSafeSmallInteger;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Ignore)
            {
                continue;
            }

            string lineContent = getLineContent(startLineNumber + i);

            int currentVisibleColumn = 0;
            for (int j = 0, lenJ = lines[i].CommentStrOffset; currentVisibleColumn < minVisibleColumn && j < lenJ; j++)
            {
                currentVisibleColumn = NextVisibleColumn(currentVisibleColumn, indentSize, lineContent[j] == (char)9, 1);
            }

            if (currentVisibleColumn < minVisibleColumn)
            {
                minVisibleColumn = currentVisibleColumn;
            }
        }

        minVisibleColumn = minVisibleColumn / indentSize * indentSize;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Ignore)
            {
                continue;
            }

            string lineContent = getLineContent(startLineNumber + i);

            int currentVisibleColumn = 0;
            int j = 0;
            for (int lenJ = lines[i].CommentStrOffset; currentVisibleColumn < minVisibleColumn && j < lenJ; j++)
            {
                currentVisibleColumn = NextVisibleColumn(currentVisibleColumn, indentSize, lineContent[j] == (char)9, 1);
            }

            lines[i].CommentStrOffset = currentVisibleColumn > minVisibleColumn ? j - 1 : j;
        }
    }
}
