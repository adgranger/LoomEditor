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
}
