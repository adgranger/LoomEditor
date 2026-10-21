// Ported from microsoft/vscode: src/vs/editor/contrib/comment/browser/lineCommentCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Features.Comment;

/// <summary>Toggles, adds or removes line comments on the selected lines, or falls back to a block comment (Monaco: <c>LineCommentCommand</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class LineCommentCommand(
    LanguageConfigurationRegistry languageConfigurations,
    Selection selection,
    int indentSize,
    LineCommentType type,
    bool insertSpace,
    bool ignoreEmptyLines,
    bool ignoreFirstLine = false) : IEditCommand
{
    private string? _selectionId;
    private int _deltaColumn;
    private bool _moveEndPositionDown;

    public bool InsertsAutoWhitespace => false;

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        Selection s = selection;
        _moveEndPositionDown = false;
        Range sr = s.Range;

        if (sr.StartLineNumber == sr.EndLineNumber && ignoreFirstLine)
        {
            builder.AddEditOperation(
                new Range(sr.StartLineNumber, model.GetLineMaxColumn(sr.StartLineNumber), sr.StartLineNumber + 1, 1),
                sr.StartLineNumber == model.LineCount ? string.Empty : "\n");
            _selectionId = builder.TrackSelection(s);
            return;
        }

        if (sr.StartLineNumber < sr.EndLineNumber && sr.EndColumn == 1)
        {
            _moveEndPositionDown = true;
            s = s.WithEnd(new Position(sr.EndLineNumber - 1, model.GetLineMaxColumn(sr.EndLineNumber - 1)));
        }

        PreflightData data = GatherPreflightData(
            type, insertSpace, model, s.Range.StartLineNumber, s.Range.EndLineNumber, ignoreEmptyLines, ignoreFirstLine, languageConfigurations);

        if (data.Supported)
        {
            ExecuteLineComments(model, builder, data, s);
            return;
        }

        ExecuteBlockComment(model, builder, s);
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        Selection result = helper.GetTrackedSelection(_selectionId!);

        if (_moveEndPositionDown)
        {
            result = result.WithEnd(new Position(result.Range.EndLineNumber + 1, 1));
        }

        return new Selection(
            result.SelectionStartLineNumber,
            result.SelectionStartColumn + _deltaColumn,
            result.PositionLineNumber,
            result.PositionColumn + _deltaColumn);
    }

    /// <summary>Generate edit operations in the remove line comment case.</summary>
    internal static List<(Range Range, string? Text)> CreateRemoveLineCommentsOperations(List<LinePreflightData> lines, int startLineNumber)
    {
        var res = new List<(Range Range, string? Text)>();

        for (int i = 0; i < lines.Count; i++)
        {
            LinePreflightData lineData = lines[i];

            if (lineData.Ignore)
            {
                continue;
            }

            res.Add((
                new Range(
                    startLineNumber + i, lineData.CommentStrOffset + 1,
                    startLineNumber + i, lineData.CommentStrOffset + lineData.CommentStrLength + 1),
                null));
        }

        return res;
    }

    /// <summary>Given a successful analysis, execute either insert line comments, either remove line comments.</summary>
    private void ExecuteLineComments(TextModel model, IEditOperationBuilder builder, PreflightData data, Selection s)
    {
        List<(Range Range, string? Text)> ops;

        if (data.ShouldRemoveComments)
        {
            ops = CreateRemoveLineCommentsOperations(data.Lines, s.Range.StartLineNumber);
        }
        else
        {
            NormalizeInsertionPoint(model.GetLineContent, data.Lines, s.Range.StartLineNumber, indentSize);
            ops = CreateAddLineCommentsOperations(data.Lines, s.Range.StartLineNumber);
        }

        var cursorPosition = new Position(s.PositionLineNumber, s.PositionColumn);

        foreach ((Range range, string? text) in ops)
        {
            builder.AddEditOperation(range, text);
            if (range.IsEmpty && new Position(range.StartLineNumber, range.StartColumn) == cursorPosition)
            {
                string lineContent = model.GetLineContent(cursorPosition.LineNumber);
                if (lineContent.Length + 1 == cursorPosition.Column)
                {
                    _deltaColumn = (text ?? string.Empty).Length;
                }
            }
        }

        _selectionId = builder.TrackSelection(s);
    }

    /// <summary>Generate edit operations in the add line comment case.</summary>
    private List<(Range Range, string? Text)> CreateAddLineCommentsOperations(List<LinePreflightData> lines, int startLineNumber)
    {
        var res = new List<(Range Range, string? Text)>();
        string afterCommentStr = insertSpace ? " " : string.Empty;

        for (int i = 0; i < lines.Count; i++)
        {
            LinePreflightData lineData = lines[i];

            if (lineData.Ignore)
            {
                continue;
            }

            var position = new Range(startLineNumber + i, lineData.CommentStrOffset + 1, startLineNumber + i, lineData.CommentStrOffset + 1);
            res.Add((position, lineData.CommentStr + afterCommentStr));
        }

        return res;
    }

    private static List<(Range Range, string? Text)>? AttemptRemoveBlockComment(TextModel model, Selection s, string startToken, string endToken)
    {
        Range sr = s.Range;
        int startLineNumber = sr.StartLineNumber;
        int endLineNumber = sr.EndLineNumber;

        int startTokenAllowedBeforeColumn = endToken.Length + Math.Max(model.GetLineFirstNonWhitespaceColumn(sr.StartLineNumber), sr.StartColumn);

        int startTokenIndex = JsStrings.LastIndexOf(model.GetLineContent(startLineNumber), startToken, startTokenAllowedBeforeColumn - 1);
        int endTokenIndex = JsStrings.IndexOf(model.GetLineContent(endLineNumber), endToken, sr.EndColumn - 1 - startToken.Length);

        if (startTokenIndex != -1 && endTokenIndex == -1)
        {
            endTokenIndex = JsStrings.IndexOf(model.GetLineContent(startLineNumber), endToken, startTokenIndex + startToken.Length);
            endLineNumber = startLineNumber;
        }

        if (startTokenIndex == -1 && endTokenIndex != -1)
        {
            startTokenIndex = JsStrings.LastIndexOf(model.GetLineContent(endLineNumber), startToken, endTokenIndex);
            startLineNumber = endLineNumber;
        }

        if (s.IsEmpty && (startTokenIndex == -1 || endTokenIndex == -1))
        {
            startTokenIndex = JsStrings.IndexOf(model.GetLineContent(startLineNumber), startToken, 0);
            if (startTokenIndex != -1)
            {
                endTokenIndex = JsStrings.IndexOf(model.GetLineContent(startLineNumber), endToken, startTokenIndex + startToken.Length);
            }
        }

        // We have to adjust to possible inner white space.
        // For Space after startToken, add Space to startToken - range math will work out.
        if (startTokenIndex != -1 && JsStrings.CharAtEquals(model.GetLineContent(startLineNumber), startTokenIndex + startToken.Length, ' '))
        {
            startToken += " ";
        }

        // For Space before endToken, add Space before endToken and shift index one left.
        if (endTokenIndex != -1 && JsStrings.CharAtEquals(model.GetLineContent(endLineNumber), endTokenIndex - 1, ' '))
        {
            endToken = " " + endToken;
            endTokenIndex -= 1;
        }

        if (startTokenIndex != -1 && endTokenIndex != -1)
        {
            return BlockCommentCommand.CreateRemoveBlockCommentOperations(
                new Range(startLineNumber, startTokenIndex + startToken.Length + 1, endLineNumber, endTokenIndex + 1), startToken, endToken);
        }

        return null;
    }

    /// <summary>Given an unsuccessful analysis, delegate to the block comment command.</summary>
    private void ExecuteBlockComment(TextModel model, IEditOperationBuilder builder, Selection s)
    {
        Range sr = s.Range;
        model.Tokenization.TokenizeIfCheap(sr.StartLineNumber);
        string languageId = model.Tokenization.GetLanguageIdAtPosition(sr.StartLineNumber, 1);
        CommentsConfiguration? config = languageConfigurations.GetLanguageConfiguration(languageId).Comments;
        if (config is null || string.IsNullOrEmpty(config.BlockCommentStartToken) || string.IsNullOrEmpty(config.BlockCommentEndToken))
        {
            // Mode does not support block comments
            return;
        }

        string startToken = config.BlockCommentStartToken;
        string endToken = config.BlockCommentEndToken;

        List<(Range Range, string? Text)>? ops = AttemptRemoveBlockComment(model, s, startToken, endToken);
        if (ops is null)
        {
            if (s.IsEmpty)
            {
                string lineContent = model.GetLineContent(sr.StartLineNumber);
                int firstNonWhitespaceIndex = StringClassification.FirstNonWhitespaceIndex(lineContent);
                if (firstNonWhitespaceIndex == -1)
                {
                    // Line is empty or contains only whitespace
                    firstNonWhitespaceIndex = lineContent.Length;
                }

                ops = BlockCommentCommand.CreateAddBlockCommentOperations(
                    new Range(sr.StartLineNumber, firstNonWhitespaceIndex + 1, sr.StartLineNumber, lineContent.Length + 1),
                    startToken,
                    endToken,
                    insertSpace);
            }
            else
            {
                ops = BlockCommentCommand.CreateAddBlockCommentOperations(
                    new Range(sr.StartLineNumber, model.GetLineFirstNonWhitespaceColumn(sr.StartLineNumber), sr.EndLineNumber, model.GetLineMaxColumn(sr.EndLineNumber)),
                    startToken,
                    endToken,
                    insertSpace);
            }

            if (ops.Count == 1)
            {
                // Leave cursor after token and Space
                _deltaColumn = startToken.Length + 1;
            }
        }

        _selectionId = builder.TrackSelection(s);
        foreach ((Range range, string? text) in ops)
        {
            builder.AddEditOperation(range, text);
        }
    }
}
