// Ported from microsoft/vscode: src/vs/editor/common/commands/shiftCommand.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Text;
using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors.Commands;

/// <summary>The options of a <see cref="ShiftCommand"/> (Monaco: <c>IShiftCommandOpts</c>).</summary>
internal sealed record ShiftCommandOptions(
    bool IsUnshift,
    int TabSize,
    int IndentSize,
    bool InsertSpaces,
    bool UseTabStops,
    EditorAutoIndentStrategy AutoIndent);

/// <summary>Indents or outdents the lines of a selection (Monaco: <c>ShiftCommand</c>).</summary>
/// <remarks>
/// <para>
/// Behavior difference: Monaco repeats the indent with a global cache (<c>cachedStringRepeat</c>); this port builds the
/// string each time. Same result, performance only.
/// </para>
/// <para>Threading: UI thread only.</para>
/// </remarks>
internal sealed class ShiftCommand : IEditCommand
{
    private readonly ShiftCommandOptions _opts;
    private readonly LanguageConfigurationRegistry _languageConfigurationService;
    private readonly Selection _selection;
    private string? _selectionId;
    private bool _useLastEditRangeForCursorEndPosition;
    private bool _selectionStartColumnStaysPut;

    /// <summary>Creates a command that shifts the lines touched by <paramref name="range"/>.</summary>
    public ShiftCommand(Selection range, ShiftCommandOptions opts, LanguageConfigurationRegistry languageConfigurationService)
    {
        ArgumentNullException.ThrowIfNull(opts);
        ArgumentNullException.ThrowIfNull(languageConfigurationService);
        _languageConfigurationService = languageConfigurationService;
        _opts = opts;
        _selection = range;
    }

    public bool InsertsAutoWhitespace => false;

    /// <summary>The indentation a line gets when it is outdented (Monaco: <c>ShiftCommand.unshiftIndent</c>).</summary>
    public static string UnshiftIndent(string line, int column, int tabSize, int indentSize, bool insertSpaces)
    {
        // Determine the visible column where the content starts
        int contentStartVisibleColumn = CursorColumns.VisibleColumnFromColumn(line, column, tabSize);

        if (insertSpaces)
        {
            int desiredTabStop = CursorColumns.PrevIndentTabStop(contentStartVisibleColumn, indentSize);
            int indentCount = desiredTabStop / indentSize; // will be an integer
            return Repeat(new string(' ', indentSize), indentCount);
        }
        else
        {
            int desiredTabStop = CursorColumns.PrevRenderTabStop(contentStartVisibleColumn, tabSize);
            int indentCount = desiredTabStop / tabSize; // will be an integer
            return Repeat("\t", indentCount);
        }
    }

    /// <summary>The indentation a line gets when it is indented (Monaco: <c>ShiftCommand.shiftIndent</c>).</summary>
    public static string ShiftIndent(string line, int column, int tabSize, int indentSize, bool insertSpaces)
    {
        // Determine the visible column where the content starts
        int contentStartVisibleColumn = CursorColumns.VisibleColumnFromColumn(line, column, tabSize);

        if (insertSpaces)
        {
            int desiredTabStop = CursorColumns.NextIndentTabStop(contentStartVisibleColumn, indentSize);
            int indentCount = desiredTabStop / indentSize; // will be an integer
            return Repeat(new string(' ', indentSize), indentCount);
        }
        else
        {
            int desiredTabStop = CursorColumns.NextRenderTabStop(contentStartVisibleColumn, tabSize);
            int indentCount = desiredTabStop / tabSize; // will be an integer
            return Repeat("\t", indentCount);
        }
    }

