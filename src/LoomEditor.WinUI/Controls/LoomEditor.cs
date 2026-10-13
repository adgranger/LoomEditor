using LoomEditor.Rendering;
using LoomEditor.Rendering.Win2D;
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
    private const float FontSizeInDips = 14;

    /// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(LoomEditor), new PropertyMetadata(string.Empty, OnTextChanged));

    private readonly EditorContext _context = new();
    private readonly TextViewRenderer _renderer = new();
    private readonly KeyboardInput _keyboard;
    private EditorFeatureHost? _featureHost;
    private CanvasControl? _canvas;
    private Win2DFont? _font;

    /// <summary>Creates the control.</summary>
    public LoomEditor()
    {
        DefaultStyleKey = typeof(LoomEditor);
        _keyboard = new KeyboardInput(_context.Commands);
        _context.SelectionChanged += (_, _) => _canvas?.Invalidate();
        _context.TextChanged += (_, _) => _canvas?.Invalidate();
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
        _font?.Dispose();
        _font = null;
    }

    private void OnCanvasCreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
    {
        // Called again if the graphics device is lost (e.g. driver update): recreate device-dependent resources.
        _font?.Dispose();
        _font = new Win2DFont(sender, FontFamilyName, FontSizeInDips);
    }

    private void OnCanvasDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        // The font is released when the control is unloaded; recreate it if the control comes back.
        _font ??= new Win2DFont(sender, FontFamilyName, FontSizeInDips);

        var surface = new Win2DRenderSurface(args.DrawingSession, _font);
        _renderer.Render(surface, _context, (float)sender.ActualHeight);
    }

    private void DetachCanvas()
    {
        if (_canvas is null)
        {
            return;
        }

        _canvas.CreateResources -= OnCanvasCreateResources;
        _canvas.Draw -= OnCanvasDraw;
        _canvas = null;
    }
}
