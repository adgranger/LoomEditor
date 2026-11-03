using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using LoomEditor.Avalonia.Input;
using LoomEditor.Avalonia.Rendering;
using LoomEditor.Avalonia.Threading;
using LoomEditor.Rendering;
using LoomEditor.View.Controller;
using LoomEditor.View.Features;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls;

/// <summary>
/// The LoomEditor code editor control.
/// </summary>
/// <example>
/// <code language="xml">
/// &lt;Window xmlns:loom="using:LoomEditor.Avalonia.Controls"&gt;
///     &lt;loom:LoomEditor Text="Hello, world!" /&gt;
/// &lt;/Window&gt;
/// </code>
/// </example>
/// <remarks>
/// <para>
/// The class is named like the root namespace on purpose (it is the product); in C# files inside a
/// <c>LoomEditor.*</c> namespace, refer to it through a <c>using</c> directive. See docs/adr/0003-naming-and-namespaces.md.
/// </para>
/// <para>Threading: like every Avalonia control, use it on the UI thread only.</para>
/// </remarks>
[TemplatePart(CanvasPartName, typeof(EditorCanvas))]
[TemplatePart(VerticalScrollBarPartName, typeof(ScrollBar))]
[TemplatePart(HorizontalScrollBarPartName, typeof(ScrollBar))]
[TemplatePart(OverlayCanvasPartName, typeof(Canvas))]
[TemplatePart(ZoneWidgetCanvasPartName, typeof(Canvas))]
[TemplatePart(LiveRegionPartName, typeof(TextBlock))]
[TemplatePart(FindWidgetPartName, typeof(Find.FindWidget))]
#pragma warning disable CA1724 // Type name matches the root namespace on purpose: see remarks above.
#pragma warning disable CA1001 // Avalonia controls are not IDisposable: disposable fields are released when the control leaves the tree.
public sealed partial class LoomEditor : TemplatedControl
#pragma warning restore CA1001
#pragma warning restore CA1724
{
    private const string CanvasPartName = "PART_Canvas";
    private const string OverlayCanvasPartName = "PART_OverlayCanvas";
    private const string ZoneWidgetCanvasPartName = "PART_ZoneWidgetCanvas";

    /// <summary>Identifies the <see cref="Text"/> styled property.</summary>
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<LoomEditor, string>(nameof(Text), string.Empty);

    /// <summary>Identifies the <see cref="SmoothScrolling"/> styled property.</summary>
    public static readonly StyledProperty<bool> SmoothScrollingProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(SmoothScrolling), false);

    private readonly EditorContext _context;
    private readonly AnimationFrameScheduler _frameScheduler;
    private TextViewRenderer? _renderer;
    private readonly ViewModel _viewModel;
    private readonly KeyboardInput _keyboard;
    private readonly IDisposable _clipboardCommands;
    private EditorFeatureHost? _featureHost;
    private EditorCanvas? _canvas;
    private AvaloniaTextMeasurer? _measurer;
    private readonly AvaloniaBitmapCache _bitmapCache = new();
    private readonly AvaloniaPaintCache _paintCache = new();
    private TopLevel? _observedTopLevel;
    private bool _isDrawing;

    static LoomEditor()
    {
        FontFamilyProperty.OverrideDefaultValue<LoomEditor>(new FontFamily(BareFontInfo.DefaultFontFamilyForCurrentOS));
        TextProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnTextChanged((string?)e.NewValue ?? string.Empty));
        SmoothScrollingProperty.Changed.AddClassHandler<LoomEditor>(static (editor, _) => editor.OnSmoothScrollingChanged());
        InitializeFontProperties();
        InitializeFontOptionProperties();
        InitializeZoomProperties();
        InitializeReadOnlyProperties();
        InitializeHighlightingProperties();
        InitializeCaretProperties();
        InitializePointerProperties();
        InitializeMarginProperties();
        InitializeLineNumbersProperties();
        InitializeMinimapProperties();
        InitializeWhitespaceProperties();
        InitializeWordWrapProperties();
        InitializeGuidesProperties();
        InitializeStickyScrollProperties();
        InitializeAccessibilityProperties();
        InitializeHighContrastProperties();
        InitializeCommentThreadsProperties();
    }

    /// <summary>Creates the control.</summary>
    public LoomEditor()
    {
        _frameScheduler = new AnimationFrameScheduler(() => TopLevel.GetTopLevel(this));
        _context = new EditorContext(ScheduleAtNextAnimationFrame, TimeProvider.System, new DispatcherUiDispatcher(), new DispatcherEditorScheduler());
        _viewModel = _context.ViewModel;
        _hostZoneLayer = new ViewZones.HostViewZoneLayer(this);
        _viewModel.ViewLayout.Scroll += OnViewLayoutScroll;
        _viewModel.ViewLayout.ContentSizeChanged += OnViewLayoutContentSizeChanged;
        _viewModel.LayoutInfoChanged += OnViewLayoutInfoChanged;
        _viewModel.ViewZones.ZonesChanged += (_, _) => Invalidate();
        _keyboard = new KeyboardInput(_context.Commands);
        _clipboardCommands = ClipboardCommands.Register(_context.Commands, _viewModel, CreateClipboardService());
        LostFocus += (_, _) => _keyboard.LeaveChordMode();
        _context.SelectionChanged += (_, _) => Invalidate();
        InitializeCaret();
        InitializeReveal();
        InitializePointer();
        InitializeFont();
        InitializeStickyScroll();
        InitializeHighlighting();
        InitializeIme();
        InitializeAccessibility();
        InitializeFind();
        InitializeCommentThreads();
        _context.Model.ContentChanged += (_, _) => Invalidate();
    }

    // The view model, for the tests (the layout of the lines is needed to aim a click).
    internal ViewModel ViewModel => _viewModel;

    /// <summary>The text displayed by the editor.</summary>
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Whether a physical mouse wheel scrolls with an animation (Monaco: <c>smoothScrolling</c>). Defaults to <c>false</c>.</summary>
    public bool SmoothScrolling
    {
        get => GetValue(SmoothScrollingProperty);
        set => SetValue(SmoothScrollingProperty, value);
    }

    /// <summary>
    /// The features to enable. Defaults to <see cref="EditorFeatures.Default"/>.
    /// Set it before the control is attached to a window; changes made afterwards are ignored.
    /// </summary>
    public IReadOnlyList<Func<IEditorFeature>> Features { get; set; } = EditorFeatures.Default;

    /// <summary>The text model of this editor: edit it with <c>PushEditOperations</c>, undo and redo through it.</summary>
    /// <remarks>Threading: use it on the UI thread only, like the control.</remarks>
    public Core.Model.TextModel Model => _context.Model;

    /// <summary>The selections of the cursors; the first one is the primary selection.</summary>
    public IReadOnlyList<Core.Text.Selection> Selections => _context.Selections;

    /// <summary>Replaces the selections, i.e. the cursors. There must be at least one.</summary>
    /// <param name="selections">The new selections.</param>
    public void SetSelections(IReadOnlyList<Core.Text.Selection> selections) => _context.SetSelections(selections);

    /// <summary>Raised after the selections changed.</summary>
    public event EventHandler? SelectionChanged
    {
        add => _context.SelectionChanged += value;
        remove => _context.SelectionChanged -= value;
    }

    /// <summary>The commands of this editor, e.g. to run <c>editor.action.selectAll</c> from a menu.</summary>
    public View.Commands.CommandRegistry Commands => _context.Commands;

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        DetachCanvas();
        _canvas = e.NameScope.Find<EditorCanvas>(CanvasPartName)
            ?? throw new InvalidOperationException($"The LoomEditor template must contain an EditorCanvas named '{CanvasPartName}'.");
        _canvas.DrawFrame = OnCanvasDraw;
        _canvas.PropertyChanged += OnCanvasPropertyChanged;
        AttachScrollBars(e.NameScope);
        AttachLiveRegion(e.NameScope);
        AttachOverlays(e.NameScope);
        AttachFindWidget(e.NameScope);
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _featureHost ??= new EditorFeatureHost(_context, Features);
        ConnectFindWidget();
        ApplyFoldingEnabled();
        StartHighlighting();
        StartAccessibility();
        StartHighContrast();
        ObserveTopLevel();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        _findWidget?.Disconnect();
        _featureHost?.Dispose();
        _featureHost = null;
        StopHighlighting();
        StopAccessibility();
        StopHighContrast();
        StopBlinkTimer();
        StopObservingTopLevel();
        ReleaseResources();
    }

    private void OnTextChanged(string text)
    {
        _context.SetText(text);
        _viewModel.SetIsDominatedByLongLines(_context.Model.IsDominatedByLongLines());
    }

    private void OnSmoothScrollingChanged() =>
        _viewModel.SetOptions(_viewModel.LayoutOptions, CreateViewLayoutOptions(_viewModel.FontInfo.LineHeight));

    private ViewLayoutOptions CreateViewLayoutOptions(int lineHeight) => new(lineHeight, SmoothScrolling: SmoothScrolling);

    private void OnCanvasDraw(DrawingContext context, Size size)
    {
        _isDrawing = true;
        try
        {
            // The resources are released when the control leaves the tree; recreate them if it comes back.
            if (_measurer is null || _renderer is null)
            {
                CreateResources();
            }

            var surface = new AvaloniaRenderSurface(context, new Rect(size), _measurer!, _bitmapCache, _paintCache);
            _renderer!.Render(surface, _viewModel, _viewModel.GetViewSelections(), HasFocus, _cursorBlinking.IsVisible);
            _hostZoneLayer.ArrangeAfterDraw();
            _commentGutter?.ArrangeAfterDraw();
            ApplyPendingHorizontalReveal();
        }
        finally
        {
            _isDrawing = false;
        }
    }

    // Avalonia refuses to invalidate a visual during its render pass: what changes while drawing is drawn at the next turn.
    private void Invalidate()
    {
        if (_isDrawing)
        {
            Dispatcher.UIThread.Post(() => _canvas?.InvalidateVisual());
        }
        else
        {
            _canvas?.InvalidateVisual();
        }
    }

    private void OnCanvasPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty)
        {
            Size size = ((Rect)e.NewValue!).Size;
            _mouseDown?.OnHeightChanged();
            _viewModel.SetViewportSize((int)size.Width, (int)size.Height);
            UpdateScrollBars();
        }
    }

    private void OnViewLayoutScroll(object? sender, ScrollEvent e)
    {
        _reveal?.OnScrollChanged(e);
        UpdateScrollBars();
        Invalidate();
        RaiseScrollChanged(e);
    }

    private void OnViewLayoutInfoChanged(object? sender, EventArgs e)
    {
        // Monaco: a new layout info resets the widest line.
        _renderer?.ResetMaxLineWidth();
        UpdateScrollBars();
        Invalidate();
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CreateResources()
    {
        ObserveTopLevel();

        _renderer?.Dispose();
        _measurer?.Dispose();
        _measurer = CreateTextMeasurer();
        _renderer = new TextViewRenderer(_measurer, _context.Scheduler, () => Invalidate());
        _renderer.MinimapSampleDataProvider = AvaloniaMinimapFontSampler.CreateSampleData;
        _renderer.Theme = EffectiveTheme;
        _renderer.RenderWhitespace = RenderWhitespace;
        _renderer.RenderControlCharacters = RenderControlCharacters;
        SyncCaretStyle();

        _viewModel.SetFontInfo(_measurer.FontInfo);
        if (_canvas is not null)
        {
            _viewModel.SetViewportSize((int)_canvas.Bounds.Width, (int)_canvas.Bounds.Height);
        }

        ApplyPendingScrollPosition();
        UpdateScrollBars();
    }

    private void ReleaseResources()
    {
        // The view model belongs to the context and outlives the resources: only the device-dependent ones go.
        _renderer?.Dispose();
        _renderer = null;
        _measurer?.Dispose();
        _measurer = null;
    }

    private AvaloniaClipboardService CreateClipboardService() => new(() => TopLevel.GetTopLevel(this)?.Clipboard);

    private IDisposable ScheduleAtNextAnimationFrame(Action callback) => _frameScheduler.Schedule(callback);

    private void DetachCanvas()
    {
        if (_canvas is null)
        {
            return;
        }

        _canvas.DrawFrame = null;
        DetachScrollBars();
        _canvas.PropertyChanged -= OnCanvasPropertyChanged;
        _canvas = null;
    }
}
