// Ported from microsoft/vscode: src/vs/editor/contrib/folding/browser/foldingRanges.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.View.Features.Folding;

/// <summary>A range of whole lines (Monaco: <c>ILineRange</c>).</summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal interface ILineRange
{
    int StartLineNumber { get; }

    int EndLineNumber { get; }
}

/// <summary>Where a folding range comes from (Monaco: <c>FoldSource</c>).</summary>
internal enum FoldSource
{
    Provider = 0,
    UserDefined = 1,
    Recovered = 2,
}

/// <summary>A folding range with its state (Monaco: <c>FoldRange</c>); mutable because <c>sanitizeAndMerge</c> updates it.</summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class FoldRange
{
    public int StartLineNumber { get; set; }

    public int EndLineNumber { get; set; }

    public string? Type { get; set; }

    public bool IsCollapsed { get; set; }

    public FoldSource Source { get; set; }
}

/// <summary>
/// The compact list of folding ranges of a model: start and end lines in two <c>uint[]</c> (the parent index is packed in their
/// high bytes), with collapsed, user-defined and recovered flags (Monaco: <c>FoldingRegions</c>).
/// </summary>
/// <remarks>Threading: UI thread only.</remarks>
internal sealed partial class FoldingRegions
{
    /// <summary>Monaco: <c>MAX_FOLDING_REGIONS</c>.</summary>
    internal const uint MaxFoldingRegions = 0xFFFF;

    /// <summary>Monaco: <c>MAX_LINE_NUMBER</c>.</summary>
    internal const uint MaxLineNumber = 0xFFFFFF;

    private const uint MaskIndent = 0xFF000000;

    private readonly uint[] _startIndexes;
    private readonly uint[] _endIndexes;
    private readonly BitField _collapseStates;
    private readonly BitField _userDefinedStates;
    private readonly BitField _recoveredStates;
    private readonly IReadOnlyList<string?>? _types;
    private bool _parentsComputed;

    public FoldingRegions(uint[] startIndexes, uint[] endIndexes, IReadOnlyList<string?>? types = null)
    {
        ArgumentNullException.ThrowIfNull(startIndexes);
        ArgumentNullException.ThrowIfNull(endIndexes);
        if (startIndexes.Length != endIndexes.Length || startIndexes.Length > MaxFoldingRegions)
        {
            throw new ArgumentException("invalid startIndexes or endIndexes size");
        }

        _startIndexes = startIndexes;
        _endIndexes = endIndexes;
        _collapseStates = new BitField(startIndexes.Length);
        _userDefinedStates = new BitField(startIndexes.Length);
        _recoveredStates = new BitField(startIndexes.Length);
        _types = types;
    }

    public int Length => _startIndexes.Length;

    private void EnsureParentIndices()
    {
        if (_parentsComputed)
        {
            return;
        }

        _parentsComputed = true;
        var parentIndexes = new List<int>();
        for (int i = 0, len = _startIndexes.Length; i < len; i++)
        {
            uint startLineNumber = _startIndexes[i];
            uint endLineNumber = _endIndexes[i];
            if (startLineNumber > MaxLineNumber || endLineNumber > MaxLineNumber)
            {
                throw new InvalidOperationException("startLineNumber or endLineNumber must not exceed " + MaxLineNumber);
            }

            while (parentIndexes.Count > 0 && !IsInsideLast(parentIndexes, (int)startLineNumber, (int)endLineNumber))
            {
                parentIndexes.RemoveAt(parentIndexes.Count - 1);
            }

            int parentIndex = parentIndexes.Count > 0 ? parentIndexes[^1] : -1;
            parentIndexes.Add(i);
            unchecked
            {
                _startIndexes[i] = startLineNumber + (((uint)parentIndex & 0xFF) << 24);
                _endIndexes[i] = endLineNumber + (((uint)parentIndex & 0xFF00) << 16);
            }
        }
    }

