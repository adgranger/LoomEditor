// Ported from microsoft/vscode: src/vs/editor/common/config/editorOptions.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;

namespace LoomEditor.View.Layout;

/// <summary>
/// The font options that go beyond family and size: weight, letter spacing, font features (ligatures) and font
/// variations. Monaco: <c>fontWeight</c>, <c>letterSpacing</c>, <c>EditorFontLigatures</c>, <c>EditorFontVariations</c>.
/// </summary>
/// <remarks>Threading: immutable, safe from any thread.</remarks>
/// <param name="FontWeight"><c>normal</c>, <c>bold</c> or a number from 1 to 1000, as a string.</param>
/// <param name="LetterSpacing">Extra spacing added after every character, in pixels.</param>
/// <param name="FontFeatureSettings">CSS <c>font-feature-settings</c> syntax; <see cref="LigaturesOff"/> by default.</param>
/// <param name="FontVariationSettings"><see cref="VariationsOff"/>, <see cref="VariationsTranslate"/> or CSS <c>font-variation-settings</c> syntax.</param>
/// <param name="LineHeight">Monaco <c>lineHeight</c>: 0 for the golden ratio, a multiplier of the font size when below 8, else a size in pixels.</param>
internal sealed record EditorFontOptions(
    string FontWeight = "normal",
    double LetterSpacing = 0,
    string FontFeatureSettings = EditorFontOptions.LigaturesOff,
    string FontVariationSettings = EditorFontOptions.VariationsOff,
    double LineHeight = 0)
{
    /// <summary>Monaco: <c>EditorFontLigatures.OFF</c>.</summary>
    public const string LigaturesOff = "\"liga\" off, \"calt\" off";

    /// <summary>Monaco: <c>EditorFontLigatures.ON</c>.</summary>
    public const string LigaturesOn = "\"liga\" on, \"calt\" on";

    /// <summary>Monaco: <c>EditorFontVariations.OFF</c> (<c>FONT_VARIATION_OFF</c>).</summary>
    public const string VariationsOff = "normal";

    /// <summary>Monaco: <c>EditorFontVariations.TRANSLATE</c> (<c>FONT_VARIATION_TRANSLATE</c>).</summary>
    public const string VariationsTranslate = "translate";

    private const int MinimumWeight = 1;
    private const int MaximumWeight = 1000;

    /// <summary>The default options: normal weight, no spacing, ligatures off, variations off.</summary>
    public static EditorFontOptions Default { get; } = new();

    /// <summary>Monaco: <c>EditorFontLigatures.validate</c>. The control converts its <c>bool</c> to <c>"true"</c> / <c>"false"</c>.</summary>
    public static string ValidateLigatures(string? input)
    {
        return input switch
        {
            null or "" or "false" => LigaturesOff,
            "true" => LigaturesOn,
            _ => input,
        };
    }

    /// <summary>Monaco: <c>EditorFontVariations.validate</c>.</summary>
    public static string ValidateVariations(string? input)
    {
        return input switch
        {
            null or "false" => VariationsOff,
            "true" => VariationsTranslate,
            _ => input,
        };
    }

    /// <summary>
    /// Monaco: <c>EditorFontWeight.validate</c>. <c>normal</c> and <c>bold</c> are kept; anything else is parsed as an
    /// integer clamped to 1-1000, and a non-number gives <c>normal</c>.
    /// </summary>
    public static string ValidateWeight(string? input)
    {
        if (input is "normal" or "bold")
        {
            return input;
        }

        if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int weight))
        {
            return "normal";
        }

        return Math.Clamp(weight, MinimumWeight, MaximumWeight).ToString(CultureInfo.InvariantCulture);
    }
}
