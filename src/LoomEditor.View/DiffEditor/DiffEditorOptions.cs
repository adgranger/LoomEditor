// Ported from microsoft/vscode: src/vs/editor/browser/widget/diffEditor/diffEditorOptions.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.DiffEditor;

/// <summary>The names of the options that changed in a <see cref="DiffEditorOptions"/> update (each is a <c>nameof</c>).</summary>
/// <param name="changedOptions">The changed option names.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed class DiffEditorOptionsChangedEventArgs(IReadOnlySet<string> changedOptions) : EventArgs
{
    /// <summary>The changed option names.</summary>
    public IReadOnlySet<string> ChangedOptions { get; } = changedOptions;
}

/// <summary>
/// The settings of a diff editor, with Monaco's defaults and validation, and the rule that picks side by side or inline from the width.
/// Not ported: <c>renderMarginRevertIcon</c>, <c>renderGutterMenu</c>, <c>maxFileSize</c>, <c>diffCodeLens</c>, <c>diffWordWrap</c>,
/// <c>diffAlgorithm</c>, <c>accessibilityVerbose</c>, <c>experimental.useTrueInlineView</c>, <c>isInEmbeddedEditor</c>,
/// <c>onlyShowAccessibleDiffViewer</c>, <c>compactMode</c>, <c>trueInlineDiff*</c>, <c>shouldRenderOldRevertArrows</c>,
/// <c>shouldRenderGutterMenu</c>, <c>temporaryInlineMode</c> and the smooth resize start width: the width based layout is therefore
/// always "auto" and <see cref="SetWidth"/> has no <c>smoothResizeStartWidth</c>. <c>IAccessibilityService</c> is replaced by
/// <see cref="IsScreenReaderOptimized"/>, which the control sets.
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed class DiffEditorOptions
{
    private bool _enableSplitViewResizing = true;
    private double _splitViewDefaultRatio = 0.5;
    private bool _renderSideBySide = true;
    private int _maxComputationTimeMs = 5000;
    private bool _ignoreTrimWhitespace = true;
    private bool _renderIndicators = true;
    private bool _originalEditable;
    private bool _readOnly;
    private bool _renderOverviewRuler = true;
    private bool _showMoves;
    private bool _showEmptyDecorations = true;
    private bool _hideOriginalLineNumbers;
    private int _renderSideBySideInlineBreakpoint = 900;
    private bool _useInlineViewWhenSpaceIsLimited = true;
    private HideUnchangedRegionsOptions _hideUnchangedRegions = new();
    private bool _isScreenReaderOptimized;
    private double _width;

    /// <summary>Raised after one or more options changed, with their names.</summary>
    public event EventHandler<DiffEditorOptionsChangedEventArgs>? Changed;

    /// <summary>Whether the sash between the two sides can be dragged. Default true.</summary>
    public bool EnableSplitViewResizing { get => _enableSplitViewResizing; set => Set(ref _enableSplitViewResizing, value, nameof(EnableSplitViewResizing)); }

    /// <summary>The initial share of the width of the original side, clamped to 0.1–0.9. Default 0.5.</summary>
    public double SplitViewDefaultRatio { get => _splitViewDefaultRatio; set => Set(ref _splitViewDefaultRatio, Math.Clamp(value, 0.1, 0.9), nameof(SplitViewDefaultRatio)); }

    /// <summary>Side by side (true) or inline (false). Default true.</summary>
    public bool RenderSideBySide { get => _renderSideBySide; set => Set(ref _renderSideBySide, value, nameof(RenderSideBySide)); }

    /// <summary>The time budget of a diff computation, in ms, clamped to at least 0. Default 5000.</summary>
    public int MaxComputationTimeMs { get => _maxComputationTimeMs; set => Set(ref _maxComputationTimeMs, Math.Max(0, value), nameof(MaxComputationTimeMs)); }

    /// <summary>Whether leading and trailing whitespace changes are ignored. Default true.</summary>
    public bool IgnoreTrimWhitespace { get => _ignoreTrimWhitespace; set => Set(ref _ignoreTrimWhitespace, value, nameof(IgnoreTrimWhitespace)); }

    /// <summary>Whether the +/- indicators are drawn. Default true.</summary>
    public bool RenderIndicators { get => _renderIndicators; set => Set(ref _renderIndicators, value, nameof(RenderIndicators)); }

    /// <summary>Whether the original side can be edited. Default false.</summary>
    public bool OriginalEditable { get => _originalEditable; set => Set(ref _originalEditable, value, nameof(OriginalEditable)); }

    /// <summary>Whether the modified side is read-only. Default false.</summary>
    public bool ReadOnly { get => _readOnly; set => Set(ref _readOnly, value, nameof(ReadOnly)); }

    /// <summary>Whether the overview ruler is drawn. Default true.</summary>
    public bool RenderOverviewRuler { get => _renderOverviewRuler; set => Set(ref _renderOverviewRuler, value, nameof(RenderOverviewRuler)); }

    /// <summary>Whether moved blocks are detected and shown (Monaco: <c>experimental.showMoves</c>). Default false.</summary>
    public bool ShowMoves { get => _showMoves; set => Set(ref _showMoves, value, nameof(ShowMoves)); }

    /// <summary>Whether empty ranges get a decoration (Monaco: <c>experimental.showEmptyDecorations</c>). Default true.</summary>
    public bool ShowEmptyDecorations { get => _showEmptyDecorations; set => Set(ref _showEmptyDecorations, value, nameof(ShowEmptyDecorations)); }

    /// <summary>Whether the inline view hides the original line numbers. Default false.</summary>
    public bool HideOriginalLineNumbers { get => _hideOriginalLineNumbers; set => Set(ref _hideOriginalLineNumbers, value, nameof(HideOriginalLineNumbers)); }

    /// <summary>The width at or below which the inline view may replace side by side, clamped to at least 0. Default 900.</summary>
    public int RenderSideBySideInlineBreakpoint
    {
        get => _renderSideBySideInlineBreakpoint;
        set => Set(ref _renderSideBySideInlineBreakpoint, Math.Max(0, value), nameof(RenderSideBySideInlineBreakpoint));
    }

    /// <summary>Whether a narrow editor switches to the inline view. Default true.</summary>
    public bool UseInlineViewWhenSpaceIsLimited
    {
        get => _useInlineViewWhenSpaceIsLimited;
        set => Set(ref _useInlineViewWhenSpaceIsLimited, value, nameof(UseInlineViewWhenSpaceIsLimited));
    }

    /// <summary>The unchanged regions settings; replaced as a whole, counts clamped to at least 0.</summary>
    public HideUnchangedRegionsOptions HideUnchangedRegions
    {
        get => _hideUnchangedRegions;
        set => Set(ref _hideUnchangedRegions, value.Validate(), nameof(HideUnchangedRegions));
    }

    /// <summary>Whether a screen reader is in use (set by the control). Default false.</summary>
    public bool IsScreenReaderOptimized { get => _isScreenReaderOptimized; set => Set(ref _isScreenReaderOptimized, value, nameof(IsScreenReaderOptimized)); }

    /// <summary>Monaco: <c>couldShowInlineViewBecauseOfSize</c>.</summary>
    public bool CouldShowInlineViewBecauseOfSize => _renderSideBySide && _width <= _renderSideBySideInlineBreakpoint;

    /// <summary>Monaco: <c>renderSideBySideInAutomaticMode</c>.</summary>
    public bool RenderSideBySideInAutomaticMode => _width > _renderSideBySideInlineBreakpoint || _isScreenReaderOptimized;

    /// <summary>Monaco: <c>renderSideBySide</c> (derived): the layout actually drawn.</summary>
    public bool RenderSideBySideEffective =>
        _renderSideBySide && !(_useInlineViewWhenSpaceIsLimited && CouldShowInlineViewBecauseOfSize && !_isScreenReaderOptimized);

    /// <summary>Monaco: <c>showMoves</c> (derived).</summary>
    public bool ShowMovesEffective => _showMoves && RenderSideBySideEffective;

    /// <summary>Monaco: <c>setWidth</c>, without <c>smoothResizeStartWidth</c>.</summary>
    public void SetWidth(double width)
    {
        bool before = RenderSideBySideEffective;
        _width = width;
        if (before != RenderSideBySideEffective)
        {
            Changed?.Invoke(this, new DiffEditorOptionsChangedEventArgs(new HashSet<string> { nameof(RenderSideBySideEffective) }));
        }
    }

    private void Set<T>(ref T field, T value, string name)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        bool before = RenderSideBySideEffective;
        field = value;
        HashSet<string> names = [name];
        if (before != RenderSideBySideEffective)
        {
            names.Add(nameof(RenderSideBySideEffective));
        }

        Changed?.Invoke(this, new DiffEditorOptionsChangedEventArgs(names));
    }
}