    private bool IsInsideLast(List<int> parentIndexes, int startLineNumber, int endLineNumber)
    {
        int index = parentIndexes[^1];
        return GetStartLineNumber(index) <= startLineNumber && GetEndLineNumber(index) >= endLineNumber;
    }

    public int GetStartLineNumber(int index) => (int)(_startIndexes[index] & MaxLineNumber);

    public int GetEndLineNumber(int index) => (int)(_endIndexes[index] & MaxLineNumber);

    public string? GetType(int index) => _types?[index];

    public bool HasTypes() => _types is not null;

    public bool IsCollapsed(int index) => _collapseStates.Get(index);

    public void SetCollapsed(int index, bool newState) => _collapseStates.Set(index, newState);

    public FoldSource GetSource(int index)
    {
        if (_userDefinedStates.Get(index))
        {
            return FoldSource.UserDefined;
        }

        return _recoveredStates.Get(index) ? FoldSource.Recovered : FoldSource.Provider;
    }

    public void SetSource(int index, FoldSource source)
    {
        _userDefinedStates.Set(index, source == FoldSource.UserDefined);
        _recoveredStates.Set(index, source == FoldSource.Recovered);
    }

    public bool SetCollapsedAllOfType(string type, bool newState)
    {
        bool hasChanged = false;
        if (_types is not null)
        {
            for (int i = 0; i < _types.Count; i++)
            {
                if (_types[i] == type)
                {
                    SetCollapsed(i, newState);
                    hasChanged = true;
                }
            }
        }

        return hasChanged;
    }

    public FoldingRegion ToRegion(int index) => new(this, index);

    /// <summary>Returns the index of the enclosing region, or -1 for a top-level region.</summary>
    public int GetParentIndex(int index)
    {
        EnsureParentIndices();
        uint parent = ((_startIndexes[index] & MaskIndent) >> 24) + ((_endIndexes[index] & MaskIndent) >> 16);
        return parent == MaxFoldingRegions ? -1 : (int)parent;
    }

    public bool Contains(int index, int line) => GetStartLineNumber(index) <= line && GetEndLineNumber(index) >= line;

    private int FindIndex(int line)
    {
        int low = 0;
        int high = _startIndexes.Length;
        if (high == 0)
        {
            return -1; // no children
        }

        while (low < high)
        {
            int mid = (low + high) / 2;
            if (line < GetStartLineNumber(mid))
            {
                high = mid;
            }
            else
            {
                low = mid + 1;
            }
        }

        return low - 1;
    }

    public int FindRange(int line)
    {
        int index = FindIndex(line);
        if (index >= 0)
        {
            if (GetEndLineNumber(index) >= line)
            {
                return index;
            }

            index = GetParentIndex(index);
            while (index != -1)
            {
                if (Contains(index, line))
                {
                    return index;
                }

                index = GetParentIndex(index);
            }
        }

        return -1;
    }

    public override string ToString()
    {
        var res = new string[Length];
        for (int i = 0; i < Length; i++)
        {
            char abbr = GetSource(i) switch { FoldSource.UserDefined => 'u', FoldSource.Recovered => 'r', _ => ' ' };
            res[i] = $"[{abbr}{(IsCollapsed(i) ? '+' : '-')}] {GetStartLineNumber(i)}/{GetEndLineNumber(i)}";
        }

        return string.Join(", ", res);
    }

    public FoldRange ToFoldRange(int index) => new()
    {
        StartLineNumber = GetStartLineNumber(index),
        EndLineNumber = GetEndLineNumber(index),
        Type = _types?[index],
        IsCollapsed = IsCollapsed(index),
        Source = GetSource(index),
    };

    private sealed class BitField(int size)
    {
        private readonly uint[] _states = new uint[(size + 31) / 32];

        public bool Get(int index) => (_states[index / 32] & (1u << (index % 32))) != 0;

        public void Set(int index, bool newState)
        {
            uint bit = 1u << (index % 32);
            if (newState)
            {
                _states[index / 32] |= bit;
            }
            else
            {
                _states[index / 32] &= ~bit;
            }
        }
    }
}

