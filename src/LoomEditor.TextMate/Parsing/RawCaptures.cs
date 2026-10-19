// Ported from microsoft/vscode-textmate: src/rawGrammar.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Diagnostics.CodeAnalysis;

namespace LoomEditor.TextMate.Parsing;

/// <summary>
/// <c>IRawCaptures</c> (<c>IRawCapturesMap</c>): capture rules by capture id (<c>"0"</c>, <c>"1"</c>, …).
/// </summary>
/// <remarks>
/// <para>
/// Enumerated like a JavaScript object (<c>for (const k in captures)</c>): integer-like keys first, in increasing
/// numeric order, then the other keys in insertion order (phase-4/README.md, trap 2).
/// </para>
/// <para>Threading: read-only after loading (see <see cref="RawRule"/>).</para>
/// </remarks>
internal sealed class RawCaptures
{
    private readonly Dictionary<string, RawRule> _rules = new(StringComparer.Ordinal);
    private readonly List<string> _keys = [];

    /// <summary>The number of captures.</summary>
    public int Count => _keys.Count;

    /// <summary>The keys, in JavaScript object order.</summary>
    public IReadOnlyList<string> Keys => _keys;

    /// <summary>Gets or sets the capture rule of <paramref name="captureId"/>.</summary>
    public RawRule this[string captureId]
    {
        get => _rules[captureId];
        set
        {
            if (_rules.TryAdd(captureId, value))
            {
                _keys.Insert(InsertionIndex(captureId), captureId);
            }
            else
            {
                _rules[captureId] = value;
            }
        }
    }

    /// <summary>Gets the capture rule of <paramref name="captureId"/>, if any.</summary>
    public bool TryGetValue(string captureId, [MaybeNullWhen(false)] out RawRule rule) => _rules.TryGetValue(captureId, out rule);

    /// <summary>A deep copy (<c>clone</c>).</summary>
    public RawCaptures DeepClone()
    {
        var clone = new RawCaptures();
        foreach (string key in _keys)
        {
            clone._rules.Add(key, _rules[key].DeepClone());
            clone._keys.Add(key);
        }

        return clone;
    }

    /// <summary>
    /// Whether <paramref name="key"/> is a JavaScript array index: the canonical decimal form of an integer in
    /// [0, 2^32 - 2] (no sign, no leading zero).
    /// </summary>
    internal static bool IsArrayIndex(string key, out uint value)
    {
        value = 0;
        if (key.Length == 0 || key.Length > 10 || (key.Length > 1 && key[0] == '0'))
        {
            return false;
        }

        ulong number = 0;
        foreach (char c in key)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }

            number = (number * 10) + (ulong)(c - '0');
        }

        if (number > uint.MaxValue - 1)
        {
            return false;
        }

        value = (uint)number;
        return true;
    }

    private int InsertionIndex(string key)
    {
        if (!IsArrayIndex(key, out uint value))
        {
            return _keys.Count;
        }

        for (int i = 0; i < _keys.Count; i++)
        {
            if (!IsArrayIndex(_keys[i], out uint other) || other > value)
            {
                return i;
            }
        }

        return _keys.Count;
    }
}
