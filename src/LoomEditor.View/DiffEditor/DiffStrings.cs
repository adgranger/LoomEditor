using System.Globalization;

namespace LoomEditor.View.DiffEditor;

/// <summary>Every user-visible English string of the diff editor's gutter actions.</summary>
/// <remarks>Threading: pure and thread-safe.</remarks>
internal static class DiffStrings
{
    /// <summary>The name of a block action button whose tooltip is not set.</summary>
    public const string BlockActionFallback = "Block action";

    /// <summary>The automation name of a block action button: <c>{tooltip}, line {s}</c> or <c>{tooltip}, lines {s} to {e}</c>.</summary>
    public static string BlockAction(string tooltip, int start, int end) => start == end
        ? string.Create(CultureInfo.InvariantCulture, $"{tooltip}, line {start}")
        : string.Create(CultureInfo.InvariantCulture, $"{tooltip}, lines {start} to {end}");

    /// <summary>The name of the line action button whose tooltip is not set.</summary>
    public const string LineActionFallback = "Line action";

    /// <summary>The automation name of a line checkbox: <c>Select deleted line {n}</c> (original side) or <c>Select added line {n}</c> (modified side).</summary>
    public static string SelectLine(bool modified, int line) => modified
        ? string.Create(CultureInfo.InvariantCulture, $"Select added line {line}")
        : string.Create(CultureInfo.InvariantCulture, $"Select deleted line {line}");
}
