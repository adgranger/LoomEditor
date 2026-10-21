// Ported from microsoft/vscode: src/vs/editor/contrib/comment/browser/blockCommentCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Features.Comment;

/// <summary>Wraps a selection in the block comment tokens of its language, or removes them (Monaco: <c>BlockCommentCommand</c>).</summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class BlockCommentCommand(Selection selection, bool insertSpace, LanguageConfigurationRegistry languageConfigurations) : IEditCommand
{
    private string? _usedEndToken;

    public bool InsertsAutoWhitespace => false;

    /// <summary>Whether <paramref name="haystack"/> has <paramref name="needle"/> at <paramref name="offset"/>, ignoring the case of ASCII letters.</summary>
    public static bool HaystackHasNeedleAtOffset(string haystack, string needle, int offset)
    {
        if (offset < 0)
        {
            return false;
        }

        if (offset + needle.Length > haystack.Length)
        {
            return false;
        }

        for (int i = 0; i < needle.Length; i++)
        {
            char codeA = haystack[offset + i];
            char codeB = needle[i];

            if (codeA == codeB)
            {
                continue;
            }

            if (codeA is >= 'A' and <= 'Z' && codeA + 32 == codeB)
            {
                // codeA is upper-case variant of codeB
                continue;
            }

            if (codeB is >= 'A' and <= 'Z' && codeB + 32 == codeA)
            {
                // codeB is upper-case variant of codeA
                continue;
            }

            return false;
        }

        return true;
    }

    public static List<(Range Range, string? Text)> CreateRemoveBlockCommentOperations(Range r, string startToken, string endToken)
    {
        var res = new List<(Range Range, string? Text)>();

        if (!r.IsEmpty)
        {
            // Remove block comment start
            res.Add((new Range(r.StartLineNumber, r.StartColumn - startToken.Length, r.StartLineNumber, r.StartColumn), null));

            // Remove block comment end
            res.Add((new Range(r.EndLineNumber, r.EndColumn, r.EndLineNumber, r.EndColumn + endToken.Length), null));
        }
        else
        {
            // Remove both continuously
            res.Add((new Range(r.StartLineNumber, r.StartColumn - startToken.Length, r.EndLineNumber, r.EndColumn + endToken.Length), null));
        }

        return res;
    }

    public static List<(Range Range, string? Text)> CreateAddBlockCommentOperations(Range r, string startToken, string endToken, bool insertSpace)
    {
        var res = new List<(Range Range, string? Text)>();

        if (!r.IsEmpty)
        {
            // Insert block comment start
            res.Add((new Range(r.StartLineNumber, r.StartColumn, r.StartLineNumber, r.StartColumn), startToken + (insertSpace ? " " : string.Empty)));

            // Insert block comment end
            res.Add((new Range(r.EndLineNumber, r.EndColumn, r.EndLineNumber, r.EndColumn), (insertSpace ? " " : string.Empty) + endToken));
        }
        else
        {
            // Insert both continuously
            res.Add((new Range(r.StartLineNumber, r.StartColumn, r.EndLineNumber, r.EndColumn), startToken + "  " + endToken));
        }

        return res;
    }

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        int startLineNumber = selection.Range.StartLineNumber;
        int startColumn = selection.Range.StartColumn;

        model.Tokenization.TokenizeIfCheap(startLineNumber);
        string languageId = model.Tokenization.GetLanguageIdAtPosition(startLineNumber, startColumn);
        CommentsConfiguration? config = languageConfigurations.GetLanguageConfiguration(languageId).Comments;
        if (config is null || string.IsNullOrEmpty(config.BlockCommentStartToken) || string.IsNullOrEmpty(config.BlockCommentEndToken))
        {
            // Mode does not support block comments
            return;
        }

        CreateOperationsForBlockComment(selection.Range, config.BlockCommentStartToken, config.BlockCommentEndToken, model, builder);
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        IReadOnlyList<ValidEditOperation> inverseEditOperations = helper.GetInverseEditOperations();
        if (inverseEditOperations.Count == 2)
        {
            Range startTokenEditOperation = inverseEditOperations[0].Range;
            Range endTokenEditOperation = inverseEditOperations[1].Range;

            return new Selection(
                startTokenEditOperation.EndLineNumber,
                startTokenEditOperation.EndColumn,
                endTokenEditOperation.StartLineNumber,
                endTokenEditOperation.StartColumn);
        }

        Range srcRange = inverseEditOperations[0].Range;
        int deltaColumn = _usedEndToken is not null ? -_usedEndToken.Length - 1 : 0; // minus 1 space before endToken
        return new Selection(srcRange.EndLineNumber, srcRange.EndColumn + deltaColumn, srcRange.EndLineNumber, srcRange.EndColumn + deltaColumn);
    }

    private void CreateOperationsForBlockComment(Range range, string startToken, string endToken, TextModel model, IEditOperationBuilder builder)
    {
        int startLineNumber = range.StartLineNumber;
        int startColumn = range.StartColumn;
        int endLineNumber = range.EndLineNumber;
        int endColumn = range.EndColumn;

        string startLineText = model.GetLineContent(startLineNumber);
        string endLineText = model.GetLineContent(endLineNumber);

        int startTokenIndex = JsStrings.LastIndexOf(startLineText, startToken, startColumn - 1 + startToken.Length);
        int endTokenIndex = JsStrings.IndexOf(endLineText, endToken, endColumn - 1 - endToken.Length);

        if (startTokenIndex != -1 && endTokenIndex != -1)
        {
            if (startLineNumber == endLineNumber)
            {
                string lineBetweenTokens = JsStrings.Substring(startLineText, startTokenIndex + startToken.Length, endTokenIndex);

                if (lineBetweenTokens.Contains(endToken, StringComparison.Ordinal))
                {
                    // force to add a block comment
                    startTokenIndex = -1;
                    endTokenIndex = -1;
                }
            }
            else
            {
                string startLineAfterStartToken = startLineText.Substring(startTokenIndex + startToken.Length);
                string endLineBeforeEndToken = endLineText.Substring(0, endTokenIndex);

                if (startLineAfterStartToken.Contains(endToken, StringComparison.Ordinal) || endLineBeforeEndToken.Contains(endToken, StringComparison.Ordinal))
                {
                    // force to add a block comment
                    startTokenIndex = -1;
                    endTokenIndex = -1;
                }
            }
        }

        List<(Range Range, string? Text)> ops;

        if (startTokenIndex != -1 && endTokenIndex != -1)
        {
            // Consider spaces as part of the comment tokens
            if (insertSpace && startTokenIndex + startToken.Length < startLineText.Length && startLineText[startTokenIndex + startToken.Length] == ' ')
            {
                // Pretend the start token contains a trailing space
                startToken += " ";
            }

            if (insertSpace && endTokenIndex > 0 && endLineText[endTokenIndex - 1] == ' ')
            {
                // Pretend the end token contains a leading space
                endToken = " " + endToken;
                endTokenIndex -= 1;
            }

            ops = CreateRemoveBlockCommentOperations(
                new Range(startLineNumber, startTokenIndex + startToken.Length + 1, endLineNumber, endTokenIndex + 1), startToken, endToken);
        }
        else
        {
            ops = CreateAddBlockCommentOperations(range, startToken, endToken, insertSpace);
            _usedEndToken = ops.Count == 1 ? endToken : null;
        }

        foreach ((Range opRange, string? text) in ops)
        {
            builder.AddTrackedEditOperation(opRange, text);
        }
    }
}
