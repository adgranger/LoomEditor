// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (DefinitionProvider)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Languages.Features;

/// <summary>Provides go-to-definition targets for a model.</summary>
/// <remarks>Threading: same contract as <see cref="ICompletionItemProvider"/>.</remarks>
public interface IDefinitionProvider
{
    /// <summary>Provides the definitions of the symbol at <paramref name="position"/>; null for none.</summary>
    Task<IReadOnlyList<LocationLink>?> ProvideDefinitionAsync(TextModel model, Position position, CancellationToken token);
}
