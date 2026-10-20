// Ported from microsoft/vscode: src/vs/editor/common/languages/autoIndent.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Languages;

/// <summary>The indentation a line inherits from the lines above it (the result of <see cref="AutoIndent.GetInheritIndentForLine"/>).</summary>
/// <param name="Indentation">The leading whitespace of <paramref name="Line"/>.</param>
/// <param name="Action"><see cref="IndentAction.Indent"/> when the line should be indented one level deeper, else <see langword="null"/>.</param>
/// <param name="Line">The line the indentation comes from (Monaco's optional <c>line</c>).</param>
/// <remarks>Threading: immutable.</remarks>
internal readonly record struct InheritIndentResult(string Indentation, IndentAction? Action, int? Line = null);

/// <summary>The auto-indentation computations (Monaco: the module functions of <c>autoIndent.ts</c>).</summary>
/// <remarks>Threading: use it from the thread of the editor.</remarks>
internal static partial class AutoIndent
{
    /// <summary>
    /// Get inherited indentation from above lines.
    /// 1. Find the nearest preceding line which doesn't match unIndentedLinePattern.
    /// 2. If this line matches indentNextLinePattern or increaseIndentPattern, it means that the indent level of `lineNumber` should be 1 greater than this line.
    /// 3. If this line doesn't match any indent rules
    ///   a. check whether the line above it matches indentNextLinePattern
    ///   b. If not, the indent level of this line is the result
    ///   c. If so, it means the indent of this line is *temporary*, go upward utill we find a line whose indent is not temporary (the same workflow a -> b -> c).
    /// 4. Otherwise, we fail to get an inherited indent from aboves. Return null and we should not touch the indent of `lineNumber`
    ///
    /// This function only return the inherited indent based on above lines, it doesn't check whether current line should decrease or not.
    /// </summary>
    /// <remarks>Monaco's <c>honorIntentialIndent</c> defaults to <see langword="true"/>; it is required here.</remarks>
    public static InheritIndentResult? GetInheritIndentForLine(
        EditorAutoIndentStrategy autoIndent,
        IVirtualModel model,
        int lineNumber,
        bool honorIntentialIndent,
        LanguageConfigurationRegistry registry)
    {
        if (autoIndent < EditorAutoIndentStrategy.Full)
        {
            return null;
        }

        IndentRulesSupport? indentRulesSupport = registry.GetLanguageConfiguration(model.GetLanguageId()).IndentRulesSupport;
        if (indentRulesSupport is null)
        {
            return null;
        }

        var processedIndentRulesSupport = new ProcessedIndentRulesSupport(model, indentRulesSupport, registry);

        if (lineNumber <= 1)
        {
            return new InheritIndentResult("", null);
        }

        // Use no indent if this is the first non-blank line
        for (int priorLineNumber = lineNumber - 1; priorLineNumber > 0; priorLineNumber--)
        {
            if (model.GetLineContent(priorLineNumber).Length != 0)
            {
                break;
            }

            if (priorLineNumber == 1)
            {
                return new InheritIndentResult("", null);
            }
        }

        int precedingUnIgnoredLine = GetPrecedingValidLine(model, lineNumber, processedIndentRulesSupport);
        if (precedingUnIgnoredLine < 0)
        {
            return null;
        }
        else if (precedingUnIgnoredLine < 1)
        {
            return new InheritIndentResult("", null);
        }

        if (processedIndentRulesSupport.ShouldIncrease(precedingUnIgnoredLine) || processedIndentRulesSupport.ShouldIndentNextLine(precedingUnIgnoredLine))
        {
            string precedingUnIgnoredLineContent = model.GetLineContent(precedingUnIgnoredLine);
            return new InheritIndentResult(Strings.GetLeadingWhitespace(precedingUnIgnoredLineContent), IndentAction.Indent, precedingUnIgnoredLine);
        }
        else if (processedIndentRulesSupport.ShouldDecrease(precedingUnIgnoredLine))
        {
            string precedingUnIgnoredLineContent = model.GetLineContent(precedingUnIgnoredLine);
            return new InheritIndentResult(Strings.GetLeadingWhitespace(precedingUnIgnoredLineContent), null, precedingUnIgnoredLine);
        }
        else
        {
            // precedingUnIgnoredLine can not be ignored.
            // it doesn't increase indent of following lines
            // it doesn't increase just next line
            // so current line is not affect by precedingUnIgnoredLine
            // and then we should get a correct inheritted indentation from above lines
            if (precedingUnIgnoredLine == 1)
            {
                return new InheritIndentResult(Strings.GetLeadingWhitespace(model.GetLineContent(precedingUnIgnoredLine)), null, precedingUnIgnoredLine);
            }

            int previousLine = precedingUnIgnoredLine - 1;

            int previousLineIndentMetadata = indentRulesSupport.GetIndentMetadata(model.GetLineContent(previousLine));
            if ((previousLineIndentMetadata & (IndentConsts.IncreaseMask | IndentConsts.DecreaseMask)) == 0
                && (previousLineIndentMetadata & IndentConsts.IndentNextLineMask) != 0)
            {
                int stopLine = 0;
                for (int i = previousLine - 1; i > 0; i--)
                {
                    if (processedIndentRulesSupport.ShouldIndentNextLine(i))
                    {
                        continue;
                    }

                    stopLine = i;
                    break;
                }

                return new InheritIndentResult(Strings.GetLeadingWhitespace(model.GetLineContent(stopLine + 1)), null, stopLine + 1);
            }

            if (honorIntentialIndent)
            {
                return new InheritIndentResult(Strings.GetLeadingWhitespace(model.GetLineContent(precedingUnIgnoredLine)), null, precedingUnIgnoredLine);
            }
            else
            {
                // search from precedingUnIgnoredLine until we find one whose indent is not temporary
                for (int i = precedingUnIgnoredLine; i > 0; i--)
                {
                    if (processedIndentRulesSupport.ShouldIncrease(i))
                    {
                        return new InheritIndentResult(Strings.GetLeadingWhitespace(model.GetLineContent(i)), IndentAction.Indent, i);
                    }
                    else if (processedIndentRulesSupport.ShouldIndentNextLine(i))
                    {
                        int stopLine = 0;
                        for (int j = i - 1; j > 0; j--)
                        {
                            // Monaco uses i here
                            if (processedIndentRulesSupport.ShouldIndentNextLine(i))
                            {
                                continue;
                            }

                            stopLine = j;
                            break;
                        }

                        return new InheritIndentResult(Strings.GetLeadingWhitespace(model.GetLineContent(stopLine + 1)), null, stopLine + 1);
                    }
                    else if (processedIndentRulesSupport.ShouldDecrease(i))
                    {
                        return new InheritIndentResult(Strings.GetLeadingWhitespace(model.GetLineContent(i)), null, i);
                    }
                }

                return new InheritIndentResult(Strings.GetLeadingWhitespace(model.GetLineContent(1)), null, 1);
            }
        }
    }

