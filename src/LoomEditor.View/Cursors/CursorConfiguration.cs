// Ported from microsoft/vscode: src/vs/editor/common/cursorCommon.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Cursors;

/// <summary>The settings every cursor operation reads: tab size, page size, word separators, multi-cursor options...</summary>
/// <remarks>
/// <para>
/// Monaco: <c>CursorConfiguration</c>. The view model creates a new configuration whenever one of its inputs changes
/// (Monaco's <c>shouldRecreate</c> is not ported).
/// </para>
/// <para>
/// Phase 4–5 cut points: <c>autoClosingPairs</c>, <c>surroundingPairs</c>, <c>blockCommentStartToken</c>,
/// <c>shouldAutoCloseBefore</c>, <c>electricChars</c>, <c>onElectricCharacter</c>, <c>_getShouldAutoClose</c>, the
/// language id and the <c>languageConfigurationService</c> are not ported. Phase 3 behaves as Monaco does for a model
/// with no language configuration.
/// </para>
/// <para>Threading: immutable; thread-safe.</para>
/// </remarks>
internal sealed class CursorConfiguration
{
    public CursorConfiguration(TextModelOptions modelOptions, EditorCursorOptions options, FontInfo fontInfo, EditorLayoutInfo layoutInfo)
    {
        ReadOnly = options.ReadOnly;
        TabSize = modelOptions.TabSize;
        IndentSize = modelOptions.IndentSize;
        InsertSpaces = modelOptions.InsertSpaces;
        StickyTabStops = options.StickyTabStops;
        LineHeight = fontInfo.LineHeight;
        TypicalHalfwidthCharacterWidth = fontInfo.TypicalHalfwidthCharacterWidth;
        PageSize = Math.Max(1, (int)Math.Floor((double)layoutInfo.Height / LineHeight) - 2);
        UseTabStops = options.UseTabStops;
        TrimWhitespaceOnDelete = options.TrimWhitespaceOnDelete;
        WordSeparators = options.WordSeparators;
        EmptySelectionClipboard = options.EmptySelectionClipboard;
        CopyWithSyntaxHighlighting = options.CopyWithSyntaxHighlighting;
        MultiCursorMergeOverlapping = options.MultiCursorMergeOverlapping;
        MultiCursorPaste = options.MultiCursorPaste;
        MultiCursorLimit = options.MultiCursorLimit;
        AutoClosingBrackets = options.AutoClosingBrackets;
        AutoClosingComments = options.AutoClosingComments;
        AutoClosingQuotes = options.AutoClosingQuotes;
        AutoClosingDelete = options.AutoClosingDelete;
        AutoClosingOvertype = options.AutoClosingOvertype;
        AutoSurround = options.AutoSurround;
        AutoIndent = options.AutoIndent;
        WordSegmenterLocales = options.WordSegmenterLocales;
        OvertypeOnPaste = options.OvertypeOnPaste;
    }

    public bool ReadOnly { get; }

    public int TabSize { get; }

    public int IndentSize { get; }

    public bool InsertSpaces { get; }

    public bool StickyTabStops { get; }

    public int PageSize { get; }

    public int LineHeight { get; }

    public double TypicalHalfwidthCharacterWidth { get; }

    public bool UseTabStops { get; }

    public bool TrimWhitespaceOnDelete { get; }

    public string WordSeparators { get; }

    public bool EmptySelectionClipboard { get; }

    public bool CopyWithSyntaxHighlighting { get; }

    public bool MultiCursorMergeOverlapping { get; }

    public MultiCursorPasteMode MultiCursorPaste { get; }

    public int MultiCursorLimit { get; }

    public EditorAutoClosingStrategy AutoClosingBrackets { get; }

    public EditorAutoClosingStrategy AutoClosingComments { get; }

    public EditorAutoClosingStrategy AutoClosingQuotes { get; }

    public EditorAutoClosingEditStrategy AutoClosingDelete { get; }

    public EditorAutoClosingEditStrategy AutoClosingOvertype { get; }

    public EditorAutoSurroundStrategy AutoSurround { get; }

    public EditorAutoIndentStrategy AutoIndent { get; }

    public IReadOnlyList<string> WordSegmenterLocales { get; }

    public bool OvertypeOnPaste { get; }

    // Monaco reads a global (InputMode.getInputMode()) that is not ported: overtype mode is out of phase 3 (ADR 0006,
    // decision 12), so it is always Insert. It stays an instance member, as the operations read it from the configuration.
    private readonly InputMode _inputMode = InputMode.Insert;

    public InputMode InputMode => _inputMode;

    public string NormalizeIndentation(string str) => Indentation.NormalizeIndentation(str, IndentSize, InsertSpaces);

    /// <summary>The visible column of a position (see <see cref="CursorColumns"/>).</summary>
    public int VisibleColumnFromColumn(ICursorSimpleModel model, Position position) =>
        CursorColumns.VisibleColumnFromColumn(model.GetLineContent(position.LineNumber), position.Column, TabSize);

    /// <summary>The column of a visible column, clamped to the line's min and max column (see <see cref="CursorColumns"/>).</summary>
    public int ColumnFromVisibleColumn(ICursorSimpleModel model, int lineNumber, int visibleColumn)
    {
        int result = CursorColumns.ColumnFromVisibleColumn(model.GetLineContent(lineNumber), visibleColumn, TabSize);

        int minColumn = model.GetLineMinColumn(lineNumber);
        if (result < minColumn)
        {
            return minColumn;
        }

        int maxColumn = model.GetLineMaxColumn(lineNumber);
        if (result > maxColumn)
        {
            return maxColumn;
        }

        return result;
    }
}
