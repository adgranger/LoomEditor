// Ported from microsoft/vscode: src/vs/editor/common/core/characterClassifier.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Text;

/// <summary>A fast character classifier that uses a compact array for ASCII values (Monaco: <c>CharacterClassifier</c>).</summary>
/// <typeparam name="T">The classification value; its default value must be the "not classified" value of the caller.</typeparam>
/// <remarks>Threading: mutable through <see cref="Set"/>; not thread-safe while being modified, safe to share once it is no longer modified.</remarks>
internal class CharacterClassifier<T>
    where T : struct
{
    private readonly T[] _asciiMap;
    private readonly Dictionary<int, T> _map = [];
    private readonly T _defaultValue;

    public CharacterClassifier(T defaultValue)
    {
        _defaultValue = defaultValue;
        _asciiMap = new T[256];
        Array.Fill(_asciiMap, defaultValue);
    }

    /// <summary>Sets the classification of a UTF-16 code unit.</summary>
    public void Set(int charCode, T value)
    {
        if (charCode >= 0 && charCode < 256)
        {
            _asciiMap[charCode] = value;
        }
        else
        {
            _map[charCode] = value;
        }
    }

    /// <summary>Gets the classification of a UTF-16 code unit; a negative code (Monaco: <c>NaN</c>) returns the default value.</summary>
    public virtual T Get(int charCode)
    {
        if (charCode >= 0 && charCode < 256)
        {
            return _asciiMap[charCode];
        }

        // Monaco: `this._map.get(charCode) || this._defaultValue`: a stored value of 0 reads as the default value.
        if (_map.TryGetValue(charCode, out var value) && !EqualityComparer<T>.Default.Equals(value, default))
        {
            return value;
        }

        return _defaultValue;
    }

    /// <summary>Resets every character to the default value.</summary>
    public void Clear()
    {
        Array.Fill(_asciiMap, _defaultValue);
        _map.Clear();
    }
}
