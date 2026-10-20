// Ported from microsoft/vscode: src/vs/workbench/services/themes/common/colorThemeData.ts (TokenColorIndex)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Themes;

/// <summary>
/// Numbers the colors of the token rules, starting at 1 (0 means "no color"): the color map of the tokenizer
/// (VS Code: <c>TokenColorIndex</c>, without the <c>get</c> that logs a missing color).
/// </summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class TokenColorIndex
{
    private readonly Dictionary<string, int> _color2Id = new(StringComparer.Ordinal);
    private readonly List<ThemeColor> _id2Color = [default]; // index 0 is unused

    /// <summary><c>add</c>: the id of the color (a new id for a new color), or 0 for <see langword="null"/>.</summary>
    public int Add(ThemeColor? color)
    {
        if (color is not { } value)
        {
            return 0;
        }

        string key = ColorNormalization.Normalize(value);
        if (_color2Id.TryGetValue(key, out int id))
        {
            return id;
        }

        id = _id2Color.Count;
        _color2Id[key] = id;
        _id2Color.Add(value);
        return id;
    }

    /// <summary><c>asArray</c>: the colors by id; index 0 is unused (<c>default</c>).</summary>
    public IReadOnlyList<ThemeColor> AsArray() => [.. _id2Color];
}
