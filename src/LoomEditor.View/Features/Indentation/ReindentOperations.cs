// Ported from microsoft/vscode: src/vs/editor/contrib/indentation/common/indentation.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Edits;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Cursors.Commands;

namespace LoomEditor.View.Features.Indentation;

/// <summary>The edits that re-indent lines with the indentation rules of the language (Monaco: <c>getReindentEditOperations</c>).</summary>
/// <remarks>Threading: stateless; UI thread only.</remarks>
internal static class ReindentOperations
{
    public static List<IdentifiedEditOperation> GetReindentEditOperations(
        TextModel model,
        LanguageConfigurationRegistry languageConfigurations,
        int startLineNumber,
        int endLineNumber)
    {
        if (model.LineCount == 1 && model.GetLineMaxColumn(1) == 1)
        {
            // Model is empty
            return [];
        }

        IndentRulesSupport? indentationRulesSupport = languageConfigurations.GetLanguageConfiguration(model.Tokenization.GetLanguageId()).IndentRulesSupport;
        if (indentationRulesSupport is null)
        {
            return [];
        }

        var processedIndentRulesSupport = new ProcessedIndentRulesSupport(model, indentationRulesSupport, languageConfigurations);
        endLineNumber = Math.Min(endLineNumber, model.LineCount);

        // Skip `unIndentedLinePattern` lines
        while (startLineNumber <= endLineNumber)
        {
            if (!processedIndentRulesSupport.ShouldIgnore(startLineNumber))
            {
                break;
            }

            startLineNumber++;
        }

        if (startLineNumber > endLineNumber - 1)
        {
            return [];
        }

        TextModelOptions options = model.Options;
        string ShiftIndent(string indentation, int? count = null)
        {
            int c = count is null or 0 ? 1 : count.Value;
            return ShiftCommand.ShiftIndent(indentation, indentation.Length + c, options.TabSize, options.IndentSize, options.InsertSpaces);
        }

        string UnshiftIndent(string indentation, int? count = null)
        {
            int c = count is null or 0 ? 1 : count.Value;
            return ShiftCommand.UnshiftIndent(indentation, indentation.Length + c, options.TabSize, options.IndentSize, options.InsertSpaces);
        }

        var indentEdits = new List<IdentifiedEditOperation>();

        // indentation being passed to lines below

        // Calculate indentation for the first line
        // If there is no passed-in indentation, we use the indentation of the first line as base.
        string currentLineText = model.GetLineContent(startLineNumber);
        string globalIndent = Strings.GetLeadingWhitespace(currentLineText);
        // idealIndentForNextLine doesn't equal globalIndent when there is a line matching `indentNextLinePattern`.
        string idealIndentForNextLine = globalIndent;

        if (processedIndentRulesSupport.ShouldIncrease(startLineNumber))
        {
            idealIndentForNextLine = ShiftIndent(idealIndentForNextLine);
            globalIndent = ShiftIndent(globalIndent);
        }
        else if (processedIndentRulesSupport.ShouldIndentNextLine(startLineNumber))
        {
            idealIndentForNextLine = ShiftIndent(idealIndentForNextLine);
        }

        startLineNumber++;

        // Calculate indentation adjustment for all following lines
        for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
        {
            if (DoesLineStartWithString(model, lineNumber))
            {
                continue;
            }

            string text = model.GetLineContent(lineNumber);
            string oldIndentation = Strings.GetLeadingWhitespace(text);
            string currentIdealIndent = idealIndentForNextLine;

            if (processedIndentRulesSupport.ShouldDecrease(lineNumber, currentIdealIndent))
            {
                idealIndentForNextLine = UnshiftIndent(idealIndentForNextLine);
                globalIndent = UnshiftIndent(globalIndent);
            }

            if (oldIndentation != idealIndentForNextLine)
            {
                indentEdits.Add(new IdentifiedEditOperation(
                    new Range(lineNumber, 1, lineNumber, oldIndentation.Length + 1),
                    Core.Text.Indentation.NormalizeIndentation(idealIndentForNextLine, options.IndentSize, options.InsertSpaces),
                    ForceMoveMarkers: true));
            }

            // calculate idealIndentForNextLine
            if (processedIndentRulesSupport.ShouldIgnore(lineNumber))
            {
                // In reindent phase, if the line matches `unIndentedLinePattern` we inherit indentation from above lines
                // but don't change globalIndent and idealIndentForNextLine.
                continue;
            }

            if (processedIndentRulesSupport.ShouldIncrease(lineNumber, currentIdealIndent))
            {
                globalIndent = ShiftIndent(globalIndent);
                idealIndentForNextLine = globalIndent;
            }
            else if (processedIndentRulesSupport.ShouldIndentNextLine(lineNumber, currentIdealIndent))
            {
                idealIndentForNextLine = ShiftIndent(idealIndentForNextLine);
            }
            else
            {
                idealIndentForNextLine = globalIndent;
            }
        }

        return indentEdits;
    }

    private static bool DoesLineStartWithString(TextModel model, int lineNumber)
    {
        if (!model.Tokenization.IsCheapToTokenize(lineNumber))
        {
            return false;
        }

        LineTokens lineTokens = model.Tokenization.GetLineTokens(lineNumber);
        return lineTokens.GetStandardTokenType(0) == StandardTokenType.String;
    }
}
