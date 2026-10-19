// Ported from microsoft/vscode: src/vs/editor/common/languages.ts
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

namespace LoomEditor.Core.Tokens;

/// <summary>Raised by a <see cref="TokenizationRegistry"/> (Monaco: <c>ITokenizationSupportChangedEvent</c>).</summary>
/// <param name="changedLanguages"><c>changedLanguages</c>.</param>
/// <param name="changedColorMap"><c>changedColorMap</c>.</param>
/// <remarks>Threading: immutable.</remarks>
internal sealed class TokenizationSupportChangedEventArgs(IReadOnlyList<string> changedLanguages, bool changedColorMap) : EventArgs
{
    /// <summary><c>changedLanguages</c>.</summary>
    public IReadOnlyList<string> ChangedLanguages { get; } = changedLanguages;

    /// <summary><c>changedColorMap</c>.</summary>
    public bool ChangedColorMap { get; } = changedColorMap;
}