    public void GetEditOperations(TextModel model, IEditOperationBuilder builder)
    {
        Range selection = _selection.Range;
        int startLine = selection.StartLineNumber;

        int endLine = selection.EndLineNumber;
        if (selection.EndColumn == 1 && startLine != endLine)
        {
            endLine--;
        }

        int tabSize = _opts.TabSize;
        int indentSize = _opts.IndentSize;
        bool insertSpaces = _opts.InsertSpaces;
        bool shouldIndentEmptyLines = startLine == endLine;

        if (_opts.UseTabStops)
        {
            // if indenting or outdenting on a whitespace only line
            if (_selection.IsEmpty && Strings.IsJavaScriptWhitespaceOnly(model.GetLineContent(startLine)))
            {
                _useLastEditRangeForCursorEndPosition = true;
            }

            // keep track of previous line's "miss-alignment"
            int previousLineExtraSpaces = 0;
            int extraSpaces = 0;
            for (int lineNumber = startLine; lineNumber <= endLine; lineNumber++, previousLineExtraSpaces = extraSpaces)
            {
                extraSpaces = 0;
                string lineText = model.GetLineContent(lineNumber);
                int indentationEndIndex = StringClassification.FirstNonWhitespaceIndex(lineText);

                if (_opts.IsUnshift && (lineText.Length == 0 || indentationEndIndex == 0))
                {
                    // empty line or line with no leading whitespace => nothing to do
                    continue;
                }

                if (!shouldIndentEmptyLines && !_opts.IsUnshift && lineText.Length == 0)
                {
                    // do not indent empty lines => nothing to do
                    continue;
                }

                if (indentationEndIndex == -1)
                {
                    // the entire line is whitespace
                    indentationEndIndex = lineText.Length;
                }

                if (lineNumber > 1)
                {
                    int contentStartVisibleColumn = CursorColumns.VisibleColumnFromColumn(lineText, indentationEndIndex + 1, tabSize);
                    if (contentStartVisibleColumn % indentSize != 0)
                    {
                        // The current line is "miss-aligned", so let's see if this is expected...
                        // This can only happen when it has trailing commas in the indent
                        if (model.Tokenization.IsCheapToTokenize(lineNumber - 1))
                        {
                            CompleteEnterAction? enterAction = EnterActions.GetEnterAction(
                                _opts.AutoIndent,
                                model,
                                new Range(lineNumber - 1, model.GetLineMaxColumn(lineNumber - 1), lineNumber - 1, model.GetLineMaxColumn(lineNumber - 1)),
                                _languageConfigurationService);
                            if (enterAction is { } action)
                            {
                                extraSpaces = previousLineExtraSpaces;
                                if (action.AppendText.Length > 0)
                                {
                                    for (int j = 0; j < action.AppendText.Length && extraSpaces < indentSize; j++)
                                    {
                                        if (action.AppendText[j] == ' ')
                                        {
                                            extraSpaces++;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }

                                if (action.RemoveText > 0)
                                {
                                    extraSpaces = Math.Max(0, extraSpaces - action.RemoveText);
                                }

                                // Act as if `prefixSpaces` is not part of the indentation
                                for (int j = 0; j < extraSpaces; j++)
                                {
                                    if (indentationEndIndex == 0 || lineText[indentationEndIndex - 1] != ' ')
                                    {
                                        break;
                                    }

                                    indentationEndIndex--;
                                }
                            }
                        }
                    }
                }

                if (_opts.IsUnshift && indentationEndIndex == 0)
                {
                    // line with no leading whitespace => nothing to do
                    continue;
                }

                string desiredIndent = _opts.IsUnshift
                    ? UnshiftIndent(lineText, indentationEndIndex + 1, tabSize, indentSize, insertSpaces)
                    : ShiftIndent(lineText, indentationEndIndex + 1, tabSize, indentSize, insertSpaces);

                AddEditOperation(builder, new Range(lineNumber, 1, lineNumber, indentationEndIndex + 1), desiredIndent);
                if (lineNumber == startLine && !_selection.IsEmpty)
                {
                    // Force the startColumn to stay put because we're inserting after it
                    _selectionStartColumnStaysPut = selection.StartColumn <= indentationEndIndex + 1;
                }
            }

        }
        else
        {
            // if indenting or outdenting on a whitespace only line
            if (!_opts.IsUnshift && _selection.IsEmpty && model.GetLineLength(startLine) == 0)
            {
                _useLastEditRangeForCursorEndPosition = true;
            }

            string oneIndent = insertSpaces ? new string(' ', indentSize) : "\t";

            for (int lineNumber = startLine; lineNumber <= endLine; lineNumber++)
            {
                string lineText = model.GetLineContent(lineNumber);
                int indentationEndIndex = StringClassification.FirstNonWhitespaceIndex(lineText);

                if (_opts.IsUnshift && (lineText.Length == 0 || indentationEndIndex == 0))
                {
                    // empty line or line with no leading whitespace => nothing to do
                    continue;
                }

                if (!shouldIndentEmptyLines && !_opts.IsUnshift && lineText.Length == 0)
                {
                    // do not indent empty lines => nothing to do
                    continue;
                }

                if (indentationEndIndex == -1)
                {
                    // the entire line is whitespace
                    indentationEndIndex = lineText.Length;
                }

                if (_opts.IsUnshift && indentationEndIndex == 0)
                {
                    // line with no leading whitespace => nothing to do
                    continue;
                }

                if (_opts.IsUnshift)
                {
                    indentationEndIndex = Math.Min(indentationEndIndex, indentSize);
                    for (int i = 0; i < indentationEndIndex; i++)
                    {
                        if (lineText[i] == '\t')
                        {
                            indentationEndIndex = i + 1;
                            break;
                        }
                    }

                    AddEditOperation(builder, new Range(lineNumber, 1, lineNumber, indentationEndIndex + 1), string.Empty);
                }
                else
                {
                    AddEditOperation(builder, new Range(lineNumber, 1, lineNumber, 1), oneIndent);
                    if (lineNumber == startLine && !_selection.IsEmpty)
                    {
                        // Force the startColumn to stay put because we're inserting after it
                        _selectionStartColumnStaysPut = selection.StartColumn == 1;
                    }
                }
            }
        }

        _selectionId = builder.TrackSelection(_selection);
    }

    public Selection ComputeCursorState(TextModel model, ICursorStateComputerData helper)
    {
        if (_useLastEditRangeForCursorEndPosition)
        {
            Range lastRange = helper.GetInverseEditOperations()[0].Range;
            return new Selection(lastRange.EndLineNumber, lastRange.EndColumn, lastRange.EndLineNumber, lastRange.EndColumn);
        }

        Selection result = helper.GetTrackedSelection(_selectionId!);

        if (_selectionStartColumnStaysPut)
        {
            // The selection start should not move
            int initialStartColumn = _selection.Range.StartColumn;
            int resultStartColumn = result.Range.StartColumn;
            if (resultStartColumn <= initialStartColumn)
            {
                return result;
            }

            Range range = result.Range;
            if (result.Direction == SelectionDirection.LeftToRight)
            {
                return new Selection(range.StartLineNumber, initialStartColumn, range.EndLineNumber, range.EndColumn);
            }

            return new Selection(range.EndLineNumber, range.EndColumn, range.StartLineNumber, initialStartColumn);
        }

        return result;
    }

    private void AddEditOperation(IEditOperationBuilder builder, Range range, string text)
    {
        if (_useLastEditRangeForCursorEndPosition)
        {
            builder.AddTrackedEditOperation(range, text);
        }
        else
        {
            builder.AddEditOperation(range, text);
        }
    }

    private static string Repeat(string text, int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length * count);
        for (int i = 0; i < count; i++)
        {
            builder.Append(text);
        }

        return builder.ToString();
    }
}
