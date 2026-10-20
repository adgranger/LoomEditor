// Ported from microsoft/vscode: src/vs/platform/theme/common/colorUtils.ts (ColorValue, ColorTransform)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Themes;

/// <summary>
/// The default of a color in the table of <see cref="EditorColorDefaults"/> (Monaco: <c>ColorValue</c>): a <c>#hex</c>
/// color, the id of another color, or a transform of other values.
/// </summary>
/// <remarks>
/// Only the transforms that the editor colors use are ported (<c>transparent</c>, <c>lessProminent</c>); <c>darken</c>,
/// <c>lighten</c>, <c>mix</c>, <c>opaque</c>, <c>oneOf</c> and <c>ifDefinedThenElse</c> are not. Threading: immutable.
/// </remarks>
internal abstract record ColorValue
{
    /// <summary>A <c>#hex</c> color or the id of another color (a string that starts with <c>#</c> is a color).</summary>
    public static implicit operator ColorValue(string value) => new Reference(value);

    /// <summary>Monaco: <c>transparent(value, factor)</c>.</summary>
    public static ColorValue Transparent(ColorValue value, double factor) => new TransparentValue(value, factor);

    /// <summary>Monaco: <c>lessProminent(value, background, factor, transparency)</c>.</summary>
    public static ColorValue LessProminent(ColorValue value, ColorValue background, double factor, double transparency) =>
        new LessProminentValue(value, background, factor, transparency);

    /// <summary>A <c>#hex</c> color or a color id.</summary>
    public sealed record Reference(string Value) : ColorValue;

    /// <summary>A color made transparent by a factor.</summary>
    public sealed record TransparentValue(ColorValue Value, double Factor) : ColorValue;

    /// <summary>A color made less prominent against a background.</summary>
    public sealed record LessProminentValue(ColorValue Value, ColorValue Background, double Factor, double Transparency) : ColorValue;
}
