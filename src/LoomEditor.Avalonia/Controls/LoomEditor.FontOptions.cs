using System.Globalization;
using Avalonia;
using Avalonia.Controls.Primitives;
using LoomEditor.View.Layout;

namespace LoomEditor.Avalonia.Controls;

// Font weight, letter spacing, ligatures and variations. Monaco: the `fontWeight`, `letterSpacing`, `fontLigatures` and
// `fontVariations` editor options. Every change recreates the text measurer.
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="FontLigatures"/> styled property.</summary>
    public static readonly StyledProperty<bool> FontLigaturesProperty =
        AvaloniaProperty.Register<LoomEditor, bool>(nameof(FontLigatures), false);

    /// <summary>Identifies the <see cref="FontFeatureSettings"/> styled property.</summary>
    public static readonly StyledProperty<string> FontFeatureSettingsProperty =
        AvaloniaProperty.Register<LoomEditor, string>(nameof(FontFeatureSettings), string.Empty);

    /// <summary>Identifies the <see cref="FontVariationSettings"/> styled property.</summary>
    public static readonly StyledProperty<string> FontVariationSettingsProperty =
        AvaloniaProperty.Register<LoomEditor, string>(nameof(FontVariationSettings), string.Empty);

    /// <summary>Identifies the <see cref="LetterSpacing"/> styled property.</summary>
    public new static readonly StyledProperty<double> LetterSpacingProperty =
        TemplatedControl.LetterSpacingProperty.AddOwner<LoomEditor>();

    /// <summary>
    /// Turns the font ligatures (<c>liga</c> and <c>calt</c>) on. Ligature support depends on the font: Cascadia Code,
    /// Fira Code and JetBrains Mono have them, Consolas does not. Ignored when <see cref="FontFeatureSettings"/> is set.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool FontLigatures
    {
        get => GetValue(FontLigaturesProperty);
        set => SetValue(FontLigaturesProperty, value);
    }

    /// <summary>
    /// CSS <c>font-feature-settings</c> syntax (e.g. <c>"ss01" on, "calt" on</c>). When not empty it replaces
    /// <see cref="FontLigatures"/>. Which features exist depends on the font. Defaults to the empty string.
    /// </summary>
    public string FontFeatureSettings
    {
        get => GetValue(FontFeatureSettingsProperty);
        set => SetValue(FontFeatureSettingsProperty, value);
    }

    /// <summary>
    /// CSS <c>font-variation-settings</c> syntax, <c>true</c> or <c>false</c>. The empty string means off. Only the
    /// <c>wght</c> axis is applied. Defaults to the empty string.
    /// </summary>
    public string FontVariationSettings
    {
        get => GetValue(FontVariationSettingsProperty);
        set => SetValue(FontVariationSettingsProperty, value);
    }

    /// <summary>Extra spacing added after every character, in device-independent pixels. Defaults to 0.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is <c>NaN</c> or infinite; the previous value is kept.</exception>
    public new double LetterSpacing
    {
        get => GetValue(LetterSpacingProperty);
        set => SetValue(LetterSpacingProperty, value);
    }

    private double _lineHeight;

    /// <summary>
    /// The height of a line, in DIPs (Monaco: <c>lineHeight</c>). Set it to 0 for the automatic height (1.35 times the font size), to a value below 8 for a
    /// multiplier of the font size, or to a height in DIPs (at least 8). Reading it gives the height in use, rounded to a whole pixel and scaled by the zoom,
    /// not the value that was set: read it again after the layout changed. Defaults to 0.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative, <c>NaN</c> or infinite.</exception>
    /// <remarks>Threading: UI thread only.</remarks>
    public double LineHeight
    {
        get => _viewModel.FontInfo.LineHeight;
        set
        {
            if (!double.IsFinite(value) || value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The line height must be a finite number, 0 or greater.");
            }

            if (value != _lineHeight)
            {
                _lineHeight = value;
                RecreateTextMeasurer();
            }
        }
    }

    private static void InitializeFontOptionProperties()
    {
        FontLigaturesProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnFontOptionChanged(e));
        FontFeatureSettingsProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnFontOptionChanged(e));
        FontVariationSettingsProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnFontOptionChanged(e));
        LetterSpacingProperty.Changed.AddClassHandler<LoomEditor>(static (editor, e) => editor.OnFontOptionChanged(e));
    }

    private void OnFontOptionChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == LetterSpacingProperty && !double.IsFinite((double)e.NewValue!))
        {
            SetCurrentValue(LetterSpacingProperty, (double)e.OldValue!);
            throw new ArgumentOutOfRangeException(nameof(e), e.NewValue, "The letter spacing must be a finite number.");
        }

        RecreateTextMeasurer();
    }

    private EditorFontOptions CurrentFontOptions
    {
        get
        {
            int weight = (int)FontWeight;
            string weightText = weight switch
            {
                400 => "normal",
                700 => "bold",
                _ => weight.ToString(CultureInfo.InvariantCulture),
            };
            string features = FontFeatureSettings.Length > 0 ? FontFeatureSettings : FontLigatures ? "true" : "false";
            string variations = FontVariationSettings.Length > 0 ? FontVariationSettings : "false";
            return new EditorFontOptions(
                EditorFontOptions.ValidateWeight(weightText),
                LetterSpacing,
                EditorFontOptions.ValidateLigatures(features),
                EditorFontOptions.ValidateVariations(variations),
                _lineHeight);
        }
    }
}
