using LoomEditor.Rendering;
using LoomEditor.Rendering.Win2D;
using LoomEditor.Core.Model.Events;
using LoomEditor.View.Layout;
using LoomEditor.View.Features;
using LoomEditor.WinUI.Accessibility;
using LoomEditor.WinUI.Input;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace LoomEditor.WinUI.Controls;

/// <summary>
/// The LoomEditor code editor control.
/// </summary>
/// <example>
/// <code language="xml">
/// &lt;Window xmlns:loom="using:LoomEditor.WinUI.Controls"&gt;
///     &lt;loom:LoomEditor Text="Hello, world!" /&gt;
/// &lt;/Window&gt;
/// </code>
/// </example>
/// <remarks>
/// <para>
/// Phase 0: displays read-only text with line numbers; Ctrl+A selects everything.
/// The class is named like the root namespace on purpose (it is the product); in C# files inside a
/// <c>LoomEditor.*</c> namespace, refer to it through a <c>using</c> directive. See docs/adr/0003-naming-and-namespaces.md.
/// </para>
/// <para>Threading: like every WinUI control, use it on the UI thread only.</para>
/// </remarks>
[TemplatePart(Name = CanvasPartName, Type = typeof(CanvasControl))]
#pragma warning disable CA1724 // Type name matches the root namespace on purpose: see remarks above.
#pragma warning disable CA1001 // WinUI controls are not IDisposable: disposable fields are released in OnUnloaded.
public sealed partial class LoomEditor : Control
#pragma warning restore CA1001
#pragma warning restore CA1724
{
    private const string CanvasPartName = "PART_Canvas";
    private const string FontFamilyName = "Cascadia Mono, Consolas, Courier New";
    private const double FontSizeInDips = 14;
    private const int TabSize = 4;

    /// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(LoomEditor), new PropertyMetadata(string.Empty, OnTextChanged));

    private readonly EditorContext _context = new();
    private TextViewRenderer? _renderer;
    private ViewModel? _viewModel;
    private readonly KeyboardInput _keyboard;
    private EditorFeatureHost? _featureHost;
    private CanvasControl? _canvas;
    private Win2DTextMeasurer? _measurer;

    /// <summary>Creates the control.</summary>
    public LoomEditor()
    {
        DefaultStyleKey = typeof(LoomEditor);
        _keyboard = new KeyboardInput(_context.Commands);
        _context.SelectionChanged += (_, _) => _canvas?.Invalidate();
        _context.Model.ContentChanged += (_, _) => _canvas?.Invalidate();
        _context.Model.RawContentChanged += OnModelRawContentChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>The text displayed by the editor.</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// The features to enable. Defaults to <see cref="EditorFeatures.Default"/>.
    /// Set it before the control is loaded; changes made afterwards are ignored.
    /// </summary>
    public IReadOnlyList<Func<IEditorFeature>> Features { get; set; } = EditorFeatures.Default;

    /// <summary>The text model of this editor: edit it with <c>PushEditOperations</c>, undo and redo through it.</summary>
    /// <remarks>Threading: use it on the UI thread only, like the control.</remarks>
    public Core.Model.TextModel Model => _context.Model;

    /// <summary>The commands of this editor, e.g. to run <c>editor.action.selectAll</c> from a menu.</summary>
    public View.Commands.CommandRegistry Commands => _context.Commands;

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        DetachCanvas();
        _canvas = GetTemplateChild(CanvasPartName) as CanvasControl
            ?? throw new InvalidOperationException($"The LoomEditor template must contain a CanvasControl named '{CanvasPartName}'.");
        _canvas.CreateResources += OnCanvasCreateResources;
        _canvas.Draw += OnCanvasDraw;
        _canvas.SizeChanged += OnCanvasSizeChanged;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (_keyboard.TryHandle(e.Key))
        {
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus(FocusState.Pointer);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new LoomEditorAutomationPeer(this);

    private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var editor = (LoomEditor)sender;
        editor._context.SetText((string?)e.NewValue ?? string.Empty);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _featureHost ??= new EditorFeatureHost(_context, Features);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _featureHost?.Dispose();
        _featureHost = null;
        ReleaseResources();
    }

    private void OnCanvasCreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
    {
        // Called again if the graphics device is lost (e.g. driver update): recreate device-dependent resources.
        CreateResources(sender);
    }

    private void OnCanvasDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        // The resources are released when the control is unloaded; recreate them if the control comes back.
        if (_measurer is null || _renderer is null || _viewModel is null)
        {
            CreateResources(sender);
        }

        var surface = new Win2DRenderSurface(args.DrawingSession, _measurer!);
        _renderer!.Render(surface, _viewModel!, _context.Selection, FocusState != FocusState.Unfocused, cursorVisible: true);
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _viewModel?.SetViewportSize((int)e.NewSize.Width, (int)e.NewSize.Height);
    }

    private void OnModelRawContentChanged(object? sender, ModelRawContentChangedEventArgs e)
    {
        // Monaco: `onFlushed` resets the widest line.
        if (e.Changes.Any(change => change is ModelRawFlush))
        {
            _renderer?.ResetMaxLineWidth();
        }
    }

    private void OnViewLayoutInfoChanged(object? sender, EventArgs e)
    {
        // Monaco: a new layout info resets the widest line.
        _renderer?.ResetMaxLineWidth();
        _canvas?.Invalidate();
    }

    private void CreateResources(CanvasControl canvas)
    {
        _renderer?.Dispose();
        _measurer?.Dispose();
        // Pixel ratio 1: layout is in DIPs; DPI changes are handled in a later card (P2-20).
        _measurer = new Win2DTextMeasurer(canvas, FontFamilyName, FontSizeInDips, TabSize, pixelRatio: 1);
        _renderer = new TextViewRenderer(_measurer);

        if (_viewModel is null)
        {
            _viewModel = new ViewModel(
                _context.Model,
                _measurer.FontInfo,
                new EditorLayoutOptions(),
                new ViewLayoutOptions(_measurer.FontInfo.LineHeight),
                ScheduleAtNextAnimationFrame,
                TimeProvider.System);
            _viewModel.ViewLayout.Scroll += OnViewLayoutScroll;
            _viewModel.LayoutInfoChanged += OnViewLayoutInfoChanged;
            _viewModel.SetViewportSize((int)canvas.ActualWidth, (int)canvas.ActualHeight);
        }
        else
        {
            _viewModel.SetFontInfo(_measurer.FontInfo);
        }
    }

    private void ReleaseResources()
    {
        _viewModel?.Dispose();
        _viewModel = null;
        _renderer?.Dispose();
        _renderer = null;
        _measurer?.Dispose();
        _measurer = null;
    }

    private void OnViewLayoutScroll(object? sender, ScrollEvent e) => _canvas?.Invalidate();

    // Smooth scrolling is off until P2-18, which replaces this with a CompositionTarget.Rendering scheduler.
    private CancelableCallback ScheduleAtNextAnimationFrame(Action callback)
    {
        var scheduled = new CancelableCallback(callback);
        DispatcherQueue.TryEnqueue(scheduled.Run);
        return scheduled;
    }

    private void DetachCanvas()
    {
        if (_canvas is null)
        {
            return;
        }

        _canvas.CreateResources -= OnCanvasCreateResources;
        _canvas.Draw -= OnCanvasDraw;
        _canvas.SizeChanged -= OnCanvasSizeChanged;
        _canvas = null;
    }

    private sealed class CancelableCallback(Action callback) : IDisposable
    {
        private Action? _callback = callback;

        public void Run() => _callback?.Invoke();

        public void Dispose() => _callback = null;
    }
}
