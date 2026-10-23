// Ported from microsoft/vscode: src/vs/editor/common/viewModel/viewModelImpl.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;
using LoomEditor.Core.Model;
using LoomEditor.Core.Model.Events;
using LoomEditor.Core.Text;
using LoomEditor.View.Cursors;
using LoomEditor.View.Events;

namespace LoomEditor.View.Layout;

/// <summary>
/// What the renderer and the control talk to: follows the <see cref="TextModel"/>, keeps <see cref="ViewLayout"/> and
/// <see cref="EditorLayoutInfo"/> up to date and builds a <see cref="ViewportData"/> per frame. Monaco: <c>ViewModel</c>
/// (subset).
/// </summary>
/// <remarks>
/// Threading: UI thread only; events are raised synchronously on the calling thread.
/// Behavior difference: the view lines are the ones of <see cref="ViewModelLinesFromProjectedModel"/> (hidden areas, word wrap); custom line heights and the view events that only the control consumed are not ported.
/// </remarks>
internal sealed partial class ViewModel : IDisposable, ICursorSimpleModel
{
    private readonly TextModel _model;
    private EditorLayoutOptions _layoutOptions;
    private ViewLayoutOptions _viewLayoutOptions;
    private FontInfo _fontInfo;
    private int _outerWidth;
    private int _outerHeight;
    private int _lineNumbersDigitCount;
    private int _layoutViewLineCount;
    private readonly ComputeOptionsMemory _layoutMemory = new();
    private bool _disposed;
    private string _breakBeforeCharacters;
    private string _breakAfterCharacters;
    private readonly ViewModelLinesFromProjectedModel _lines;
    private readonly CursorsController _cursor;
    private EditorCursorOptions _cursorOptions = EditorCursorOptions.Default;

    public ViewModel(
        TextModel model,
        FontInfo fontInfo,
        EditorLayoutOptions layoutOptions,
        ViewLayoutOptions viewLayoutOptions,
        Func<Action, IDisposable> scheduleAtNextAnimationFrame,
        TimeProvider timeProvider,
        int editorId = 0)
    {
        EditorId = editorId;
        _model = model;
        _fontInfo = fontInfo;
        _layoutOptions = layoutOptions;
        _viewLayoutOptions = viewLayoutOptions;
        _lineNumbersDigitCount = CountDigits(model.LineCount);
        _layoutViewLineCount = model.LineCount;
        LayoutInfo = ComputeLayoutInfo();
        EditorWrappingOptions wrapping = layoutOptions.Wrapping;
        _breakBeforeCharacters = wrapping.WordWrapBreakBeforeCharacters;
        _breakAfterCharacters = wrapping.WordWrapBreakAfterCharacters;
        _lines = new ViewModelLinesFromProjectedModel(
            editorId,
            model,
            MonospaceLineBreaksComputerFactory.Create(wrapping),
            fontInfo,
            model.Options.TabSize,
            LayoutInfo.WrappingColumn,
            wrapping.WrappingIndent,
            wrapping.WordBreak,
            wrapping.WrapOnEscapedLineFeeds,
            wrapping.UseTwoCellFullwidthCharacters);
        ViewLayout = new ViewLayout(
            viewLayoutOptions, LayoutInfo, fontInfo, _lines.GetViewLineCount(), [], scheduleAtNextAnimationFrame, timeProvider);

        EventDispatcher = new ViewModelEventDispatcher();
        CoordinatesConverter = _lines.CreateCoordinatesConverter();
        CursorConfig = CreateCursorConfiguration();
        _cursor = new CursorsController(model, this, CoordinatesConverter, CursorConfig);

        AttachDecorations();

        _model.RawContentChanged += OnModelRawContentChanged;
        _model.InjectedTextChanged += OnModelInjectedTextChanged;
        _model.OptionsChanged += OnModelOptionsChanged;
        _model.LanguageChanged += OnModelLanguageChanged;
        _model.LanguageConfigurationChanged += OnModelLanguageChanged;
        AttachToModelTokens(scheduleAtNextAnimationFrame);
        UpdateLayoutViewLineCount();
    }

    /// <summary>Raised after <see cref="SetOptions"/>, even when the layout did not change.</summary>
    public event EventHandler? OptionsChanged;

    /// <summary>Raised after <see cref="LayoutInfo"/> changed.</summary>
    public event EventHandler? LayoutInfoChanged;

    public TextModel Model => _model;

    public ViewLayout ViewLayout { get; }

