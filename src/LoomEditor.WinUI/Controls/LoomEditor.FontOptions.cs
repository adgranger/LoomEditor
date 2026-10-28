using System.Globalization;
using LoomEditor.View.Layout;
using Microsoft.UI.Xaml;

namespace LoomEditor.WinUI.Controls;

// Font weight, letter spacing, ligatures and variations. Monaco: the `fontWeight`, `letterSpacing`, `fontLigatures` and
// `fontVariations` editor options. Every change recreates the text measurer.
public sealed partial class LoomEditor
{
    /// <summary>Identifies the <see cref="FontLigatures"/> dependency property.</summary>
    public static readonly DependencyProperty FontLigaturesProperty = DependencyProperty.Register(
        nameof(FontLigatures), typeof(bool), typeof(LoomEditor), new PropertyMetadata(false, OnFontOptionChanged));

    /// <summary>Identifies the <see cref="FontFeatureSettings"/> dependency property.</summary>
    public static readonly DependencyProperty FontFeatureSettingsProperty = DependencyProperty.Register(
        nameof(FontFeatureSettings), typeof(string), typeof(LoomEditor), new PropertyMetadata(string.Empty, OnFontOptionChanged));

    /// <summary>Identifies the <see cref="FontVariationSettings"/> dependency property.</summary>
    public static readonly DependencyProperty FontVariationSettingsProperty = DependencyProperty.Register(
        nameof(FontVariationSettings), typeof(string), typeof(LoomEditor), new PropertyMetadata(string.Empty, OnFontOptionChanged));

    /// <summary>Identifies the <see cref="LetterSpacing"/> dependency property.</summary>
    public static readonly DependencyProperty LetterSpacingProperty = DependencyProperty.Register(
        nameof(LetterSpacing), typeof(double), typeof(LoomEditor), new PropertyMetadata(0.0, OnFontOptionChanged));

    /// <summary>
    /// Turns the font ligatures (<c>liga</c> and <c>calt</c>) on. Ligature support depends on the font: Cascadia Code,
    /// Fira Code and JetBrains Mono have them, Consolas does not. Ignored when <see cref="FontFeatureSettings"/> is set.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool FontLigatures
    {
        get => (bool)GetValue(FontLigaturesProperty);
        set => SetValue(FontLigaturesProperty, value);
    }

    /// <summary>
    /// CSS <c>font-feature-settings</c> syntax (e.g. <c>"ss01" on, "calt" on</c>). When not empty it replaces
    /// <see cref="FontLigatures"/>. Which features exist depends on the font. Defaults to the empty string.
    /// </summary>
    public string FontFeatureSettings
    {
        get => (string)GetValue(FontFeatureSettingsProperty);
        set => SetValue(FontFeatureSettingsProperty, value);
    }

    /// <summary>
    /// CSS <c>font-variation-settings</c> syntax, <c>true</c> or <c>false</c>. The empty string means off. Only the
    /// <c>wght</c> axis is applied. Defaults to the empty string.
    /// </summary>
    public string FontVariationSettings
    {
        get => (string)GetValue(FontVariationSettingsProperty);
        set => SetValue(FontVariationSettingsProperty, value);
    }

    /// <summary>Extra spacing added after every character, in device-independent pixels. Defaults to 0.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is <c>NaN</c> or infinite; the previous value is kept.</exception>
    public double LetterSpacing
    {
        get => (double)GetValue(LetterSpacingProperty);
        set => SetValue(LetterSpacingProperty, value);
    }

    private static void OnFontOptionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var editor = (LoomEditor)sender;
        if (ReferenceEquals(e.Property, LetterSpacingProperty) && !double.IsFinite((double)e.NewValue))
        {
            editor.SetValue(LetterSpacingProperty, e.OldValue);
            throw new ArgumentOutOfRangeException(nameof(e), e.NewValue, "The letter spacing must be a finite number.");
        }

        editor.RecreateTextMeasurer();
    }

    private EditorFontOptions CurrentFontOptions
    {
        get
        {
            ushort weight = FontWeight.Weight;
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
                EditorFontOptions.ValidateVariations(variations));
        }
    }
}
