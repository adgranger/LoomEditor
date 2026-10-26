// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (Location)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Languages.Features;

/// <summary>A range in a document identified by URI.</summary>
/// <remarks>Threading: immutable value, safe to share between threads.</remarks>
/// <param name="Uri">The document URI.</param>
/// <param name="Range">The range in the document.</param>
public readonly record struct Location(string Uri, Range Range)
{
    /// <summary>The same location as a <see cref="LocationLink"/> without optional ranges.</summary>
    public LocationLink ToLink() => new(Uri, Range);
}
