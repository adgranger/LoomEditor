// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (HoverProvider)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using LoomEditor.Core.Model;
using LoomEditor.Core.Text;

namespace LoomEditor.Core.Languages.Features;

/// <summary>Provides hover information for a model.</summary>
/// <remarks>Threading: same contract as <see cref="ICompletionItemProvider"/> (UI-thread call, any-thread completion, cancelled results ignored).</remarks>
public interface IHoverProvider
{
    /// <summary>Provides the hover at <paramref name="position"/>; null for none.</summary>
    Task<Hover?> ProvideHoverAsync(TextModel model, Position position, CancellationToken token);
}
