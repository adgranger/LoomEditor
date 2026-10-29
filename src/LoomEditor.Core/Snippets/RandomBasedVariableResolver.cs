// Ported from microsoft/vscode: src/vs/editor/contrib/snippet/browser/snippetVariables.ts (RandomBasedVariableResolver)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using System.Globalization;

namespace LoomEditor.Core.Snippets;

/// <summary>Resolves <c>RANDOM</c>, <c>RANDOM_HEX</c> and <c>UUID</c> (Monaco: <c>RandomBasedVariableResolver</c>).</summary>
/// <remarks>Threading: UI thread only; resolvers are created per insertion.</remarks>
internal sealed class RandomBasedVariableResolver(Random random, Func<Guid>? newGuid = null) : IVariableResolver
{
    private readonly Func<Guid> _newGuid = newGuid ?? Guid.NewGuid;

    /// <inheritdoc />
    public string? Resolve(Variable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);

        return variable.Name switch
        {
            "RANDOM" => random.Next(1_000_000).ToString("D6", CultureInfo.InvariantCulture),
            "RANDOM_HEX" => random.Next(0x1000000).ToString("x6", CultureInfo.InvariantCulture),
            "UUID" => _newGuid().ToString(),
            _ => null,
        };
    }
}
