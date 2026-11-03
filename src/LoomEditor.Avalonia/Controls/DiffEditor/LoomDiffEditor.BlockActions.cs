using LoomEditor.View.DiffEditor;
using LoomEditor.Avalonia.Controls.DiffEditor;
using Avalonia;
using Avalonia.Media;

namespace LoomEditor.Avalonia.Controls;

// The per-block action buttons in the glyph margin of the side-by-side view (phase 14). The editor knows nothing about what an action does: it raises an event.
public sealed partial class LoomDiffEditor
{
    /// <summary>Identifies the <see cref="BlockActionsVisibility"/> styled property.</summary>
    public static readonly StyledProperty<DiffBlockActionsVisibility> BlockActionsVisibilityProperty =
        AvaloniaProperty.Register<LoomDiffEditor, DiffBlockActionsVisibility>(nameof(BlockActionsVisibility), DiffBlockActionsVisibility.Off);

    /// <summary>Identifies the <see cref="PrimaryBlockActionGlyph"/> styled property.</summary>
    public static readonly StyledProperty<string?> PrimaryBlockActionGlyphProperty = RegisterGutterString(nameof(PrimaryBlockActionGlyph));

    /// <summary>Identifies the <see cref="PrimaryBlockActionToolTip"/> styled property.</summary>
    public static readonly StyledProperty<string?> PrimaryBlockActionToolTipProperty = RegisterGutterString(nameof(PrimaryBlockActionToolTip));

    /// <summary>Identifies the <see cref="SecondaryBlockActionGlyph"/> styled property.</summary>
    public static readonly StyledProperty<string?> SecondaryBlockActionGlyphProperty = RegisterGutterString(nameof(SecondaryBlockActionGlyph));

    /// <summary>Identifies the <see cref="SecondaryBlockActionToolTip"/> styled property.</summary>
    public static readonly StyledProperty<string?> SecondaryBlockActionToolTipProperty = RegisterGutterString(nameof(SecondaryBlockActionToolTip));

    /// <summary>
    /// Raised when a block action button is clicked, on the UI thread, synchronously from the click. The host decides what the action does; the editor changes
    /// nothing.
    /// </summary>
    public event EventHandler<DiffBlockActionInvokedEventArgs>? BlockActionInvoked;

    /// <summary>
    /// When the block action buttons show in the side-by-side view: one button per action at the first line of each visible block, in the modified editor (the
    /// original editor for a pure deletion). Each action with a glyph gets its own glyph margin lane, one line height wide. No button in the inline view.
    /// Defaults to <see cref="DiffBlockActionsVisibility.Off"/>.
    /// </summary>
    public DiffBlockActionsVisibility BlockActionsVisibility
    {
        get => GetValue(BlockActionsVisibilityProperty);
        set => SetValue(BlockActionsVisibilityProperty, value);
    }

    /// <summary>The glyph (a character of the Segoe Fluent Icons / Segoe MDL2 Assets font) of the primary block action; null (the default) hides the action.</summary>
    public string? PrimaryBlockActionGlyph
    {
        get => GetValue(PrimaryBlockActionGlyphProperty);
        set => SetValue(PrimaryBlockActionGlyphProperty, value);
    }

    /// <summary>The tooltip of the primary block action, also the start of its automation name (<c>"{tooltip}, lines {s} to {e}"</c>). Defaults to null.</summary>
    public string? PrimaryBlockActionToolTip
    {
        get => GetValue(PrimaryBlockActionToolTipProperty);
        set => SetValue(PrimaryBlockActionToolTipProperty, value);
    }

    /// <summary>The glyph of the secondary block action, in a lane right of the primary one; null (the default) hides the action.</summary>
    public string? SecondaryBlockActionGlyph
    {
        get => GetValue(SecondaryBlockActionGlyphProperty);
        set => SetValue(SecondaryBlockActionGlyphProperty, value);
    }

    /// <summary>The tooltip of the secondary block action, also the start of its automation name. Defaults to null.</summary>
    public string? SecondaryBlockActionToolTip
    {
        get => GetValue(SecondaryBlockActionToolTipProperty);
        set => SetValue(SecondaryBlockActionToolTipProperty, value);
    }

    // The font of the glyph strings: Segoe Fluent Icons on Windows, the font of the host elsewhere.
    internal const string DefaultGlyphFontFamily = "Segoe Fluent Icons, Segoe MDL2 Assets";

