// Ported from microsoft/vscode: src/vs/editor/common/cursor/cursorTypeEditOperations.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Cursors;

/// <summary>Typing a closing bracket re-indents its line to match the opening bracket (Monaco: <c>InterceptorElectricCharOperation</c>).</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static class InterceptorElectricCharOperation
{
    public static EditOperationResult? GetEdits(
        EditOperationType prevEditOperationType,
        CursorConfiguration config,
        TextModel model,
        IReadOnlyList<Selection> selections,
        string ch,
        bool isDoingComposition)
    {
        // Electric characters make sense only when dealing with a single cursor,
        // as multiple cursors typing brackets for example would interfer with bracket matching
        if (!isDoingComposition && IsTypeInterceptorElectricChar(model, selections))
        {
            EditOperationResult? r = TypeInterceptorElectricChar(prevEditOperationType, config, model, selections[0], ch);
            if (r is not null)
            {
                return r;
            }
        }

        return null;
    }

    private static bool IsTypeInterceptorElectricChar(TextModel model, IReadOnlyList<Selection> selections) =>
        selections.Count == 1 && model.Tokenization.IsCheapToTokenize(selections[0].Range.EndLineNumber);

    private static EditOperationResult? TypeInterceptorElectricChar(
        EditOperationType prevEditOperationType,
        CursorConfiguration config,
        TextModel model,
        Selection selection,
        string ch)
    {
        if (!config.ElectricChars.Contains(ch) || !selection.IsEmpty)
        {
            return null;
        }

        Position position = selection.Position;
        model.Tokenization.ForceTokenization(position.LineNumber);
        LineTokens lineTokens = model.Tokenization.GetLineTokens(position.LineNumber);
        // Monaco catches the exceptions of onElectricCharacter (onUnexpectedError): a port must fail the tests instead.
        ElectricAction? electricAction = config.OnElectricCharacter(ch, lineTokens, position.Column);
        if (electricAction is not { MatchOpenBracket: { Length: > 0 } matchOpenBracket })
        {
            return null;
        }

        int endColumn = (lineTokens.GetLineContent() + ch).LastIndexOf(matchOpenBracket, StringComparison.Ordinal) + 1;
        // give at most 500ms to compute
        Range? match = model.BracketPairs.FindMatchingBracketUp(matchOpenBracket, new Position(position.LineNumber, endColumn), TimeSpan.FromMilliseconds(500));
        if (match is null)
        {
            return null;
        }

        if (match.Value.StartLineNumber == position.LineNumber)
        {
            // matched something on the same line => no change in indentation
            return null;
        }

        string matchLine = model.GetLineContent(match.Value.StartLineNumber);
        string matchLineIndentation = Strings.GetLeadingWhitespace(matchLine);
        string newIndentation = config.NormalizeIndentation(matchLineIndentation);
        string lineText = model.GetLineContent(position.LineNumber);
        int firstNonWhitespaceColumn = model.GetLineFirstNonWhitespaceColumn(position.LineNumber);
        int lineFirstNonBlankColumn = firstNonWhitespaceColumn != 0 ? firstNonWhitespaceColumn : position.Column;
        string prefix = lineText.Substring(lineFirstNonBlankColumn - 1, position.Column - lineFirstNonBlankColumn);
        string typeText = newIndentation + prefix + ch;
        var typeSelection = new Range(position.LineNumber, 1, position.LineNumber, position.Column);
        var command = new ReplaceCommand(typeSelection, typeText);
        return new EditOperationResult(
            TypeEditHelpers.GetTypingOperation(typeText, prevEditOperationType),
            [command],
            false,
            true);
    }
}
