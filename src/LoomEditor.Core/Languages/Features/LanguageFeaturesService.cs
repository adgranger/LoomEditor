// Ported from microsoft/vscode: src/vs/editor/common/services/languageFeatures.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages.Features;

/// <summary>The provider registries of an editor (Monaco <c>ILanguageFeaturesService</c>: completion, hover and definition only).</summary>
/// <remarks>Threading: UI thread only (registration and queries); the providers' tasks are not the registry's concern.</remarks>
public sealed class LanguageFeaturesService
{
    /// <summary>The completion providers (Monaco <c>completionProvider</c>).</summary>
    public LanguageFeatureRegistry<ICompletionItemProvider> CompletionProviders { get; } = new();

    /// <summary>The hover providers (Monaco <c>hoverProvider</c>).</summary>
    public LanguageFeatureRegistry<IHoverProvider> HoverProviders { get; } = new();

    /// <summary>The definition providers (Monaco <c>definitionProvider</c>).</summary>
    public LanguageFeatureRegistry<IDefinitionProvider> DefinitionProviders { get; } = new();
}
