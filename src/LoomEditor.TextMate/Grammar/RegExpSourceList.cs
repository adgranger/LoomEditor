// Ported from microsoft/vscode-textmate: src/rule.ts
// Upstream commit: 25b68dad91920b1ed79d357534b8c4582f62d80d  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.TextMate.Oniguruma;

namespace LoomEditor.TextMate.Grammar;

/// <summary>
/// <c>RegExpSourceList</c>: the regexes of a rule, compiled on demand into one <see cref="CompiledRule"/> per anchor
/// variant (<c>IRegExpSourceListAnchorCache</c>).
/// </summary>
/// <remarks>Threading: single-threaded; it owns the compiled rules and disposes them.</remarks>
internal sealed class RegExpSourceList : IDisposable
{
    private readonly List<RegExpSource> _items = [];
    private bool _hasAnchors;
    private CompiledRule? _cached;
    private CompiledRule? _a0G0;
    private CompiledRule? _a0G1;
    private CompiledRule? _a1G0;
    private CompiledRule? _a1G1;

    /// <summary><c>length</c>.</summary>
    public int Length => _items.Count;

    /// <summary><c>dispose</c>.</summary>
    public void Dispose() => DisposeCaches();

    /// <summary><c>push</c>.</summary>
    public void Push(RegExpSource item)
    {
        _items.Add(item);
        _hasAnchors = _hasAnchors || item.HasAnchor;
    }

    /// <summary><c>unshift</c>.</summary>
    public void Unshift(RegExpSource item)
    {
        _items.Insert(0, item);
        _hasAnchors = _hasAnchors || item.HasAnchor;
    }

    /// <summary><c>setSource</c>: changes the source of one item and busts the caches.</summary>
    public void SetSource(int index, string newSource)
    {
        if (_items[index].Source != newSource)
        {
            // bust the cache
            DisposeCaches();
            _items[index].SetSource(newSource);
        }
    }

    /// <summary><c>compile</c>: the sources as they are.</summary>
    public CompiledRule Compile(IOnigLib onigLib)
    {
        _cached ??= new CompiledRule(onigLib, [.. _items.Select(e => e.Source)], [.. _items.Select(e => e.RuleId)]);
        return _cached;
    }

    /// <summary><c>compileAG</c>: the variant where <c>\A</c> and <c>\G</c> are allowed or not.</summary>
    public CompiledRule CompileAG(IOnigLib onigLib, bool allowA, bool allowG)
    {
        if (!_hasAnchors)
        {
            return Compile(onigLib);
        }

        if (allowA)
        {
            if (allowG)
            {
                return _a1G1 ??= ResolveAnchors(onigLib, allowA, allowG);
            }

            return _a1G0 ??= ResolveAnchors(onigLib, allowA, allowG);
        }

        if (allowG)
        {
            return _a0G1 ??= ResolveAnchors(onigLib, allowA, allowG);
        }

        return _a0G0 ??= ResolveAnchors(onigLib, allowA, allowG);
    }

    // _resolveAnchors
    private CompiledRule ResolveAnchors(IOnigLib onigLib, bool allowA, bool allowG) =>
        new(onigLib, [.. _items.Select(e => e.ResolveAnchors(allowA, allowG))], [.. _items.Select(e => e.RuleId)]);

    // _disposeCaches
    private void DisposeCaches()
    {
        _cached?.Dispose();
        _cached = null;
        _a0G0?.Dispose();
        _a0G0 = null;
        _a0G1?.Dispose();
        _a0G1 = null;
        _a1G0?.Dispose();
        _a1G0 = null;
        _a1G1?.Dispose();
        _a1G1 = null;
    }
}