    /// <summary>Identifies the <see cref="GlyphFontFamily"/> styled property.</summary>
    public static readonly StyledProperty<FontFamily> GlyphFontFamilyProperty =
        AvaloniaProperty.Register<LoomDiffEditor, FontFamily>(nameof(GlyphFontFamily), new FontFamily(DefaultGlyphFontFamily));

    /// <summary>
    /// The font that draws the glyph strings of the block actions (and of the line action). Avalonia-only: WinUI uses the system symbol font. Defaults to
    /// <c>"Segoe Fluent Icons, Segoe MDL2 Assets"</c>, which only exists on Windows: set a font of your own elsewhere.
    /// </summary>
    public FontFamily GlyphFontFamily
    {
        get => GetValue(GlyphFontFamilyProperty);
        set => SetValue(GlyphFontFamilyProperty, value);
    }

#pragma warning disable AVP1001 // Only called from the static field initializers above.
    private static StyledProperty<string?> RegisterGutterString(string name)
    {
        StyledProperty<string?> property = AvaloniaProperty.Register<LoomDiffEditor, string?>(name);
        property.Changed.AddClassHandler<LoomDiffEditor>(static (editor, _) => editor.UpdateGutterLanes());
        return property;
    }
#pragma warning restore AVP1001

    // Called when the template gives the two editors: each creates its gutter controls on its gutter layer when it has one.
    private void AttachBlockActions()
    {
        LoomEditor original = _original!;
        LoomEditor modified = _modified!;
        original.SetDiffGutterFactory((layer, hitTest) => CreateDiffGutter(original, layer, false, hitTest));
        modified.SetDiffGutterFactory((layer, hitTest) => CreateDiffGutter(modified, layer, true, hitTest));
    }

    private void DetachBlockActions()
    {
        _original?.SetDiffGutterFactory(null);
        _modified?.SetDiffGutterFactory(null);
    }

    private DiffGutterControllers CreateDiffGutter(LoomEditor editor, Overlay.OverlayHost layer, bool modifiedSide, Func<double, double, View.Controller.MouseTarget> hitTest)
    {
        var blockActions = new DiffBlockActionsController(editor, layer, modifiedSide, hitTest);
        blockActions.Invoked += (block, action) => BlockActionInvoked?.Invoke(this, new DiffBlockActionInvokedEventArgs(block, action));
        blockActions.SetBlocks(_changes);
        var lineCheckBoxes = CreateLineCheckBoxes(editor, layer, modifiedSide, hitTest);
        var controllers = new DiffGutterControllers(blockActions, lineCheckBoxes);
        ConfigureGutter(controllers, GetGutterLanes());
        return controllers;
    }

    // Decision 1: the same lane count on both editors, so the two gutters keep the same width. Called for every phase 14 property and every layout change.
    private void UpdateGutterLanes()
    {
        if (_original is null || _modified is null)
        {
            return;
        }

        DiffGutterLanes lanes = GetGutterLanes();
        foreach (LoomEditor editor in new[] { _original, _modified })
        {
            editor.GlyphMarginLaneCount = lanes.Count;
            if (editor.DiffGutter is { } controllers)
            {
                ConfigureGutter(controllers, lanes);
            }
        }
    }

    private DiffGutterLanes GetGutterLanes() => DiffGutterLanes.Compute(
        _options.RenderSideBySideEffective,
        LineSelectionEnabled,
        LineActionGlyph is not null,
        BlockActionsVisibility != DiffBlockActionsVisibility.Off,
        PrimaryBlockActionGlyph is not null,
        SecondaryBlockActionGlyph is not null);

    private void ConfigureGutter(DiffGutterControllers controllers, DiffGutterLanes lanes)
    {
        controllers.BlockActions.SetGlyphFont(GlyphFontFamily);
        controllers.BlockActions.Configure(
            BlockActionsVisibility,
            PrimaryBlockActionGlyph,
            PrimaryBlockActionToolTip,
            SecondaryBlockActionGlyph,
            SecondaryBlockActionToolTip,
            lanes.PrimaryBlockAction,
            lanes.SecondaryBlockAction);
        controllers.LineCheckBoxes.SetGlyphFont(GlyphFontFamily);
        controllers.LineCheckBoxes.Configure(lanes.Select, lanes.LineAction, LineActionGlyph, LineActionToolTip);
    }

    private void UpdateBlockActionBlocks(IReadOnlyList<DiffChangeBlock> blocks)
    {
        _original?.DiffGutter?.BlockActions.SetBlocks(blocks);
        _modified?.DiffGutter?.BlockActions.SetBlocks(blocks);
    }
}
