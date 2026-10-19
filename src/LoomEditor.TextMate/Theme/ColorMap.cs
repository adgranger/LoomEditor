// Ported from microsoft/vscode-textmate: src/theme.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.TextMate.Theme;

/// <summary><c>ColorMap</c> of vscode-textmate: gives each color an id.</summary>
/// <remarks>
/// <para>
/// Colors are upper-cased and get ids from 1 (id 0 means "no color"; slot 0 of <see cref="GetColorMap"/> is
/// <see langword="null"/>). A color map created from a list is frozen: as upstream, it stores the given colors as they
/// are, looks up the upper-cased color, and treats id 0 as missing.
/// </para>
/// <para>Threading: single-threaded while it grows; a frozen map is read-only.</para>
/// </remarks>
internal sealed class ColorMap
{
    private readonly bool _isFrozen;
    private readonly List<string?> _id2color = [];
    private readonly Dictionary<string, int> _color2id = new(StringComparer.Ordinal);
    private int _lastColorId;

    /// <summary>Creates a growing color map, or a frozen one from <paramref name="colorMap"/>.</summary>
    public ColorMap(IReadOnlyList<string>? colorMap = null)
    {
        if (colorMap is not null)
        {
            _isFrozen = true;
            for (int i = 0; i < colorMap.Count; i++)
            {
                _color2id[colorMap[i]] = i;
                _id2color.Add(colorMap[i]);
            }
        }
    }

    /// <summary><c>getId</c>: the id of <paramref name="color"/>; 0 for <see langword="null"/>.</summary>
    /// <exception cref="InvalidOperationException">The map is frozen and does not contain the color.</exception>
    public int GetId(string? color)
    {
        if (color is null)
        {
            return 0;
        }

        color = color.ToUpperInvariant();
        if (_color2id.TryGetValue(color, out int value) && value != 0)
        {
            return value;
        }

        if (_isFrozen)
        {
            throw new InvalidOperationException($"Missing color in color map - {color}");
        }

        value = ++_lastColorId;
        _color2id[color] = value;
        while (_id2color.Count <= value)
        {
            _id2color.Add(null);
        }

        _id2color[value] = color;
        return value;
    }

    /// <summary><c>getColorMap</c>: a copy of the colors by id.</summary>
    public string?[] GetColorMap() => [.. _id2color];
}
