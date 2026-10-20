// Ported from microsoft/vscode: src/vs/workbench/services/textMate/browser/indexTransformer.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Text;

namespace LoomEditor.Core.Model.Tokens;

/// <summary>Maps an index before some edits to the index after them (Monaco: <c>IIndexTransformer</c>).</summary>
internal interface IIndexTransformer
{
    /// <summary><c>transform</c>: the new index, or <see langword="null"/> when <paramref name="index"/> is touched by an edit.</summary>
    int? Transform(int index);
}

/// <summary>Monaco: <c>MonotonousIndexTransformer</c>. Can only be called with increasing values of <c>index</c>.</summary>
/// <remarks>Threading: not thread-safe (it has a cursor).</remarks>
internal sealed class MonotonousIndexTransformer : IIndexTransformer
{
    private readonly LengthEdit _transformation;
    private int _idx;
    private int _offset;

    /// <summary>Creates a transformer for <paramref name="transformation"/>.</summary>
    public MonotonousIndexTransformer(LengthEdit transformation)
    {
        ArgumentNullException.ThrowIfNull(transformation);
        _transformation = transformation;
    }

    /// <summary><c>fromMany</c>: applies the transformations one after the other.</summary>
    public static IIndexTransformer FromMany(IReadOnlyList<LengthEdit> transformations)
    {
        ArgumentNullException.ThrowIfNull(transformations);
        // TODO (upstream) improve performance by combining transformations first
        return new CombinedIndexTransformer([.. transformations.Select(t => new MonotonousIndexTransformer(t))]);
    }

    /// <summary>Precondition: <paramref name="index"/> is not smaller than the previous value.</summary>
    public int? Transform(int index)
    {
        IReadOnlyList<LengthReplacement> replacements = _transformation.Replacements;
        while (_idx < replacements.Count && replacements[_idx].ReplaceRange.EndExclusive <= index)
        {
            _offset += replacements[_idx].GetLengthDelta();
            _idx++;
        }

        // assert nextChange === undefined || index < nextChange.offset + nextChange.length
        if (_idx < replacements.Count && replacements[_idx].ReplaceRange.Start <= index)
        {
            // Offset is touched by the change
            return null;
        }

        return index + _offset;
    }
}

/// <summary>Monaco: <c>CombinedIndexTransformer</c>.</summary>
/// <remarks>Threading: not thread-safe.</remarks>
internal sealed class CombinedIndexTransformer(IReadOnlyList<IIndexTransformer> transformers) : IIndexTransformer
{
    /// <inheritdoc/>
    public int? Transform(int index)
    {
        foreach (IIndexTransformer transformer in transformers)
        {
            int? result = transformer.Transform(index);
            if (result is null)
            {
                return null;
            }

            index = result.Value;
        }

        return index;
    }
}
