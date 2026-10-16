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
/// (phase 2 subset).
/// </summary>
/// <remarks>
/// Threading: UI thread only; events are raised synchronously on the calling thread.
/// Behavior difference: in phase 2 view lines are model lines (identity mapping, no word wrap, folding, injected text,
/// decorations or view events), so only the <c>viewLayout.onFlushed</c> / <c>onLinesDeleted</c> / <c>onLinesInserted</c>
/// calls of <c>onDidChangeContentOrInjectedText</c> are ported. Projections come in phase 6.
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
    private bool _disposed;
    private readonly CursorsController _cursor;
    private EditorCursorOptions _cursorOptions = EditorCursorOptions.Default;

    public ViewModel(
        TextModel model,
        FontInfo fontInfo,
        EditorLayoutOptions layoutOptions,
        ViewLayoutOptions viewLayoutOptions,
        Func<Action, IDisposable> scheduleAtNextAnimationFrame,
        TimeProvider timeProvider)
    {
        _model = model;
        _fontInfo = fontInfo;
        _layoutOptions = layoutOptions;
        _viewLayoutOptions = viewLayoutOptions;
        _lineNumbersDigitCount = CountDigits(model.LineCount);

        LayoutInfo = ComputeLayoutInfo();
        ViewLayout = new ViewLayout(
            viewLayoutOptions, LayoutInfo, fontInfo, model.LineCount, [], scheduleAtNextAnimationFrame, timeProvider);

        EventDispatcher = new ViewModelEventDispatcher();
        CoordinatesConverter = new IdentityCoordinatesConverter(model);
        CursorConfig = CreateCursorConfiguration();
        _cursor = new CursorsController(model, this, CoordinatesConverter, CursorConfig);

        _model.RawContentChanged += OnModelRawContentChanged;
        _model.OptionsChanged += OnModelOptionsChanged;
    }

    /// <summary>Raised after <see cref="LayoutInfo"/> changed.</summary>
    public event EventHandler? LayoutInfoChanged;

    public TextModel Model => _model;

    public ViewLayout ViewLayout { get; }

    public EditorLayoutInfo LayoutInfo { get; private set; }

    public FontInfo FontInfo => _fontInfo;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _model.RawContentChanged -= OnModelRawContentChanged;
        _model.OptionsChanged -= OnModelOptionsChanged;
        _cursor.Dispose();
        ViewLayout.Dispose();
    }

    /// <summary>Sets the size of the control, in DIPs, and recomputes the layout.</summary>
    public void SetViewportSize(int outerWidth, int outerHeight)
    {
        _outerWidth = outerWidth;
        _outerHeight = outerHeight;
        RecomputeLayoutInfo();
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
        UpdateCursorConfiguration();
    }

    /// <summary>Sets the layout and scrolling options.</summary>
    public void SetOptions(EditorLayoutOptions layoutOptions, ViewLayoutOptions viewLayoutOptions)
    {
        _layoutOptions = layoutOptions;
        _viewLayoutOptions = viewLayoutOptions;
        ViewLayout.OnOptionsChanged(viewLayoutOptions);
        RecomputeLayoutInfo();
    }

    /// <summary>Builds the data to render the current viewport.</summary>
    public ViewportData GetViewportData(IReadOnlyList<Selection> selections)
    {
        return new ViewportData(
            selections,
            ViewLayout.GetLinesViewportData(),
            ViewLayout.GetWhitespaceViewportData(),
            _model);
    }

    private void OnModelRawContentChanged(object? sender, ModelRawContentChangedEventArgs e)
    {
        foreach (ModelRawChange change in e.Changes)
        {
            switch (change)
            {
                case ModelRawFlush:
                    ViewLayout.OnFlushed(_model.LineCount, []);
                    break;
                case ModelRawLinesDeleted deleted:
                    ViewLayout.OnLinesDeleted(deleted.FromLineNumber, deleted.ToLineNumber);
                    break;
                case ModelRawLinesInserted inserted:
                    ViewLayout.OnLinesInserted(inserted.FromLineNumber, inserted.FromLineNumber + inserted.Count - 1);
                    break;
                default:
                    // ModelRawLineChanged, ModelRawEolChanged: nothing to do until there is a line mapping.
                    break;
            }
        }

        ViewLayout.OnHeightMaybeChanged();

        int digitCount = CountDigits(_model.LineCount);
        if (digitCount != _lineNumbersDigitCount)
        {
            _lineNumbersDigitCount = digitCount;
            RecomputeLayoutInfo();
        }

        // Monaco: emitContentChangeEvent runs after the layout has been updated (onDidChangeContentOrInjectedText).
        EmitViewEvent(eventsCollector => _cursor.OnModelContentChanged(eventsCollector, e));
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
                GlyphMarginDecorationLaneCount: 1));
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
