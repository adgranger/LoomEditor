// Ported from microsoft/vscode: src/vs/editor/common/cursorCommon.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Languages;
using LoomEditor.Core.Languages.Supports;
using LoomEditor.Core.Model;
using LoomEditor.Core.Text;
using LoomEditor.Core.Tokens;
using LoomEditor.View.Layout;

namespace LoomEditor.View.Cursors;

/// <summary>The settings every cursor operation reads: tab size, page size, word separators, multi-cursor options...</summary>
/// <remarks>
/// <para>
/// Monaco: <c>CursorConfiguration</c>. The view model creates a new configuration whenever one of its inputs changes
/// (Monaco's <c>shouldRecreate</c> is not ported).
/// </para>
/// <para>Threading: immutable; thread-safe.</para>
/// </remarks>
/// <summary>Tells whether the text after an auto-closed character allows closing it (Monaco: <c>shouldAutoCloseBefore</c>; the predicates take the first character after the cursor).</summary>
internal sealed record AutoCloseBeforePredicates(Func<char, bool> Quote, Func<char, bool> Bracket, Func<char, bool> Comment);

internal sealed class CursorConfiguration
{
    private static readonly Func<char, bool> AutoCloseAlways = _ => true;
    private static readonly Func<char, bool> AutoCloseNever = _ => false;
    private static readonly Func<char, bool> AutoCloseBeforeWhitespace = c => c == ' ' || c == (char)9; // space or tab

    private readonly string _languageId;
    private IReadOnlySet<string>? _electricChars;

    public CursorConfiguration(
        string languageId,
        TextModelOptions modelOptions,
        EditorCursorOptions options,
        FontInfo fontInfo,
        EditorLayoutInfo layoutInfo,
        LanguageConfigurationRegistry languageConfigurationService)
    {
        _languageId = languageId;
        LanguageConfigurationService = languageConfigurationService;
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
        InputMode = options.InputMode;

        ShouldAutoCloseBefore = new AutoCloseBeforePredicates(
            GetShouldAutoClose(languageId, AutoClosingQuotes, forQuotes: true),
            GetShouldAutoClose(languageId, AutoClosingBrackets, forQuotes: false),
            GetShouldAutoClose(languageId, AutoClosingComments, forQuotes: false));

        ResolvedLanguageConfiguration languageConfiguration = languageConfigurationService.GetLanguageConfiguration(languageId);
        AutoClosingPairs = languageConfiguration.GetAutoClosingPairs();

        var surroundingPairs = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (IAutoClosingPair pair in languageConfiguration.GetSurroundingPairs())
        {
            surroundingPairs[pair.Open] = pair.Close;
        }

        SurroundingPairs = surroundingPairs;
        BlockCommentStartToken = languageConfiguration.Comments?.BlockCommentStartToken;
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

    /// <summary>The language configurations (Monaco: <c>languageConfigurationService</c>).</summary>
    public LanguageConfigurationRegistry LanguageConfigurationService { get; }

    public AutoClosingPairs AutoClosingPairs { get; }

    /// <summary>The surrounding pairs, open to close.</summary>
    public IReadOnlyDictionary<string, string> SurroundingPairs { get; }

    public string? BlockCommentStartToken { get; }

    public AutoCloseBeforePredicates ShouldAutoCloseBefore { get; }

    /// <summary>The electric characters of the language, computed on first read.</summary>
    public IReadOnlySet<string> ElectricChars =>
        _electricChars ??= new HashSet<string>(
            LanguageConfigurationService.GetLanguageConfiguration(_languageId).ElectricCharacter.GetElectricCharacters(),
            StringComparer.Ordinal);

    /// <summary>The input mode of the editor (Monaco: <c>InputMode.getInputMode()</c>, a global there).</summary>
    public EditorInputMode InputMode { get; }

    /// <summary>Returns the opening bracket type to match indentation with (Monaco: <c>onElectricCharacter</c>).</summary>
    public ElectricAction? OnElectricCharacter(string character, LineTokens context, int column)
    {
        ScopedLineTokens scopedLineTokens = ScopedLineTokens.Create(context, column - 1);
        BracketElectricCharacterSupport electricCharacterSupport =
            LanguageConfigurationService.GetLanguageConfiguration(scopedLineTokens.LanguageId).ElectricCharacter;
        return electricCharacterSupport.OnElectricCharacter(character, scopedLineTokens, column - scopedLineTokens.FirstCharOffset);
    }

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

    private Func<char, bool> GetShouldAutoClose(string languageId, EditorAutoClosingStrategy autoCloseConfig, bool forQuotes) => autoCloseConfig switch
    {
        EditorAutoClosingStrategy.BeforeWhitespace => AutoCloseBeforeWhitespace,
        EditorAutoClosingStrategy.LanguageDefined => GetLanguageDefinedShouldAutoClose(languageId, forQuotes),
        EditorAutoClosingStrategy.Always => AutoCloseAlways,
        EditorAutoClosingStrategy.Never => AutoCloseNever,
        _ => throw new ArgumentOutOfRangeException(nameof(autoCloseConfig)),
    };

    private Func<char, bool> GetLanguageDefinedShouldAutoClose(string languageId, bool forQuotes)
    {
        string autoCloseBeforeSet = LanguageConfigurationService.GetLanguageConfiguration(languageId).GetAutoCloseBeforeSet(forQuotes);
        return c => autoCloseBeforeSet.Contains(c, StringComparison.Ordinal);
    }
}
