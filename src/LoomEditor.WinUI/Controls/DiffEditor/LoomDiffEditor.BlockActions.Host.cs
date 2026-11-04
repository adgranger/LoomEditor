using LoomEditor.WinUI.Controls.DiffEditor;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// The per-block action buttons in the glyph margin of the side-by-side view (phase 14). The editor knows nothing about what an action does: it raises an event.
public sealed partial class LoomDiffEditor
{
    /// <summary>Identifies the <see cref="BlockActionsVisibility"/> dependency property.</summary>
    public static readonly DependencyProperty BlockActionsVisibilityProperty = DependencyProperty.Register(
        nameof(BlockActionsVisibility), typeof(DiffBlockActionsVisibility), typeof(LoomDiffEditor),
        new PropertyMetadata(DiffBlockActionsVisibility.Off, (d, _) => ((LoomDiffEditor)d).UpdateGutterLanes()));

    /// <summary>Identifies the <see cref="PrimaryBlockActionGlyph"/> dependency property.</summary>
    public static readonly DependencyProperty PrimaryBlockActionGlyphProperty = RegisterGutterString(nameof(PrimaryBlockActionGlyph));

    /// <summary>Identifies the <see cref="PrimaryBlockActionToolTip"/> dependency property.</summary>
    public static readonly DependencyProperty PrimaryBlockActionToolTipProperty = RegisterGutterString(nameof(PrimaryBlockActionToolTip));

    /// <summary>Identifies the <see cref="SecondaryBlockActionGlyph"/> dependency property.</summary>
    public static readonly DependencyProperty SecondaryBlockActionGlyphProperty = RegisterGutterString(nameof(SecondaryBlockActionGlyph));

    /// <summary>Identifies the <see cref="SecondaryBlockActionToolTip"/> dependency property.</summary>
    public static readonly DependencyProperty SecondaryBlockActionToolTipProperty = RegisterGutterString(nameof(SecondaryBlockActionToolTip));

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
        get => (DiffBlockActionsVisibility)GetValue(BlockActionsVisibilityProperty);
        set => SetValue(BlockActionsVisibilityProperty, value);
    }

    /// <summary>The glyph (a character of the Segoe Fluent Icons / Segoe MDL2 Assets font) of the primary block action; null (the default) hides the action.</summary>
    public string? PrimaryBlockActionGlyph
    {
        get => (string?)GetValue(PrimaryBlockActionGlyphProperty);
        set => SetValue(PrimaryBlockActionGlyphProperty, value);
    }

    /// <summary>The tooltip of the primary block action, also the start of its automation name (<c>"{tooltip}, lines {s} to {e}"</c>). Defaults to null.</summary>
    public string? PrimaryBlockActionToolTip
    {
        get => (string?)GetValue(PrimaryBlockActionToolTipProperty);
        set => SetValue(PrimaryBlockActionToolTipProperty, value);
    }

    /// <summary>The glyph of the secondary block action, in a lane right of the primary one; null (the default) hides the action.</summary>
    public string? SecondaryBlockActionGlyph
    {
        get => (string?)GetValue(SecondaryBlockActionGlyphProperty);
        set => SetValue(SecondaryBlockActionGlyphProperty, value);
    }

    /// <summary>The tooltip of the secondary block action, also the start of its automation name. Defaults to null.</summary>
    public string? SecondaryBlockActionToolTip
    {
        get => (string?)GetValue(SecondaryBlockActionToolTipProperty);
        set => SetValue(SecondaryBlockActionToolTipProperty, value);
    }

    private static DependencyProperty RegisterGutterString(string name) =>
        DependencyProperty.Register(name, typeof(string), typeof(LoomDiffEditor), new PropertyMetadata(null, (d, _) => ((LoomDiffEditor)d).UpdateGutterLanes()));

    // The glyphs come from the system symbol font: nothing to configure.
    private static void ConfigureGutterFonts(DiffGutterControllers controllers)
    {
    }
}
