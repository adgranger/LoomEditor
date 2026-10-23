// Ported from microsoft/vscode: src/vs/editor/common/languages.ts (LocationLink, Definition)
// Upstream commit: 0c9c0a4295a9222e12a605537738719455ae3056  —  Copyright (c) Microsoft Corporation. MIT License.

using Range = LoomEditor.Core.Text.Range;

namespace LoomEditor.Core.Languages.Features;

/// <summary>A link from a source range to a target range in a document. Monaco's <c>Definition</c> union is flattened to a list of these.</summary>
/// <remarks>Threading: immutable, safe to share between threads.</remarks>
/// <param name="Uri">The target document URI.</param>
/// <param name="Range">The full target range.</param>
/// <param name="OriginSelectionRange">The source range that is the link.</param>
/// <param name="TargetSelectionRange">The target range to select and reveal.</param>
public sealed record LocationLink(string Uri, Range Range, Range? OriginSelectionRange = null, Range? TargetSelectionRange = null);
