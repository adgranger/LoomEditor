// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.View.Cursors;

/// <summary>The editor options that the cursor reads, with Monaco's defaults.</summary>
/// <remarks>
/// <para>
/// Not in Monaco: Monaco reads these through <c>IEditorConfiguration.options.get(EditorOption.…)</c>. The defaults are
/// those of <c>editorOptions.ts</c>. Monaco's <c>emptySelectionClipboard</c> is also <c>true</c> only when the environment
/// supports it (<c>env.emptySelectionClipboard</c>), which is always the case here.
/// </para>
/// <para>Threading: immutable; thread-safe.</para>
/// </remarks>
internal sealed record EditorCursorOptions
{
    public static EditorCursorOptions Default { get; } = new();

    public bool ReadOnly { get; init; }

    public bool StickyTabStops { get; init; }

    public bool UseTabStops { get; init; } = true;

    public bool TrimWhitespaceOnDelete { get; init; }

    /// <summary>The <c>columnSelection</c> option: Shift+arrows select a column (default <c>false</c>). Read by the keybindings.</summary>
    public bool ColumnSelection { get; init; }

    /// <summary>The <c>cursorSurroundingLines</c> option: lines kept visible around the cursor when it is revealed (default 0).</summary>
    public int CursorSurroundingLines { get; init; }

    /// <summary>The <c>cursorSurroundingLinesStyle</c> option: when <see cref="CursorSurroundingLines"/> is enforced.</summary>
    public CursorSurroundingLinesStyle CursorSurroundingLinesStyle { get; init; } = CursorSurroundingLinesStyle.Default;

    /// <summary>The <c>revealHorizontalRightPadding</c> option: pixels kept free right of a revealed range (default 15).</summary>
    public int RevealHorizontalRightPadding { get; init; } = 15;

    public string WordSeparators { get; init; } = WordHelper.UsualWordSeparators;

    public bool EmptySelectionClipboard { get; init; } = true;

    public bool CopyWithSyntaxHighlighting { get; init; } = true;

    public bool MultiCursorMergeOverlapping { get; init; } = true;

    public MultiCursorPasteMode MultiCursorPaste { get; init; } = MultiCursorPasteMode.Spread;

    public int MultiCursorLimit { get; init; } = 10000;

    public EditorAutoClosingStrategy AutoClosingBrackets { get; init; } = EditorAutoClosingStrategy.LanguageDefined;

    public EditorAutoClosingStrategy AutoClosingComments { get; init; } = EditorAutoClosingStrategy.LanguageDefined;

    public EditorAutoClosingStrategy AutoClosingQuotes { get; init; } = EditorAutoClosingStrategy.LanguageDefined;

    public EditorAutoClosingEditStrategy AutoClosingDelete { get; init; } = EditorAutoClosingEditStrategy.Auto;

    public EditorAutoClosingEditStrategy AutoClosingOvertype { get; init; } = EditorAutoClosingEditStrategy.Auto;

    public EditorAutoSurroundStrategy AutoSurround { get; init; } = EditorAutoSurroundStrategy.LanguageDefined;

    public EditorAutoIndentStrategy AutoIndent { get; init; } = EditorAutoIndentStrategy.Full;

    public IReadOnlyList<string> WordSegmenterLocales { get; init; } = [];

    public bool OvertypeOnPaste { get; init; } = true;
}