    /// <summary>The id of the editor that owns this view model and its decorations; 0 when there is none (Monaco: <c>editorId</c>).</summary>
    public int EditorId { get; }

    public EditorLayoutInfo LayoutInfo { get; private set; }

    public FontInfo FontInfo => _fontInfo;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        DetachFromModelTokens();
        DetachDecorations();
        _model.RawContentChanged -= OnModelRawContentChanged;
        _model.InjectedTextChanged -= OnModelInjectedTextChanged;
        _model.OptionsChanged -= OnModelOptionsChanged;
        _model.LanguageChanged -= OnModelLanguageChanged;
        _model.LanguageConfigurationChanged -= OnModelLanguageChanged;
        _cursor.Dispose();
        _lines.Dispose();
        ViewLayout.Dispose();
    }

    /// <summary>Sets the size of the control, in DIPs, and recomputes the layout.</summary>
    public void SetViewportSize(int outerWidth, int outerHeight)
    {
        _outerWidth = outerWidth;
        _outerHeight = outerHeight;
        RecomputeLayoutInfo();
        UpdateWrappingSettings();
        UpdateCursorConfiguration();
    }

    /// <summary>Sets the measured font. The line height follows the font, like Monaco's <c>lineHeight</c> option.</summary>
    public void SetFontInfo(FontInfo fontInfo)
    {
        _fontInfo = fontInfo;
        ViewLayout.OnFontInfoChanged(fontInfo);

        if (_viewLayoutOptions.LineHeight != fontInfo.LineHeight)
        {
            _viewLayoutOptions = _viewLayoutOptions with { LineHeight = fontInfo.LineHeight };
            ViewLayout.OnOptionsChanged(_viewLayoutOptions);
        }

        RecomputeLayoutInfo();
        UpdateWrappingSettings();
        UpdateCursorConfiguration();
    }

    /// <summary>The layout options in use.</summary>
    public EditorLayoutOptions LayoutOptions => _layoutOptions;

    /// <summary>The scrolling options in use.</summary>
    public ViewLayoutOptions ViewLayoutOptions => _viewLayoutOptions;

    /// <summary>Sets the layout and scrolling options.</summary>
    public void SetOptions(EditorLayoutOptions layoutOptions, ViewLayoutOptions viewLayoutOptions)
    {
        _layoutOptions = layoutOptions;
        _viewLayoutOptions = viewLayoutOptions;
        ViewLayout.OnOptionsChanged(viewLayoutOptions);
        RecomputeLayoutInfo();
        UpdateWrappingSettings();
        OptionsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Builds the data to render the current viewport.</summary>
    public ViewportData GetViewportData(IReadOnlyList<Selection> selections)
    {
        return new ViewportData(
            selections,
            ViewLayout.GetLinesViewportData(),
            ViewLayout.GetWhitespaceViewportData(),
            this,
            _decorations);
    }

    // Monaco: onDidChangeContentOrInjectedText (the model change part; custom line heights are not ported)
    private void OnModelRawContentChanged(object? sender, ModelRawContentChangedEventArgs e)
    {
        OnDidChangeContentOrInjectedText(e.Changes, e.VersionId);

        // Monaco: emitContentChangeEvent runs after the layout has been updated (onDidChangeContentOrInjectedText).
        EmitViewEvent(eventsCollector => _cursor.OnModelContentChanged(eventsCollector, e));
    }

    // Monaco: an injected text change has no version id (versionId = null).
    private void OnModelInjectedTextChanged(object? sender, ModelInjectedTextChangedEventArgs e)
    {
        OnDidChangeContentOrInjectedText(e.Changes, null);
        EmitViewEvent(_cursor.OnModelInjectedTextChanged);
    }

    private void OnDidChangeContentOrInjectedText(IReadOnlyList<ModelRawChange> changes, int? versionId)
    {
        EmitViewEvent(eventsCollector =>
        {
            bool hadOtherModelChange = false;
            bool hadModelLineChangeThatChangedLineMapping = false;

            // Do a first pass to compute line mappings, and a second pass to actually interpret them
            Queue<ModelLineProjectionData?> lineBreakQueue = ComputeLineBreaks(changes);

            foreach (ModelRawChange change in changes)
            {
                switch (change)
                {
                    case ModelRawFlush:
                        _lines.OnModelFlushed();
                        _decorations.Reset();
                        ViewLayout.OnFlushed(GetLineCount(), []);
                        hadOtherModelChange = true;
                        break;
                    case ModelRawLinesDeleted deleted:
                        if (_lines.OnModelLinesDeleted(versionId, deleted.FromLineNumber, deleted.ToLineNumber) is { } linesDeleted)
                        {
                            ViewLayout.OnLinesDeleted(linesDeleted.FromLineNumber, linesDeleted.ToLineNumber);
                        }

                        hadOtherModelChange = true;
                        break;
                    case ModelRawLinesInserted inserted:
                        if (_lines.OnModelLinesInserted(versionId, inserted.FromLineNumber, inserted.ToLineNumber, TakeCount(lineBreakQueue, inserted.Count)) is { } linesInserted)
                        {
                            ViewLayout.OnLinesInserted(linesInserted.FromLineNumber, linesInserted.ToLineNumber);
                        }

                        hadOtherModelChange = true;
                        break;
                    case ModelRawLineChanged changed:
                        (bool lineMappingChanged, _, ViewLineRange? insertedRange, ViewLineRange? deletedRange) =
                            _lines.OnModelLineChanged(versionId, changed.LineNumber, lineBreakQueue.Dequeue());
                        hadModelLineChangeThatChangedLineMapping = lineMappingChanged;
                        if (insertedRange is { } insertedLines)
                        {
                            ViewLayout.OnLinesInserted(insertedLines.FromLineNumber, insertedLines.ToLineNumber);
                        }

                        if (deletedRange is { } deletedLines)
                        {
                            ViewLayout.OnLinesDeleted(deletedLines.FromLineNumber, deletedLines.ToLineNumber);
                        }

                        break;
                    default:
                        // ModelRawEolChanged: nothing to do. The new version is accepted below.
                        break;
                }
            }

            if (versionId is { } acceptedVersionId && _lines.AcceptVersionId(acceptedVersionId))
            {
                // Every line was hidden: the lines revealed one.
                ViewLayout.OnFlushed(GetLineCount(), []);
            }

            ViewLayout.OnHeightMaybeChanged();

            if (!hadOtherModelChange && hadModelLineChangeThatChangedLineMapping)
            {
                _cursor.OnLineMappingChanged(eventsCollector);
                _decorations.OnLineMappingChanged();
            }
        });

        int digitCount = CountDigits(_model.LineCount);
        if (digitCount != _lineNumbersDigitCount)
        {
            _lineNumbersDigitCount = digitCount;
            RecomputeLayoutInfo();
        }

        UpdateLayoutViewLineCount();
        HandleVisibleLinesChanged();
    }

    private EditorLayoutInfo ComputeLayoutInfo()
    {
        // Monaco: EditorLayoutInfoComputer.compute builds its input from the environment.
        return EditorLayoutInfoComputer.ComputeLayout(
            _layoutOptions,
            new EditorLayoutInfoComputerEnv(
                _outerWidth,
                _outerHeight,
                _fontInfo.LineHeight,
                _lineNumbersDigitCount,
                _fontInfo.TypicalHalfwidthCharacterWidth,
                _fontInfo.MaxDigitWidth,
                _fontInfo.PixelRatio,
                GlyphMarginDecorationLaneCount: 1,
                ViewLineCount: _layoutViewLineCount,
                Memory: _layoutMemory,
                ScrollBeyondLastLine: _viewLayoutOptions.ScrollBeyondLastLine,
                PaddingTop: _viewLayoutOptions.PaddingTop,
                PaddingBottom: _viewLayoutOptions.PaddingBottom));
    }

    // Monaco: _updateConfigurationViewLineCount (the minimap layout depends on the view line count)
    private void UpdateLayoutViewLineCount()
    {
        int viewLineCount = GetLineCount();
        if (viewLineCount == _layoutViewLineCount)
        {
            return;
        }

        _layoutViewLineCount = viewLineCount;
        if (_layoutOptions.Minimap.Enabled)
        {
            RecomputeLayoutInfo();
            UpdateWrappingSettings();
        }
    }

    private void RecomputeLayoutInfo()
    {
        EditorLayoutInfo layoutInfo = ComputeLayoutInfo();
        if (layoutInfo == LayoutInfo)
        {
            return;
        }

        LayoutInfo = layoutInfo;
        ViewLayout.OnLayoutInfoChanged(layoutInfo);
        LayoutInfoChanged?.Invoke(this, EventArgs.Empty);
    }

    private static int CountDigits(int lineCount)
    {
        return lineCount.ToString(CultureInfo.InvariantCulture).Length;
    }
}
