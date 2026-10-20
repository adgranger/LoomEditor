// Ported from microsoft/vscode: src/vs/editor/common/languages/languageConfigurationRegistry.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Languages;

/// <summary>
/// Raised by a <see cref="LanguageConfigurationRegistry"/> (Monaco: <c>LanguageConfigurationServiceChangeEvent</c>,
/// also <c>LanguageConfigurationChangeEvent</c>).
/// </summary>
/// <param name="languageId">The language whose configuration changed.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed class LanguageConfigurationChangedEventArgs(string languageId) : EventArgs
{
    /// <summary><c>languageId</c>.</summary>
    public string LanguageId { get; } = languageId;

    /// <summary><c>affects</c>: whether the change affects <paramref name="languageId"/>.</summary>
    public bool Affects(string languageId) => LanguageId == languageId;
}
