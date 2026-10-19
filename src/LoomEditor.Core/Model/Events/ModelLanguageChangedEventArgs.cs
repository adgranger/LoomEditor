// Ported from microsoft/vscode: src/vs/editor/common/textModelEvents.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Model.Events;

/// <summary>The language of the model changed (Monaco: <c>IModelLanguageChangedEvent</c>).</summary>
/// <param name="oldLanguage">The previous language id.</param>
/// <param name="newLanguage">The new language id.</param>
/// <param name="source">What changed the language (Monaco: <c>source</c>, "api" by default).</param>
/// <remarks>Threading: immutable; raised on the thread that owns the model.</remarks>
internal sealed class ModelLanguageChangedEventArgs(string oldLanguage, string newLanguage, string source) : EventArgs
{
    /// <summary><c>oldLanguage</c>.</summary>
    public string OldLanguage { get; } = oldLanguage;

    /// <summary><c>newLanguage</c>.</summary>
    public string NewLanguage { get; } = newLanguage;

    /// <summary><c>source</c>.</summary>
    public string Source { get; } = source;
}