    /// <summary>
    /// The indentation <paramref name="lineNumber"/> should have from the lines above it and the indentation and
    /// on-enter rules, or <see langword="null"/> (Monaco: <c>getGoodIndentForLine</c>).
    /// </summary>
    public static string? GetGoodIndentForLine(
        EditorAutoIndentStrategy autoIndent,
        IVirtualModel virtualModel,
        string languageId,
        int lineNumber,
        IIndentConverter indentConverter,
        LanguageConfigurationRegistry registry)
    {
        if (autoIndent < EditorAutoIndentStrategy.Full)
        {
            return null;
        }

        ResolvedLanguageConfiguration richEditSupport = registry.GetLanguageConfiguration(languageId);

        IndentRulesSupport? indentRulesSupport = registry.GetLanguageConfiguration(languageId).IndentRulesSupport;
        if (indentRulesSupport is null)
        {
            return null;
        }

        var processedIndentRulesSupport = new ProcessedIndentRulesSupport(virtualModel, indentRulesSupport, registry);
        InheritIndentResult? indent = GetInheritIndentForLine(autoIndent, virtualModel, lineNumber, true, registry);

        if (indent is { } inherited)
        {
            int? inheritLine = inherited.Line;
            if (inheritLine is { } line)
            {
                // Apply enter action as long as there are only whitespace lines between inherited line and this line.
                bool shouldApplyEnterRules = true;
                for (int inBetweenLine = line; inBetweenLine < lineNumber - 1; inBetweenLine++)
                {
                    if (!string.IsNullOrWhiteSpace(virtualModel.GetLineContent(inBetweenLine)))
                    {
                        shouldApplyEnterRules = false;
                        break;
                    }
                }

                if (shouldApplyEnterRules)
                {
                    EnterAction? enterResult = richEditSupport.OnEnter(autoIndent, "", virtualModel.GetLineContent(line), "");

                    if (enterResult is { } result)
                    {
                        string indentation = Strings.GetLeadingWhitespace(virtualModel.GetLineContent(line));

                        if (result.RemoveText != 0)
                        {
                            indentation = indentation[..(indentation.Length - result.RemoveText)];
                        }

                        if (result.IndentAction == IndentAction.Indent || result.IndentAction == IndentAction.IndentOutdent)
                        {
                            indentation = indentConverter.ShiftIndent(indentation);
                        }
                        else if (result.IndentAction == IndentAction.Outdent)
                        {
                            indentation = indentConverter.UnshiftIndent(indentation);
                        }

                        if (processedIndentRulesSupport.ShouldDecrease(lineNumber))
                        {
                            indentation = indentConverter.UnshiftIndent(indentation);
                        }

                        if (!string.IsNullOrEmpty(result.AppendText))
                        {
                            indentation += result.AppendText;
                        }

                        return Strings.GetLeadingWhitespace(indentation);
                    }
                }
            }

            if (processedIndentRulesSupport.ShouldDecrease(lineNumber))
            {
                if (inherited.Action == IndentAction.Indent)
                {
                    return inherited.Indentation;
                }
                else
                {
                    return indentConverter.UnshiftIndent(inherited.Indentation);
                }
            }
            else
            {
                if (inherited.Action == IndentAction.Indent)
                {
                    return indentConverter.ShiftIndent(inherited.Indentation);
                }
                else
                {
                    return inherited.Indentation;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Get nearest preceding line which doesn't match unIndentPattern or contains all whitespace.
    /// Result:
    /// -1: run into the boundary of embedded languages
    /// 0: every line above are invalid
    /// else: nearest preceding line of the same language
    /// </summary>
    private static int GetPrecedingValidLine(IVirtualModel model, int lineNumber, ProcessedIndentRulesSupport processedIndentRulesSupport)
    {
        // Monaco asks for column 0; column 1 is the same first position here (P5-16).
        string languageId = model.GetLanguageIdAtPosition(lineNumber, 1);
        if (lineNumber > 1)
        {
            int lastLineNumber;
            int resultLineNumber = -1;

            for (lastLineNumber = lineNumber - 1; lastLineNumber >= 1; lastLineNumber--)
            {
                // Monaco asks for column 0.
                if (model.GetLanguageIdAtPosition(lastLineNumber, 1) != languageId)
                {
                    return resultLineNumber;
                }

                string text = model.GetLineContent(lastLineNumber);
                if (processedIndentRulesSupport.ShouldIgnore(lastLineNumber) || string.IsNullOrWhiteSpace(text))
                {
                    resultLineNumber = lastLineNumber;
                    continue;
                }

                return lastLineNumber;
            }
        }

        return -1;
    }
}
